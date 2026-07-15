//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 原始图像包装：用于显示 Texture/RenderTexture 的 RawImage 组件。
    /// 适用于头像、技能图标（动态生成的纹理）、相机渲染目标等场景。
    /// 用法：rawImage.SetTexture(myTexture); rawImage.SetUV(new Rect(0,0,1,1));
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class UIRawImage : UIComponentBase
    {
        protected RawImage _image;
        protected RawImage Img => _image != null ? _image : (_image = GetComponent<RawImage>());

        public void SetTexture(Texture texture)
        {
            if (Img != null) Img.texture = texture;
        }

        public void SetColor(Color color)
        {
            if (Img != null) Img.color = color;
        }

        /// <summary>设置 UV 裁剪区域（用于精灵图集等）。</summary>
        public void SetUV(Rect uvRect)
        {
            if (Img != null) Img.uvRect = uvRect;
        }

        public void SetActive(bool flag)
        {
            if (Img != null) Img.gameObject.SetActive(flag);
        }

        public void SetMaterial(Material mat)
        {
            if (Img != null) Img.material = mat;
        }
    }
}
