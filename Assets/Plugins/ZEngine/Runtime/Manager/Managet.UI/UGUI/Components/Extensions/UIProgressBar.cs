//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 进度条：常用作血条、加载条、经验条等。
    /// 底层用 Image(fillMethod=Filled) 驱动填充，可选文本百分比显示。
    /// 子层级约定：Background(Image) + Fill(Image, fillMethod) + Text(TMP, 可选)
    /// 用法：progressBar.SetProgress(0.75f);  含插值动画: progressBar.AnimateTo(0.75f, 0.3f);
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIProgressBar : UIComponentBase
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private TextMeshProUGUI _valueText;

        // 注意：_fillImage 需要 fillMethod = Horizontal / Vertical 且 Image.type = Filled
        // Prefab 创建时已在子节点 Fill 上预设（或由工厂方法设置）

        protected virtual void Awake()
        {
            // 尝试从子节点自动查找
            if (_fillImage == null)
            {
                var fill = transform.Find("Fill");
                if (fill != null) _fillImage = fill.GetComponent<Image>();
            }
            if (_valueText == null)
            {
                var txt = transform.Find("Text");
                if (txt != null) _valueText = txt.GetComponent<TextMeshProUGUI>();
            }
        }

        private DG.Tweening.Tweener _tween;

        /// <summary>设置进度 (0~1)。不带动画，并 kill 进行中的动画。</summary>
        public void SetProgress(float progress)
        {
            // Kill 未完成的 tween（DOTween 核心静态方法，不依赖 UI 模块扩展）
            if (_tween != null)
                DG.Tweening.DOTween.Kill(_tween);
            var v = Mathf.Clamp01(progress);
            if (_fillImage != null)
                _fillImage.fillAmount = v;
            if (_valueText != null)
                _valueText.text = Mathf.RoundToInt(v * 100) + "%";
        }

        /// <summary>插值动画到目标值。使用 DOTween 核心 API（DOTween.Kill 静态方法，不依赖 UI 模块扩展）。</summary>
        public DG.Tweening.Tweener AnimateTo(float target, float duration)
        {
            if (_tween != null)
                DG.Tweening.DOTween.Kill(_tween);
            var to = Mathf.Clamp01(target);
            _tween = DG.Tweening.DOTween.To(
                () => _fillImage != null ? _fillImage.fillAmount : 0f,
                v => {
                    if (_fillImage != null) _fillImage.fillAmount = v;
                    if (_valueText != null) _valueText.text = Mathf.RoundToInt(v * 100) + "%";
                },
                to, duration);
            return _tween;
        }

        public float Progress => _fillImage != null ? _fillImage.fillAmount : 0f;

        public override void OnRelease()
        {
            // kill 未完成的 tween，避免释放后残留（setter 内有 null 守卫，但主动 kill 更干净）
            if (_tween != null)
                DG.Tweening.DOTween.Kill(_tween);
            _tween = null;
        }
    }
}
