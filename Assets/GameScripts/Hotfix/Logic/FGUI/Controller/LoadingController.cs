//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class LoadingController : BaseController
    {
        private UILoadingView compt;
        private LoadingModel model;
        private float progress = 0;
        private float initTime = 0;
        private const float preLoadingTime = 3f;    //预加载时间

        public override void Initialize()
        {
            base.Initialize();
            compt = _view.GetView() as UILoadingView;
            model = _view.Data as LoadingModel;

            if (compt != null)
            {
                compt.m_ProgressBar.onChanged.Add(OnProgressBarChanged);
            }

            _view.OnChanged = (data) =>
            {

            };
        }


        private void OnProgressBarChanged(EventContext context)
        {
            
        }

        public override void OnUpdate()
        {
            if(initTime < preLoadingTime * 0.9f)
            {
                if (progress >= 0.9f && !model.CanLoaded)
                    return;
                
                initTime += Time.deltaTime;
                progress = initTime / preLoadingTime;
                compt.m_ProgressValue.text = $"{(int)(progress * 100)}%"; // 更新进度文本
                compt.m_ProgressBar.value = progress * 100; // 更新进度条
            }
            else
            {
                // 2.7秒假进度跑完后，硬闸：场景未就绪，则卡在 99%
                if (model == null || !model.CanLoaded)
                {
                    compt.m_TipText.text = "等待中...";
                    return;
                }
                
                initTime += Time.deltaTime;
                progress = initTime / preLoadingTime;
                compt.m_ProgressValue.text = $"{(int)(progress * 100)}%"; // 更新进度文本
                compt.m_ProgressBar.value = progress * 100; // 更新进度条
                if (progress >= 1f)
                {
                    model.OnLoadingCompleted?.Invoke();
                    _view.CanRemoved = true; // 设置视图可以被移除
                }
            }
        }

        public override void OnRelease()
        {
            if (compt != null)
            {
                compt.m_ProgressBar.onChanged.Remove(OnProgressBarChanged);
                compt = null;
            }
            base.OnRelease();
        }
    }
}
