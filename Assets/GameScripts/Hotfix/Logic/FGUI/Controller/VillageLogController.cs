using System.Text;
using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.FuncModule.Village;
using Hotfix.Main.Leiya;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.Log;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    /// <summary>
    /// 村志面板：打开时自动拉取最新村志，按天格式化到滚动富文本中。
    /// FGUI 组件：UIVillageLogView（VillageLog 包 / VillageLogView）
    ///   m_mask    — GGraph  背景遮罩（点击关闭）
    ///   m_back    — GGraph  面板背景
    ///   m_content — UIScrollText → m_content: GRichTextField  正文
    /// </summary>
    public class VillageLogController : BaseController
    {
        #region 字段

        private UIVillageLogView _compt;
        private VillageLogModel  _model;

        #endregion

        #region 生命周期

        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as UIVillageLogView;
            _model = _view.Data as VillageLogModel;
            if (_compt == null) return;

            BindEvents();
            ShowLoading();
            FetchChronicle();
        }

        public override void OnRelease()
        {
            if (_compt?.m_mask != null)
                _compt.m_mask.onClick.Remove(OnMaskClick);

            _compt = null;
            _model = null;
            base.OnRelease();
        }

        #endregion


        private void BindEvents()
        {
            if (_compt.m_mask != null)
                _compt.m_mask.onClick.Add(OnMaskClick);
        }

        private void ShowLoading()
        {
            SetContent("<font color='#aaaaaa'>正在加载村志...</font>");
        }


        // 数据拉取
        private void FetchChronicle()
        {
            string runId = _model?.RunId;

            // 首先获取当前游戏时间来判断天数
            AIVillageClient.Instance.GetTime(
                onSuccess: (timeResponse) =>
                {
                    // 尝试从缓存中获取村志数据
                    var cachedChronicle = VillageChronicleCache.Instance.GetCachedChronicle(runId, timeResponse.Day);
                    if (cachedChronicle != null)
                    {
                        LogManager.Instance.Info($"[VillageLog] 使用缓存的村志数据: 第{timeResponse.Day}天");
                        if (_model != null) _model.Chronicle = cachedChronicle;
                        RenderChronicle(cachedChronicle);
                        return;
                    }

                    // 缓存中没有数据或天数不匹配，从服务器获取
                    LogManager.Instance.Info($"[VillageLog] 缓存未命中，从服务器获取村志: 第{timeResponse.Day}天");
                    FetchChronicleFromServer(runId, timeResponse.Day);
                },
                onError: (error) =>
                {
                    LogManager.Instance.Warning($"[VillageLog] 获取游戏时间失败，直接从服务器获取村志: {error}");
                    // 如果获取时间失败，直接从服务器获取村志（兼容性处理）
                    FetchChronicleFromServer(runId, 1);
                });
        }

        /// <summary>
        /// 从服务器获取村志数据并缓存
        /// </summary>
        /// <param name="runId">运行ID</param>
        /// <param name="currentDay">当前天数，-1表示未知</param>
        private void FetchChronicleFromServer(string runId, int currentDay)
        {
            AIVillageClient.Instance.GetChronicle(runId,
                onSuccess: (response) =>
                {
                    if (_model != null) _model.Chronicle = response;
                    RenderChronicle(response);

                    // 保存到缓存（如果有有效的天数信息）
                    if (currentDay > 0 && response != null && response.Success)
                    {
                        VillageChronicleCache.Instance.SaveChronicleToCache(runId, response);
                        LogManager.Instance.Info($"[VillageLog] 村志已保存到缓存: 共{currentDay}天");
                    }
                },
                onError: (error) =>
                {
                    LogManager.Instance.Error($"[VillageLog] 村志加载失败: {error}");
                    SetContent("<font color='#ff6666'>村志加载失败，请稍后重试。</font>");
                });
        }


        #region 渲染

        private void RenderChronicle(ChronicleResponse response)
        {
            if (response == null || !response.Success)
            {
                string hint = response?.Message ?? "村志加载失败";
                SetContent($"<font color='#ff6666'>{EscapeHtml(hint)}</font>");
                return;
            }

            if (response.Days == null || response.Days.Count == 0)
            {
                // 空状态显示
                var emptyContent = @"
<font color='#ebecf3'>暂无村志，模拟开始后，村志将自动生成</font>
                ";
                SetContent(emptyContent);
                return;
            }

            var sb = new StringBuilder();

            // 添加头部说明
            sb.Append("<font color='#6c7086' size='16'><i>本卷如实记载村庄逐日运行实况，不作评判，以供后查。</i></font>\n\n");

            // 倒序显示（最新的在上面）
            for (int i = response.Days.Count - 1; i >= 0; i--)
            {
                var day = response.Days[i];
                bool hasDeath = day.Deaths != null && day.Deaths.Count > 0;

                // 日期标题 - 有死亡事件时显示特殊标记
                string deathIcon = hasDeath ? " ☠" : "";
                string titleColor = hasDeath ? "#f38ba8" : "#c9a96e";
                sb.Append($"<b><font size='24' color='{titleColor}'> 第 {day.Day} 天{deathIcon}</font></b>\n");

                // 事件纪实
                if (!string.IsNullOrEmpty(day.Chronicle))
                {
                    sb.Append($"<font color='#89b4fa' size='20'>上篇 · 事件纪实</font>\n");
                    sb.Append($"<font color='#bac2de' size='16'>{EscapeHtml(day.Chronicle)}</font>\n\n");
                }

                // 社会动态
                if (!string.IsNullOrEmpty(day.Analysis))
                {
                    sb.Append($"<font color='#a6e3a1' size='20'>下篇 · 社会动态</font>\n");
                    sb.Append($"<font color='#bac2de' size='16'>{EscapeHtml(day.Analysis)}</font>\n\n");
                }

                // 死亡记录
                if (hasDeath)
                {
                    sb.Append($"<font color='#f38ba8' size='20'>本日讣告</font>\n");
                    foreach (var death in day.Deaths)
                    {
                        sb.Append($"<font color='#f5c2c7' size='16'><b>{EscapeHtml(death.Name)}</b>：{EscapeHtml(death.Cause)}</font>\n");
                    }
                    sb.Append("\n");
                }

                // 天数分隔
                sb.Append("────────────────────────\n\n");

                Debug.Log($"{sb.ToString()}");
            }
            Debug.Log($"TotalContent: {sb.ToString()}");

            SetContent(sb.ToString());
        }

        private void SetContent(string richText)
        {
            if (_compt?.m_content?.m_content == null) return;
            _compt.m_content.m_content.text = richText;
            // 滚动到顶部
            _compt.m_content.scrollPane?.ScrollToView(_compt.m_content.m_content, false);
        }

        private static string EscapeHtml(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        #endregion

        #region 事件处理

        private void OnMaskClick(EventContext context)
        {
            UIManager.Instance.CloseView<VillageLogView>();
        }

        #endregion
    }
}
