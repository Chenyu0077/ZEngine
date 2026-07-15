using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using ZEngine.Manager.Log;

/// <summary>
/// 从 StreamingAssets/client_config.json 读取前端地址及模型配置。
/// 打包后可直接修改该 JSON 文件，无需重新打包。
/// </summary>
public class ClientConfig
{
    private const string FileName = "client_config.json";

    [JsonProperty("hostServerUrl")]
    public string HostServerUrl { get; set; } = "http://101.132.189.85:8084/GameProject/PC";

    [JsonProperty("wsUrl")]
    public string WsUrl { get; set; } = "ws://localhost:8080/ws";

    [JsonProperty("restApiUrl")]
    public string RestApiUrl { get; set; } = "http://localhost:8080/api";

    [JsonProperty("commonModels")]
    public List<string> CommonModels { get; set; } = new List<string> { "deepseek-v4-flash" };

    [JsonProperty("reflectModels")]
    public List<string> ReflectModels { get; set; } = new List<string> { "deepseek-v4-pro" };

    [JsonProperty("presetModes")]
    public List<string> PresetModes { get; set; } = new List<string>
    {
        "fam4_test_villain",
        "fam16_s9_hist_ming",
        "fam16_s9_hist_with_villain"
    };

   

    private static ClientConfig _instance;
    public static ClientConfig Instance
    {
        get
        {
            if (_instance == null)
                _instance = Load();
            return _instance;
        }
    }


    private static ClientConfig Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, FileName);
        if (!File.Exists(path))
        {
            LogManager.Instance.Warning($"[ClientConfig] 配置文件不存在，使用默认值: {path}");
            return new ClientConfig();
        }

        try
        {
            string json = File.ReadAllText(path);
            var cfg = JsonConvert.DeserializeObject<ClientConfig>(json) ?? new ClientConfig();
            LogManager.Instance.Info($"[ClientConfig] 加载成功: {path}");
            return cfg;
        }
        catch (System.Exception e)
        {
            LogManager.Instance.Error($"[ClientConfig] 解析失败，使用默认值: {e.Message}");
            return new ClientConfig();
        }
    }
}
