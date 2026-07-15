# HitFlash2D Shader

## 原理

混合精灵原图色与闪白颜色，通过 `_FlashAmount`（0→1）控制混合比例：

- `_FlashAmount = 0` → 完全显示原图
- `_FlashAmount = 1` → 完全替换为闪白色（保留原图 Alpha 轮廓）

混合公式：

```hlsl
flash = lerp(base.rgb, _FlashColor.rgb * base.a, _FlashAmount)
```

关键细节：闪白色乘以 `base.a` 而非直接使用 `_FlashColor.rgb`，确保半透明像素（如抗锯齿边缘）的闪白不会溢出精灵轮廓。

## 流程图

```
Fragment 输入 (uv)
        │
        ▼
  采样精灵贴图 col
        │
        ▼
  Alpha ≤ Cutoff? ──Yes──► discard
        │
        No
        ▼
  base = col × tint
        │
        ▼
  flash = lerp(base.rgb, FlashColor.rgb × base.a, FlashAmount)
        │
        ▼
  输出 (flash, base.a)
```

## 混合效果示意

```
FlashAmount = 0.0      FlashAmount = 0.5       FlashAmount = 1.0
┌─────────────┐       ┌─────────────┐        ┌─────────────┐
│             │       │   ░ ░ ░     │        │ ███████████ │
│  原始精灵    │  →    │  ░ 原图 ░   │   →    │ ███████████ │
│             │       │   ░ ░ ░     │        │ ███████████ │
└─────────────┘       └─────────────┘        └─────────────┘
  完全原图             半透明混合               完全闪白（保留轮廓）
```

## Properties

| 属性 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `_MainTex` | 2D | white | `[PerRendererData]` 由 SpriteRenderer 自动注入 |
| `_Color` | Color | (1,1,1,1) | 原图 Tint 染色 |
| `_FlashColor` | Color | (1,1,1,1) | 受击闪白颜色，默认纯白 |
| `_FlashAmount` | Range(0,1) | 0 | 闪白混合度，0=原图，1=全闪白 |
| `_AlphaCutoff` | Range(0.001,1) | 0.1 | 精灵 Alpha 阈值 |

## 渲染状态

| 设置 | 值 | 说明 |
|---|---|---|
| Blend | SrcAlpha OneMinusSrcAlpha | 标准半透明混合 |
| ZWrite | Off | 2D 不写深度 |
| Cull | Off | 双面渲染 |

## 使用方式

1. 创建材质，选择 Shader `Game/HitFlash2D`
2. 将材质赋给 SpriteRenderer
3. 通过代码控制 `_FlashAmount` 驱动闪白动画

### 代码控制示例

```csharp
using UnityEngine;
using System.Collections;

public class HitFlash : MonoBehaviour
{
    private Material _material;
    private Coroutine _flashCoroutine;

    void Awake()
    {
        _material = GetComponent<SpriteRenderer>().material;
    }

    public void PlayFlash(float duration = 0.15f)
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine(duration));
    }

    IEnumerator FlashRoutine(float duration)
    {
        _material.SetFloat("_FlashAmount", 1f);
        yield return new WaitForSeconds(duration);
        _material.SetFloat("_FlashAmount", 0f);
        _flashCoroutine = null;
    }
}
```

### 进阶：缓动闪白

```csharp
IEnumerator FlashRoutine(float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        // 先快闪再渐退
        float amount = 1f - Mathf.Pow(t, 2f);
        _material.SetFloat("_FlashAmount", amount);
        yield return null;
    }
    _material.SetFloat("_FlashAmount", 0f);
}
```