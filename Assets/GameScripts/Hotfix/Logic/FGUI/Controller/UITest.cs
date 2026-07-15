using System;
using FairyGUI;
using UnityEngine;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class UITest : MonoBehaviour
    {
        [SerializeField] private bool isOpen = true;
        private NPCMainView view;
        private GameControlView controlView;
        private GameConfigView configView;
        
        private void Update()
        {
            if (!isOpen) return;
            
            if (Input.GetMouseButton(0))
            {
                view = UIManager.Instance.OpenViewSync<NPCMainView>();
                //controlView = UIManager.Instance.OpenViewSync<GameControlView>();
                //configView = UIManager.Instance.OpenViewSync<GameConfigView>();
                //view.GetView().DOMoveY(1000f, 5f);
            }

            if (Input.GetMouseButton(1))
            {
                //view?.GetView().DOMoveY(-600f, 5f);
            }
        }

        private void OnDestroy()
        {
            view?.GetView().DOKill();
        }
    }
}