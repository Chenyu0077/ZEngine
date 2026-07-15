//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using System;
using YooAsset;
using ZEngine.Core;
using ZEngine.Manager.Resource;
using System.Collections.Generic;
using HybridCLR;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using ZEngine.Manager.Scene;
using ZEngine.Manager.Audio;
using ZEngine.Manager.Pool;
using ZEngine.Manager.Event;
using ZEngine.Module.Collider2D;
using ZEngine.Manager.UI;
using ZEngine.Manager.Timer;
using ZEngine.Module.Archive;
using ZEngine.Manager.Http;
using ZEngine.Manager.Log;
using ZEngine.Manager.Mouse;
using ZEngine.Manager.UI.UGUI;
using ZEngine.Config;
using ZEngine.Manager.Network;

public class GameLauncher : MonoBehaviour
{
    /// <summary>
    /// 资源系统运行模式
    /// </summary>
    public EPlayMode PlayMode = EPlayMode.HostPlayMode;

    [Header("热更进度条（场景中搭建后拖入）")]
    public HotUpdateProgressUI hotUpdateProgressUI;

    private ResourcePackage _defaultPackage;
    private object _hotUpdateMainInstance;
    private MethodInfo _updateMethod;
    private MethodInfo _lateUpdateMethod;
    private MethodInfo _fixedUpdateMethod;
    private MethodInfo _destroyMethod;

    private void Awake()
    {
#if !UNITY_EDITOR
    #if ENABLE_HOT_UPDATE
        PlayMode = EPlayMode.HostPlayMode;
    #else
        // 热更新关闭时强制使用本地离线包，不访问 CDN
        PlayMode = EPlayMode.OfflinePlayMode;
    #endif
        
#endif

        InitApplication();
        ZEngineMain.Initialize(this);
        ZEngineMain.CreateManager<LogManager>();
    }

    private async void Start()
    {
        if (hotUpdateProgressUI == null) hotUpdateProgressUI = HotUpdateProgressUI.Instance;
        await InitYooAssets();
    }

    private void Update()
    {
        _updateMethod?.Invoke(_hotUpdateMainInstance, null);
    }

    private void OnGUI()
    {
        ZEngineMain.DrawGUI();
    }

    private void LateUpdate()
    {
        _lateUpdateMethod?.Invoke(_hotUpdateMainInstance, null);
    }

    private void FixedUpdate()
    {
        ZEngineMain.Update();
        _fixedUpdateMethod?.Invoke(_hotUpdateMainInstance, null);
    }

    private void OnDestroy()
    {
        _destroyMethod?.Invoke(_hotUpdateMainInstance, null);
        ZEngineMain.Destroy();
    }


    private void InitApplication()
    {
        Application.runInBackground = true;
        Application.backgroundLoadingPriority = ThreadPriority.High;

        // 设置最大帧数
        Application.targetFrameRate = 60;

        // 屏幕不休眠
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void CreateManager()
    {
        //ZEngineMain.CreateManager<ResourceManager>();
        ZEngineMain.CreateManager<SceneManager>();
        ZEngineMain.CreateManager<AudioManager>();

        ObjectPoolManager.CreateParameters parameters = new ObjectPoolManager.CreateParameters();
        parameters.DefaultInitCapacity = 0;
        parameters.DefaultMaxCapacity = 9999;
        parameters.DefaultDestroyTime = 5f;
        ZEngineMain.CreateManager<ObjectPoolManager>(parameters);
        ZEngineMain.CreateManager<EventManager>();
        ZEngineMain.CreateManager<UIManager>();
        ZEngineMain.CreateManager<UUIManager>();
        ZEngineMain.CreateManager<TimerManager>();
        ZEngineMain.CreateManager<ColliderManager>();
        ZEngineMain.CreateManager<ArchiveManager>();
        ZEngineMain.CreateManager<HttpManager>();
        ZEngineMain.CreateManager<MouseManager>();
        ZEngineMain.CreateManager<NetworkManager>();
    }


    #region YooAsset初始化并初始化资源管理器
    private async UniTask InitYooAssets()
    {
        string locationRoot = GameAssetPaths.AssetRoot;
        string packageName = "DefaultPackage";
        // 1.初始化资源系统
        YooAssets.Initialize();
        InitializationOperation initializationOperation = null;
        
        if (PlayMode == EPlayMode.EditorSimulateMode)
        {
            //编辑器模拟模式
            var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
            var packageRoot = buildResult.PackageRootDirectory;
            var editorFileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);

            var initParameters = new EditorSimulateModeParameters();
            initParameters.EditorFileSystemParameters = editorFileSystemParams;
            ZEngineMain.CreateManager<ResourceManager>(initParameters);
            initializationOperation = ResourceManager.Instance.InitializeAsync(out _defaultPackage, locationRoot, packageName);
        }
        else if(PlayMode == EPlayMode.OfflinePlayMode)
        {
            //单机运行模式
            var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

            var initParameters = new OfflinePlayModeParameters();
            initParameters.BuildinFileSystemParameters = buildinFileSystemParams;
            ZEngineMain.CreateManager<ResourceManager>(initParameters);
            initializationOperation = ResourceManager.Instance.InitializeAsync(out _defaultPackage, locationRoot, packageName);
        }
        else if(PlayMode == EPlayMode.HostPlayMode)
        {
            //联机运行模式
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            var cacheFileSystemParams = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

            var initParameters = new HostPlayModeParameters();
            initParameters.CacheFileSystemParameters = cacheFileSystemParams;
            initParameters.BuildinFileSystemParameters = buildinFileSystemParams;
            ZEngineMain.CreateManager<ResourceManager>(initParameters);
            initializationOperation = ResourceManager.Instance.InitializeAsync(out _defaultPackage, locationRoot, packageName);
        }
        else if (PlayMode == EPlayMode.WebPlayMode)
        {
            //Web运行模式
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            var webServerFileSystemParams = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
            var webRemoteFileSystemParams = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices); //支持跨域下载

            var initParameters = new WebPlayModeParameters();
            initParameters.WebServerFileSystemParameters = webServerFileSystemParams;
            initParameters.WebRemoteFileSystemParameters = webRemoteFileSystemParams;
            ZEngineMain.CreateManager<ResourceManager>(initParameters);
            initializationOperation = ResourceManager.Instance.InitializeAsync(out _defaultPackage, locationRoot, packageName);

        }

        await initializationOperation;

        if (initializationOperation.Status == EOperationStatus.Succeed)
        {
            LogManager.Instance.Info("资源包初始化成功！");
        }
        else
        {
            LogManager.Instance.Error($"资源包初始化失败：{initializationOperation.Error}");
        }

        //2.获取资源版本（本地或远程，两种模式都需要）
        string packageVersion = await ResourceManager.Instance.RequestPackageVersion();

        //3.更新补丁清单（激活 manifest，两种模式都需要）
        // 更新成功后自动保存版本号，作为下次初始化的版本。
        // 也可以通过operation.SavePackageVersion()方法保存。
        await ResourceManager.Instance.UpdatePackageManifestAsync(packageVersion);

#if ENABLE_HOT_UPDATE
        //4.下载补丁包（仅热更新模式从 CDN 拉取最新资源）
        if (hotUpdateProgressUI != null) hotUpdateProgressUI.Show();
        await ResourceManager.Instance.DownLoadPackageFiles((progress, downloaded, total) =>
        {
            if (hotUpdateProgressUI != null) hotUpdateProgressUI.SetProgress(progress, downloaded, total);
        });
        //if (hotUpdateProgressUI != null) hotUpdateProgressUI.Hide();
        LogManager.Instance.Info("[热更新已开启]");
#else
        LogManager.Instance.Info("[热更新已关闭]");
#endif

        //判断是否下载成功
        var assets = new List<string> { "HotUpdate.dll" }.Concat(AOTMetaAssemblyFiles);
        foreach (var asset in assets)
        {
            string assetPath = string.Format("{0}{1}", GameAssetPaths.Hotfix, asset);
            var handle = await ResourceManager.Instance.LoadAssetAsync<TextAsset>(assetPath);
            if (handle == null)
                continue;
            var assetObj = handle.AssetObject as TextAsset;
            s_assetDatas[asset] = assetObj;
            LogManager.Instance.Info($"dll:{asset}   {assetObj != null}");
        }

        //创建管理器
        CreateManager();
        //启动游戏
        StartGame();
    }


    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    private class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }

    private string GetHostServerURL()
    {
        //模拟下载地址，8084为Nginx里面设置的端口号，项目名，平台名
        var cfg = ClientConfig.Instance;
        return $"{cfg.HostServerUrl}/Bundles";
    }

    #endregion


    #region 补充元数据

    //补充元数据dll的列表
    //通过RuntimeApi.LoadMetadataForAOTAssembly()函数来补充AOT泛型的原始元数据
    private static List<string> AOTMetaAssemblyFiles { get; } = new() { "mscorlib.dll", "System.dll", "System.Core.dll", };
    private static Dictionary<string, TextAsset> s_assetDatas = new Dictionary<string, TextAsset>();
    private static Assembly _hotUpdateAss;

    public static byte[] ReadBytesFromStreamingAssets(string dllName)
    {
        if (s_assetDatas.ContainsKey(dllName))
        {
            return s_assetDatas[dllName].bytes;
        }

        return Array.Empty<byte>();
    }



    /// <summary>
    /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
    /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
    /// </summary>
    private static void LoadMetadataForAOTAssemblies()
    {
        /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
        /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
        HomologousImageMode mode = HomologousImageMode.SuperSet;
        foreach (var aotDllName in AOTMetaAssemblyFiles)
        {
            byte[] dllBytes = ReadBytesFromStreamingAssets(aotDllName);
            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            ZEngineLog.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
        }
    }

    #endregion


    #region 热更入口
    void StartGame()
    {
        // 加载AOT dll的元数据
        LoadMetadataForAOTAssemblies();
        // 加载热更dll
#if !UNITY_EDITOR
        _hotUpdateAss = Assembly.Load(ReadBytesFromStreamingAssets("HotUpdate.dll"));
#else
        _hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
#endif

        LogManager.Instance.Info("运行热更DLL代码");
        Type type = _hotUpdateAss.GetType("Hotfix.Core.HotUpdateMain");
        _hotUpdateMainInstance = Activator.CreateInstance(type);
        _updateMethod = type.GetMethod("Update");
        _lateUpdateMethod = type.GetMethod("LateUpdate");
        _fixedUpdateMethod = type.GetMethod("FixedUpdate");
        _destroyMethod = type.GetMethod("Destroy");
        type.GetMethod("Run").Invoke(_hotUpdateMainInstance, null);
    }

    #endregion
}
