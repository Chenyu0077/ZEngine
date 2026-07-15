//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Hotfix.Logic.UI;
using Main.FuncModule.Camera2D;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ZEngine.AI.FSM;
using ZEngine.Manager.UI;
using ZEngine.Manager.UI.UGUI;

namespace Hotfix.Main.Logic.Nodes
{
    public class InitNode : IFsmNode
    {
        public string Name => "InitNode";

        public FiniteStateMachine SubFsm { get; }

        public void OnEnter()
        {
            // ── 初始化相机 ───────────────────────────────────────────────
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObj = new GameObject("Main Camera");
                cameraObj.AddComponent<Camera>();
            }
            else
            {
                if (camera.GetComponent<Camera2DController>() == null)
                {
                    var controller = camera.gameObject.AddComponent<Camera2DController>();
                    controller.SetMode(Camera2DMode.GodView);
                }
            }
            var stageCamera = FairyGUI.StageCamera.main; 
            var cameraData = stageCamera.GetUniversalAdditionalCameraData(); 
            cameraData.renderType = CameraRenderType.Overlay; 
            if(camera.GetUniversalAdditionalCameraData() == null)
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camera.GetUniversalAdditionalCameraData().cameraStack.Add(stageCamera);
            GameObject.DontDestroyOnLoad(stageCamera.gameObject);

            // 打开MainUI
            UUIManager.Instance.OpenViewSync<ComponentTestView>();
            HotUpdateProgressUI.Instance.Hide();
        }


        public void OnUpdate()
        {

        }

        public void OnFixedUpdate()
        {

        }

        public void OnExit()
        {

        }

        public void OnHandleMessage(object msg)
        {

        }
    }
}
