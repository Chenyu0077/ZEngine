using System;
using DG.Tweening;
using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.FuncModule.AITown;
using Main.FuncModule.Camera2D;
using Hotfix.FuncModule.Village;
using Hotfix.UI.Generate.Common;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.Log;
using ZEngine.Manager.UI;
using Hotfix.Main.Logic;

namespace Hotfix.Main.UI
{
    /// <summary>
    /// 游戏流程阶段，按操作顺序递进
    /// </summary>
    public enum GameFlowPhase
    {
        Idle,               // 初始，未做任何操作
        ServerStarting,     // 服务器启动中
        ServerReady,        // 服务器就绪，等待连接
        Connecting,         // WebSocket 连接中
        Connected,          // 已连接，可以生成家庭/开始模拟
        SimulationRunning,  // 模拟运行中
        SimulationPaused,   // 模拟已暂停
        SimulationStopped,  // 模拟已停止（连接仍在）
    }

    public class GameControlController : BaseController
    {
        private UIGameControlView compt;

        [Header("家庭数设置")]
        private int currentFamilyCount = 1; // 当前家庭数
        private const int MIN_FAMILY_COUNT = 0; // 最小家庭数
        private const int MAX_FAMILY_COUNT = 16; // 最大家庭数

        private const int BTN_PAUSE_ORIGINAL_INDEX = 5;
        private const int BTN_RESUME_ORIGINAL_INDEX = 6;
        private GObject _btnPause;
        private GObject _btnResume;

        private bool _wsConnected;

        private GameFlowPhase _flowPhase = GameFlowPhase.Idle;
        public GameFlowPhase FlowPhase => _flowPhase;
        public static event Action<GameFlowPhase> OnFlowPhaseChanged;

        private void SetFlowPhase(GameFlowPhase phase)
        {
            if (_flowPhase == phase) return;
            _flowPhase = phase;
            LogManager.Instance.Info($"[GameControlController] 流程阶段: {phase}");
            if (compt != null)
                compt.m_statusText.text = GetFlowPhaseRichText(phase);
            OnFlowPhaseChanged?.Invoke(phase);
        }

        private static string GetFlowPhaseRichText(GameFlowPhase phase)
        {
            var (label, color) = phase switch
            {
                GameFlowPhase.Idle               => ("• 空闲",       "#888888"),
                GameFlowPhase.ServerStarting     => ("• 服务器启动中", "#FFA500"),
                GameFlowPhase.ServerReady        => ("• 服务器就绪",  "#00BFFF"),
                GameFlowPhase.Connecting         => ("• 连接中",      "#FFA500"),
                GameFlowPhase.Connected          => ("• 已连接",      "#00CC66"),
                GameFlowPhase.SimulationRunning  => ("• 模拟运行中",  "#00FF99"),
                GameFlowPhase.SimulationPaused   => ("• 模拟已暂停",  "#FFDD00"),
                GameFlowPhase.SimulationStopped  => ("• 模拟已停止",  "#FF6666"),
                _                                => (phase.ToString(), "#FFFFFF"),
            };
            return $"[color={color}]{label}[/color]";
        }


        public override void Initialize()
        {
            base.Initialize();
            compt = _view.GetView() as UIGameControlView;

            if (compt != null)
            {
                /*// 初始化滑动条设置
                InitializeFamilySlider();

                // 绑定事件
                compt.m_faimlySlider.onChanged.Add(OnFaimlySliderChanged);
                compt.m_faimlyCount.onChanged.Add(OnFamilyCountTextChanged);*/
                compt.m_btnList.GetChild("btnSpawnFaimly").onClick.Add(OnSpawnFaimlyBtnEvent);
                compt.m_btnList.GetChild("btnStart").onClick.Add(OnStartBtnEvent);
                compt.m_btnList.GetChild("btnStop").onClick.Add(OnStopBtnEvent);
                //compt.m_btnList.GetChild("btnCommand").onClick.Add(OnCommandBtnEvent);
                compt.m_btnList.GetChild("btnReturnMain").onClick.Add(OnReturnMainBtnEvent);
                compt.m_btnList.GetChild("btnStartServer").onClick.Add(OnStartServerBtnEvent);
                compt.m_btnList.GetChild("btnStopServer").onClick.Add(OnStopServerBtnEvent);
                compt.m_btnList.GetChild("btnStartConnect").onClick.Add(OnStartConnectBtnEvent);
                compt.m_btnList.GetChild("btnCommand").visible = false;

                _btnPause = compt.m_btnList.GetChild("btnPause");
                _btnResume = compt.m_btnList.GetChild("btnResume");
                _btnPause.onClick.Add(OnPauseBtnEvent);
                _btnResume.onClick.Add(OnResumeBtnEvent);
                compt.m_btnList.RemoveChild(_btnPause, false);
                compt.m_btnList.RemoveChild(_btnResume, false);
                compt.m_btnList.ResizeToFit();
                compt.m_controlModeBtn.onClick.Add(OnControlModeBtnEvent);
                compt.m_popBtn.onClick.Add(OnPopBtnEvent);
                compt.m_popCtrol.selectedIndex = 1;
                
                // 置灰且不能点击
                // var btnStartServer = compt.m_btnList.GetChild("btnStartServer");
                // var btnStopServer = compt.m_btnList.GetChild("btnStopServer");
                // btnStartServer.grayed = true; btnStartServer.touchable = false;
                // if (btnStartServer is UICommonBtn2 btnStart) btnStart.m_maskCtrol.selectedIndex = 1;
                // btnStopServer.grayed = true; btnStopServer.touchable = false;
                // if (btnStopServer is UICommonBtn2 btnStop) btnStop.m_maskCtrol.selectedIndex = 1;
            }

            _view.OnChanged = (data) =>
            {
                // 数据变更时的处理
            };
            
            // 网络连接
            _wsConnected = false;
            WebSocketMgr.Instance.OnConnected    += OnWsConnected;
            WebSocketMgr.Instance.OnDisconnected += OnWsDisconnected;
            ServerLauncher.OnServerReady         += OnServerReady;
        }


        #region WebSocket连接

        private void OnServerReady()
        {
            var client = AIVillageClient.Instance;
            LogManager.Instance.Info($"[GameControlController] 服务器就绪，自动连接，类型: {client.ServerType}");
            SetFlowPhase(GameFlowPhase.ServerReady);
            SetFlowPhase(GameFlowPhase.Connecting);
            client.Connect();
        }

        private void OnWsConnected()
        {
            Debug.Log("OpenGamePermanceViews");
            _wsConnected = true;
            SetFlowPhase(GameFlowPhase.Connected);
            OpenGamePermanceViews();
            TrySendGameReady();
            MaxTipModel tipModel = new MaxTipModel{ TipContent = "连接成功! 请先设置你的BaseUrl和Key，成功后再进行后续操作"};
            UIManager.Instance.OpenViewSync<MaxTipView>(tipModel);
        }

        private void OnWsDisconnected()
        {
            _wsConnected = false;
            SetFlowPhase(GameFlowPhase.Idle);
            TipModel tipModel = new TipModel{ TipContent = "连接失败"};
            UIManager.Instance.OpenViewSync<TipView>(tipModel);
        }
        
        /// <summary>
        /// WebSocket 就绪后，发送 game_ready
        /// </summary>
        private void TrySendGameReady()
        {
            if (!_wsConnected) return;

            LogManager.Instance.Info("[WorldSpawnNode] 发送 game_ready");
            WebSocketMgr.Instance.SendGameReady();
        }
    
        /// <summary>
        /// 打开常驻面板
        /// </summary>
        private void OpenGamePermanceViews()
        {
            UIManager.Instance.OpenViewSync<GamePermanceView>();
            UIManager.Instance.OpenViewSync<NPCMainView>();
        }

        #endregion


        #region 家庭滑动条
        
        /*/// <summary>
        /// 初始化家庭数滑动条
        /// </summary>
        private void InitializeFamilySlider()
        {
            // 设置滑动条范围
            compt.m_faimlySlider.min = (float)MIN_FAMILY_COUNT;
            compt.m_faimlySlider.max = (float)MAX_FAMILY_COUNT;
            compt.m_faimlySlider.value = (float)currentFamilyCount;

            // 更新显示
            UpdateFamilyCountDisplay();

            Debug.Log($"📊 家庭数滑动条初始化: 范围 {MIN_FAMILY_COUNT}-{MAX_FAMILY_COUNT}, 当前值: {currentFamilyCount}");
        }

        /// <summary>
        /// 滑动条数值变化事件
        /// </summary>
        /// <param name="context"></param>
        private void OnFaimlySliderChanged(EventContext context)
        {
            float sliderValue = (float)compt.m_faimlySlider.value;
            int newFamilyCount = Mathf.RoundToInt(sliderValue);

            newFamilyCount = Mathf.Clamp(newFamilyCount, MIN_FAMILY_COUNT, MAX_FAMILY_COUNT);

            if (newFamilyCount != currentFamilyCount)
            {
                currentFamilyCount = newFamilyCount;

                // 同步滑动条值为整数（避免小数显示）
                compt.m_faimlySlider.value = (float)currentFamilyCount;

                // 更新文本显示
                UpdateFamilyCountDisplay();

                Debug.Log($"📊 家庭数已更新: {currentFamilyCount}");
            }
        }

        /// <summary>
        /// 文本输入框变化事件
        /// </summary>
        /// <param name="context"></param>
        private void OnFamilyCountTextChanged(EventContext context)
        {
            string textValue = compt.m_faimlyCount.text;

            if (int.TryParse(textValue, out int inputCount))
            {
                int clampedCount = Mathf.Clamp(inputCount, MIN_FAMILY_COUNT, MAX_FAMILY_COUNT);

                // 如果输入值超出范围，更正文本显示
                if (clampedCount != inputCount)
                {
                    compt.m_faimlyCount.text = clampedCount.ToString();
                }

                // 更新当前值和滑动条
                if (clampedCount != currentFamilyCount)
                {
                    currentFamilyCount = clampedCount;
                    compt.m_faimlySlider.value = (float)currentFamilyCount;
                }
            }
            else if (!string.IsNullOrEmpty(textValue))
            {
                // 如果输入的不是有效数字，恢复为当前值
                compt.m_faimlyCount.text = currentFamilyCount.ToString();
                Debug.LogWarning("⚠️ 输入的家庭数格式无效，已恢复为当前值");
            }
        }

        /// <summary>
        /// 更新家庭数显示
        /// </summary>
        private void UpdateFamilyCountDisplay()
        {
            if (compt != null)
            {
                compt.m_faimlyCount.text = currentFamilyCount.ToString();
            }
        }*/
        
        #endregion


        private void ShowPauseAndResumeButtons()
        {
            if (_btnPause != null && _btnPause.parent == null)
            {
                compt.m_btnList.AddChildAt(_btnPause, BTN_PAUSE_ORIGINAL_INDEX);
            }
            if (_btnResume != null && _btnResume.parent == null)
            {
                compt.m_btnList.AddChildAt(_btnResume, BTN_RESUME_ORIGINAL_INDEX);
            }
            compt.m_btnList.ResizeToFit();
        }

        private void OnSpawnFaimlyBtnEvent(EventContext context)
        {
            UIManager.Instance.OpenViewSync<WaitingView>();
            AIVillageClient.Instance.RegisterNPCs(currentFamilyCount, (response) =>
            {
                // 按家庭分配房屋，并绑定家庭成员
                VillageBuilder.Instance.AssignHouses(response.Npcs);
            });
        }
 
        private void OnStartBtnEvent(EventContext context)
        {
            WaitingModel model = new WaitingModel(){ WaitContent = "等待模拟初始化中..."};
            UIManager.Instance.OpenViewSync<WaitingView>(model);
            AIVillageClient.Instance.StartSimulation(currentFamilyCount, (response) =>
            {
                SetFlowPhase(GameFlowPhase.SimulationRunning);
                ShowPauseAndResumeButtons();
                UIManager.Instance.CloseView<WaitingView>();
                UIManager.Instance.OpenViewSync<InteractView>();
                UIManager.Instance.OpenViewSync<TaxView>();
            },
            (eror) =>
            {
                UIManager.Instance.CloseView<WaitingView>();
            });
            ShowPauseAndResumeButtons();
        }

        private void OnStopBtnEvent(EventContext context)
        {
            AIVillageClient.Instance.StopSimulation();
            SetFlowPhase(GameFlowPhase.SimulationStopped);
        }

        private void OnReturnMainBtnEvent(EventContext context)
        {
            if (AIVillageClient.Instance.SimStatus == SimultionStatus.Started)
            {
                AIVillageClient.Instance.PauseSimulation();
            }
            var model = new MainModel { HasGameStarted = true };
            UIManager.Instance.OpenViewSync<MainView>(model);
        }

        private void OnPauseBtnEvent(EventContext context)
        {
            AIVillageClient.Instance.PauseSimulation();
            SetFlowPhase(GameFlowPhase.SimulationPaused);
        }

        private void OnResumeBtnEvent(EventContext context)
        {
            AIVillageClient.Instance.ResumeSimulation();
            SetFlowPhase(GameFlowPhase.SimulationRunning);
        }
        
        /// <summary>
        /// 命令按钮点击事件
        /// </summary>
        private void OnCommandBtnEvent(EventContext context)
        {
            UIManager.Instance.OpenViewSync<NPCCommandView>();
        }

        /// <summary>
        /// 玩家控制切换按钮
        /// </summary>
        private void OnControlModeBtnEvent(EventContext context)
        {
            var camera = Camera.main;
            var cameraControl = camera.GetComponent<Camera2DController>();
            if (cameraControl != null)
            {
                Camera2DMode mode = compt.m_controlModeBtn.selected ? Camera2DMode.Follow : Camera2DMode.GodView;

                if (mode == Camera2DMode.Follow)
                {
                    var agent = AITownManager.Instance.GetAgentOfVillageChief();
                    if (agent != null)
                    {
                        LogManager.Instance.Info("村长控制开启");
                        // 先平滑移动相机到目标位置并缩放到更近的距离
                        Vector2 targetPos = new(agent.transform.position.x, agent.transform.position.y);
                        float followZoom = 5f; // 跟随模式使用更近的缩放值，可根据需要调整
                        cameraControl.MoveTo(targetPos, followZoom);

                        // 设置跟随目标
                        cameraControl.SetFollowTarget(agent.transform);
                    }
                    else
                    {
                        LogManager.Instance.Warning("VillageChief 未创建");
                        cameraControl.SetMode(Camera2DMode.GodView);
                    }
                }
                else
                {
                    // 切换到上帝视角时，可以缩放到更远的距离以获得更好的全景视野
                    float godViewZoom = 12f; // 上帝视角使用更远的缩放值
                    Vector2 currentPos = new(camera.transform.position.x, camera.transform.position.y);
                    cameraControl.MoveTo(currentPos, godViewZoom);
                    cameraControl.SetMode(mode);
                }
            }
        }
        
        /// <summary>
        /// 弹出按钮事件
        /// </summary>
        private void OnPopBtnEvent(EventContext context)
        {
            compt.m_popCtrol.selectedIndex = compt.m_popCtrol.selectedIndex == 0 ? 1 : 0;
            float screenWidth = GRoot.inst.width;                                                                                                                                                                             
            float screenHeight = GRoot.inst.height;
            if (compt.m_popCtrol.selectedIndex == 0)
            {
                float toY = _view.GetPosition(BaseView.ScreenCorner.TopLeft, 10, -compt.height).y;
                compt.DOMoveY( toY, 1.5f).SetEase(Ease.InOutQuad);
            }
            else
            {
                float toY = _view.GetPosition(BaseView.ScreenCorner.TopLeft, 10, 10f).y;
                compt.DOMoveY(toY, 1.5f).SetEase(Ease.InOutQuad);
            }
        }


        /// <summary>
        /// 开启服务器
        /// </summary>
        private void OnStartServerBtnEvent(EventContext context)
        {
            SetFlowPhase(GameFlowPhase.ServerStarting);
            ServerLauncher.Instance.LaunchServer();
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        private void OnStopServerBtnEvent(EventContext context)
        {
            ServerLauncher.Instance.StopServer();
            SetFlowPhase(GameFlowPhase.Idle);
        }

        /// <summary>
        /// 开启Websocket连接
        /// </summary>
        private void OnStartConnectBtnEvent(EventContext context)
        {
            var client = AIVillageClient.Instance;
            LogManager.Instance.Info($"[WorldSpawnNode] 连接服务器，类型: {client.ServerType}");
            SetFlowPhase(GameFlowPhase.Connecting);
            client.Connect();
        }
        
        
        public override void OnRelease()
        {
            WebSocketMgr.Instance.OnConnected    -= OnWsConnected;
            WebSocketMgr.Instance.OnDisconnected -= OnWsDisconnected;
            ServerLauncher.OnServerReady         -= OnServerReady;
            
            if (compt != null)
            {
                // 移除事件监听
                /*compt.m_faimlySlider.onChanged.Remove(OnFaimlySliderChanged);
                compt.m_faimlyCount.onChanged.Remove(OnFamilyCountTextChanged);*/
                compt.m_btnList.GetChild("btnSpawnFaimly").onClick.Remove(OnSpawnFaimlyBtnEvent);
                compt.m_btnList.GetChild("btnStart").onClick.Remove(OnStartBtnEvent);
                compt.m_btnList.GetChild("btnStop").onClick.Remove(OnStopBtnEvent);
                //compt.m_btnList.GetChild("btnCommand").onClick.Remove(OnCommandBtnEvent);
                compt.m_btnList.GetChild("btnReturnMain").onClick.Remove(OnReturnMainBtnEvent);
                if (_btnPause != null) _btnPause.onClick.Remove(OnPauseBtnEvent);
                if (_btnResume != null) _btnResume.onClick.Remove(OnResumeBtnEvent);
                compt.m_controlModeBtn.onClick.Remove(OnControlModeBtnEvent);
                compt.m_popBtn.onClick.Remove(OnPopBtnEvent);
                compt = null;
            }

            base.OnRelease();
        }
    }
}