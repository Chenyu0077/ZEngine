using System.Collections.Generic;
using FairyGUI;
using Hotfix.FuncModule.AITown.Command;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using UnityEngine.InputSystem;
using ZEngine.Manager.Mouse;
using ZEngine.Manager.UI;
using ZEngine.Module.Input;

namespace Hotfix.Main.UI
{
    public class NPCCommandController : BaseController
    {
        private UINPCCommandView _compt;
        private NPCCommandModel  _model;

        private readonly List<string> _history = new List<string>();
        private int _historyIndex = -1;

        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as UINPCCommandView;
            _model = _view.Data as NPCCommandModel;

            if (_compt == null) return;

            _compt.m_BtnSend.onClick.Add(OnSend);
            _compt.m_BtnClose.onClick.Add(OnClose);
            _compt.m_Input.onSubmit.Add(OnInputSubmit);
            _compt.m_Input.onChanged.Add(OnInputChanged);
            _compt.m_HistoryList.itemRenderer = RenderHistory;

            // 初始提示
            SetHint(string.Empty);
            _compt.m_ResultText.text = string.Empty;

            if (_model != null && !string.IsNullOrEmpty(_model.AgentName) && _compt.m_Title != null)
                _compt.m_Title.text = "NPC 命令 — " + _model.AgentName;

            // 输入框获得焦点
            _compt.m_Input.promptText = "输入命令...";
            _compt.m_Input.RequestFocus();
            
            _view.OnChanged = UpdatePanel;
            UpdatePanel(_model);
        }

        private void UpdatePanel(BaseModel data)
        {
            if (data is NPCCommandModel commandModel)
            {
                _model = commandModel;
                if (!string.IsNullOrEmpty(commandModel.AgentName) && _compt.m_Title != null)
                    _compt.m_Title.text = "NPC 命令 — " + commandModel.AgentName;
            }
        }


        private void OnInputSubmit(EventContext ctx)
        {
            ExecuteSend();
        }
        
        public override void OnUpdate()
        {
            if (_compt == null || !_compt.m_Input.focused) return;

            if (InputManager.GetKeyDown(Key.UpArrow))
                NavigateHistory(1);
            else if (InputManager.GetKeyDown(Key.DownArrow))
                NavigateHistory(-1);
        }

        private void OnInputChanged(EventContext ctx)
        {
            SetHint(_compt.m_Input.text);
        }

        private void OnSend(EventContext ctx)
        {
            ExecuteSend();
        }

        private void OnClose(EventContext ctx)
        {
            UIManager.Instance.CloseView<NPCCommandView>();
        }

        

        private void ExecuteSend()
        {
            string input = _compt.m_Input.text?.Trim();
            if (string.IsNullOrEmpty(input)) return;

            // 加入历史
            _history.Insert(0, input);
            if (_history.Count > 20) _history.RemoveAt(_history.Count - 1);
            _historyIndex = -1;

            // 刷新历史列表
            RefreshHistoryList();

            // 清空输入框
            _compt.m_Input.text = string.Empty;
            SetHint(string.Empty);

            // 解析并执行
            var result = CommandParser.Parse(input, _model?.Agent);

            if (result.Success && result.Task != null)
            {
                if (_model?.Agent != null)
                {
                    _model.Agent.TaskRunner.EnqueueTask(result.Task);
                    _compt.m_ResultText.text = $"[color=#00ff00][OK] 任务已入队: {result.Task.Type}[/color]";
                }
                else
                {
                    _compt.m_ResultText.text = "[color=#ff4444][ERR] 未绑定 NPC，无法执行任务[/color]";
                }
            }
            else if (result.Success && result.Info != null)
            {
                _compt.m_ResultText.text = $"[color=#44bbff][INFO]\n{result.Info}[/color]";
            }
            else
            {
                _compt.m_ResultText.text = $"[color=#ff4444][ERR] {result.Error}[/color]";
            }
        }

        // 设置提示词
        private void SetHint(string inputText)
        {
            if (_compt.m_HintText == null) return;

            if (string.IsNullOrWhiteSpace(inputText))
            {
                _compt.m_HintText.m_content.text = "输入命令名查看用法，例: move 10 5";
                return;
            }

            string token = inputText.Split(' ')[0];
            var def = CommandRegistry.Find(token);
            if (def != null)
            {
                if (def.Name == "help")
                    _compt.m_HintText.m_content.text = CommandRegistry.BuildHelpText();
                else    
                    _compt.m_HintText.m_content.text = def.Usage + "\n" + def.Description;
            }
            else
                _compt.m_HintText.m_content.text = "输入命令名查看用法，例: help";
        }

        // 追踪历史记录
        private void NavigateHistory(int direction)
        {
            if (_history.Count == 0) return;

            _historyIndex = Mathf.Clamp(_historyIndex + direction, -1, _history.Count - 1);
            _compt.m_Input.text = _historyIndex >= 0 ? _history[_historyIndex] : string.Empty;
        }

        private void RefreshHistoryList()
        {
            if (_compt.m_HistoryList == null) return;
            _compt.m_HistoryList.numItems = _history.Count;
        }

        private void RenderHistory(int index, GObject item)
        {
            if (index < 0 || index >= _history.Count) return;

            if (item is UIHistoryItem historyItem)
            {
                historyItem.m_content.text = "> " + _history[index];   
            }
        }


        // ── 释放 ────────────────────────────────────────────────────────────

        public override void OnRelease()
        {
            if (_compt != null)
            {
                _compt.m_BtnSend.onClick.Remove(OnSend);
                _compt.m_BtnClose.onClick.Remove(OnClose);
                _compt.m_Input.onSubmit.Remove(OnInputSubmit);
                _compt.m_Input.onChanged.Remove(OnInputChanged);
                _compt.m_HistoryList.itemRenderer = null;
                _compt = null;
            }
            _view.OnChanged = null;
            _history.Clear();
            base.OnRelease();
        }
    }
}
