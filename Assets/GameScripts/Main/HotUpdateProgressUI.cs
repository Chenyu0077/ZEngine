using Main.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 热更新进度条 UI，挂在 GameLauncher 场景的同名 GameObject 上。
/// 全部用代码创建，不依赖任何 Asset 资源，确保在 YooAsset 初始化前即可显示。
/// </summary>
public class HotUpdateProgressUI : BehaviourSingleton<HotUpdateProgressUI>
{
    [Header("UI 引用（场景中搭建后拖入）")]
    public GameObject root;         // 整个面板根节点，完成后隐藏
    public Slider progressBar;      // 进度条 Slider
    public Text progressText;       // 百分比文本，如 "37%"
    public Text tipText;            // 提示文本，如 "正在更新资源..."
    public Text detailText;         // 详细信息，如 "12.3 MB / 56.7 MB"

    private void Awake()
    {
        SetVisible(false);
    }

    /// <summary>
    /// 开始下载前调用，显示进度条并初始化。
    /// </summary>
    public void Show(string tip = "正在更新资源，请稍候...")
    {
        SetVisible(true);
        if (tipText) tipText.text = tip;
        SetProgress(0, 0, 0);
    }

    /// <summary>
    /// 更新进度，由 GameLauncher 通过回调驱动。
    /// </summary>
    /// <param name="progress">0~1</param>
    /// <param name="downloadedBytes">已下载字节</param>
    /// <param name="totalBytes">总字节</param>
    public void SetProgress(float progress, long downloadedBytes, long totalBytes)
    {
        if (progressBar) progressBar.value = progress;
        if (progressText) progressText.text = $"{(int)(progress * 100)}%";
        if (detailText && totalBytes > 0)
            detailText.text = $"{FormatBytes(downloadedBytes)} / {FormatBytes(totalBytes)}";
        else if (detailText)
            detailText.text = string.Empty;
    }

    /// <summary>
    /// 仅更新提示文字，不改变进度条（用于无字节进度的阶段，如版本检查、解压）。
    /// </summary>
    public void SetTip(string tip)
    {
        if (tipText) tipText.text = tip;
    }

    /// <summary>
    /// 下载完成后调用，隐藏面板。
    /// </summary>
    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (root) root.SetActive(visible);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024f * 1024f):F1} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024f:F1} KB";
        return $"{bytes} B";
    }
}
