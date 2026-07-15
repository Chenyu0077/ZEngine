//------------------------------
// ZEngine
// 作者:
//------------------------------

using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.Timer;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class GameOverController : BaseController
    {
        private UIGameOverView _compt;
        private Timer _summaryPollTimer;
        private const float SummaryPollInterval = 3f;

        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as UIGameOverView;

            if (_compt != null)
            {
                _compt.m_ExitBtn.onClick.Add(OnExitBtnEvent);
                BindData(_view.Data as GameOverModel);
            }
        }

        private void BindData(GameOverModel model)
        {
            if (model?.GameStatus == null) return;

            var s = model.GameStatus;

            if (model.IsTaxFailure)
            {
                // 税败结局：只显示结局类型和问责措辞，不显示村民数据，不启动 summary 轮询
                _compt.m_scrollContent.m_content.text = $"结束类型：{FormatTitle(s.GameOverKind)}\n\n{s.GameOverReason}";
                return;
            }

            _compt.m_scrollContent.m_content.text =
                $"结束类型：{FormatTitle(s.GameOverKind)}\n" +
                $"{s.GameOverReason}\n\n" +
                $"存活村民：{s.Alive} 人\n" +
                $"死亡村民：{s.Dead} 人\n" +
                $"村民总数：{s.NPCCount} 人";

            RefreshSummary(s.GameOverSummary);

            // summary 异步生成中，轮询直到拿到内容
            if (string.IsNullOrEmpty(s.GameOverSummary) && s.GameOverKind != "manual_stop")
                StartSummaryPolling();
        }

        private void RefreshSummary(string summary)
        {
            if (_compt == null) return;
            if (string.IsNullOrEmpty(summary))
                _compt.m_scrollContent.m_content.text += "\n\n终局总结生成中…";
            else
                _compt.m_scrollContent.m_content.text += $"\n\n{summary}";
        }

        private void StartSummaryPolling()
        {
            _summaryPollTimer = TimerManager.Instance.CreatePepeatTimer(PollSummary, SummaryPollInterval, SummaryPollInterval);
        }

        private void PollSummary()
        {
            AIVillageClient.Instance.GetSimStatus(response =>
            {
                if (_compt == null) return;
                if (response == null || string.IsNullOrEmpty(response.GameOverSummary)) return;

                // 拿到总结，停止轮询并刷新内容
                _summaryPollTimer?.Kill();
                _summaryPollTimer = null;

                var s = response;
                _compt.m_scrollContent.m_content.text =
                    $"结束类型：{FormatTitle(s.GameOverKind)}\n" +
                    $"{s.GameOverReason}\n\n" +
                    $"存活村民：{s.Alive} 人\n" +
                    $"死亡村民：{s.Dead} 人\n" +
                    $"村民总数：{s.NPCCount} 人\n\n" +
                    s.GameOverSummary;
            }, null);
        }

        private static string FormatTitle(string kind) => kind switch
        {
            "player_dead"   => "村长身亡",
            "collapse"      => "村庄崩溃",
            "manual_stop"   => "模拟终止",
            "dismissed"     => "革职查办",
            "imprisoned"    => "锁拿下狱",
            "scapegoat"     => "充作替罪",
            _               => "游戏结束",
        };

        private void OnExitBtnEvent(EventContext context)
        {
            _summaryPollTimer?.Kill();
            _summaryPollTimer = null;

            var model = _view.Data as GameOverModel;
            // if (model?.IsTaxFailure == true)
            // {
            //     // 税败只是一个失败事件，关闭弹窗即可，不退出游戏
            //     UIManager.Instance.CloseView<GameOverView>();
            //     return;
            // }

            if (AIVillageClient.Instance.SimStatus != SimultionStatus.None)
            {
                AIVillageClient.Instance.StopSimulation(
                    (response) => { QuitGame(); },
                    (response) => { QuitGame(); }
                    );
            }
            else
            {
                QuitGame();
            }
        }

        private void QuitGame()
        {
            UIManager.Instance.CloseView<GameOverView>();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        public override void OnRelease()
        {
            _summaryPollTimer?.Kill();
            _summaryPollTimer = null;

            if (_compt != null)
            {
                _compt.m_ExitBtn.onClick.Remove(OnExitBtnEvent);
                _compt = null;
            }
            base.OnRelease();
        }
    }
}
