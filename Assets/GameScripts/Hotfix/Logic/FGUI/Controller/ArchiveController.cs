//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using FairyGUI;
using Hotfix.FuncModule;
using System;
using System.Collections.Generic;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.UI;
using ZEngine.Module.Archive;

namespace Hotfix.Main.UI
{
    public class ArchiveController : BaseController
    {
        private UIArchiveView compt;
        private List<SlotInfo> slotInfos;

        public override void Initialize()
        {
            base.Initialize();
            compt = _view.GetView() as UIArchiveView;
            slotInfos = ArchiveManager.Instance.GetAllSlotInfos();

            if (compt != null)
            {
                compt.m_CloseBtn.onClick.Add(OnCloseBtnEvent);
                compt.m_SaveList.itemRenderer = RenderListItem;
                compt.m_SaveList.SetVirtual();
                compt.m_addBtn.onClick.Add(OnAddBtnEvent);
            }

            compt.m_SaveList.numItems = slotInfos.Count;

            _view.OnChanged = (data) =>
            {

            };
        }


        private void OnCloseBtnEvent(EventContext context)
        {
            Debug.Log("关闭介绍界面");
            UIManager.Instance.CloseView<ArchiveView>();
        }

        private void RenderListItem(int index, GObject item)
        {
            // 这里可以根据 index 设置 item 的内容
            // 例如设置文本、图片等
            var saveItem = item as UIArchiveCom;
            saveItem.onClick.Add((context) => OnSaveItemClick(index));
            saveItem.m_showInfo.text = $"存档 {slotInfos[index].SlotName}-{slotInfos[index].ID}-{slotInfos[index].SaveTime}";
        }

        private void OnSaveItemClick(int index)
        {
            Debug.Log($"点击了存档 {index}");
            // 这里可以添加点击存档的逻辑
        }


        private void OnAddBtnEvent(EventContext context)
        {
            var slotInfo = ArchiveManager.Instance.CreateNewSlot<ArchiveSlot>();
            slotInfos = ArchiveManager.Instance.GetAllSlotInfos();
            compt.m_SaveList.numItems = ArchiveManager.Instance.GetAllSlotInfos().Count;
        }

        public override void OnRelease()
        {
            if (compt != null)
            {
                compt.m_CloseBtn.onClick.Remove(OnCloseBtnEvent);
                compt.m_addBtn.onClick.Remove(OnAddBtnEvent);
                compt = null;
            }
            base.OnRelease();
        }
    }
}
