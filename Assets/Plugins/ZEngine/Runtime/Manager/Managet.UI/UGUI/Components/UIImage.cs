//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using UnityEngine.UI;
using YooAsset;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 图片 UI 组件：包装原生 Image，提供精灵/颜色/填充度接口，
    /// 并支持按资源路径同步加载精灵（句柄自管理，替换/销毁时释放）。
    /// 用法：预制体子节点上挂 Image + 本组件，View 内 [UIBind("path")] UIImage img;，
    /// img.SetSpriteByPath("Icon/coin");
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIImage : UIComponentBase
    {
        protected Image _image;
        // 懒解析：Awake 未执行时也能正确工作
        protected Image Img => _image != null ? _image : (_image = GetComponent<Image>());
        private AssetHandle _spriteHandle;

        protected virtual void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void SetSprite(Sprite sprite)
        {
            var img = Img;
            if (img != null)
                img.sprite = sprite;
        }

        public void SetColor(Color color)
        {
            var img = Img;
            if (img != null)
                img.color = color;
        }

        public void SetFillAmount(float amount)
        {
            var img = Img;
            if (img != null)
                img.fillAmount = Mathf.Clamp01(amount);
        }

        public void SetNativeSize()
        {
            var img = Img;
            if (img != null)
                img.SetNativeSize();
        }

        /// <summary>是否参与射线检测（遮罩非模态时关闭以放行下层点击）</summary>
        public void SetRaycastTarget(bool flag)
        {
            var img = Img;
            if (img != null)
                img.raycastTarget = flag;
        }

        public void SetActive(bool flag)
        {
            var img = Img;
            if (img != null)
                img.gameObject.SetActive(flag);
        }

        /// <summary>
        /// 按资源路径同步加载精灵并赋值。加载前会释放上一张精灵的句柄。
        /// 注意：ResourceManager 须已初始化，否则抛异常（由调用方保证时序）。
        /// </summary>
        public void SetSpriteByPath(string location)
        {
            ReleaseSpriteHandle();
            _spriteHandle = ResourceManager.Instance.LoadAssetSync<Sprite>(location);
            if (_spriteHandle != null && _spriteHandle.AssetObject is Sprite sprite)
                _image.sprite = sprite;
        }

        private void ReleaseSpriteHandle()
        {
            if (_spriteHandle != null)
            {
                ResourceManager.Instance.Release(_spriteHandle);
                _spriteHandle = null;
            }
        }

        protected virtual void OnDestroy()
        {
            // 自管理句柄：组件销毁时释放，避免资源泄漏（与宿主 View 释放路径解耦）
            ReleaseSpriteHandle();
        }
    }
}
