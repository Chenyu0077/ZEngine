# Dissolve2D Shader

## 原理

利用噪声纹理的亮度作为每个像素的"溶解判定值"，与阈值 `_DissolveThreshold` 比较：

- 噪声值 ≤ 阈值 → 该像素已溶解，`discard` 丢弃
- 噪声值 > 阈值 → 该像素存活
- 噪声值刚超过阈值（距离 < `_DissolveEdgeWidth`）→ 处于溶解边缘，渲染双色渐变边缘光

随着阈值从 0 增大到 1，越来越多的像素被丢弃，精灵逐渐"溶解"消失。

## 流程图

```
Fragment 输入 (uv, uvDissolve)
        │
        ▼
  采样精灵 Alpha
        │
        ▼
  Alpha ≤ Cutoff? ──Yes──► discard（精灵本身透明区）
        │
        No
        ▼
  采样噪声纹理 R 通道 → noise
        │
        ▼
  noise ≤ Threshold? ──Yes──► discard（已溶解区域）
        │
        No
        ▼
  edgeDist = noise - Threshold
        │
        ▼
  edgeDist < EdgeWidth? ──Yes──► 边缘渐变（EdgeColor1 → EdgeColor2）
        │
        No
        ▼
  返回原图色 (col × tint)
```

## 溶解过程示意

Threshold 从 0 → 1 递增：

```
Threshold = 0.0       Threshold = 0.4       Threshold = 0.8       Threshold = 1.0
┌─────────────┐      ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│             │      │   ░ ░ ░     │      │     ░       │      │             │
│   完整精灵   │  →   │ ░ 🔥 原图 ░ │  →   │       🔥   │  →   │   全部溶解   │
│             │      │   ░ ░ ░     │      │     ░       │      │             │
└─────────────┘      └─────────────┘      └─────────────┘      └─────────────┘

░ = 正在溶解的边缘（EdgeColor1 → EdgeColor2 渐变）
🔥 = 边缘发光区
空白 = 已溶解 / 透明
```

## Properties

| 属性 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `_MainTex` | 2D | white | `[PerRendererData]` 由 SpriteRenderer 自动注入 |
| `_Color` | Color | (1,1,1,1) | 原图 Tint 染色 |
| `_DissolveTex` | 2D | white | 噪声纹理，R 通道控制溶解模式 |
| `_DissolveThreshold` | Range(0,1) | 0 | 溶解进度，0=完整，1=完全溶解 |
| `_DissolveEdgeWidth` | Range(0,0.3) | 0.05 | 边缘发光宽度（噪声值空间） |
| `_EdgeColor1` | Color | (1,0.3,0,1) | 边缘内侧颜色（靠近溶解区，偏暗/红） |
| `_EdgeColor2` | Color | (1,1,0.3,1) | 边缘外侧颜色（靠近存活区，偏亮/黄） |
| `_AlphaCutoff` | Range(0.001,1) | 0.1 | 精灵 Alpha 阈值 |

## 渲染状态

| 设置 | 值 | 说明 |
|---|---|---|
| Blend | SrcAlpha OneMinusSrcAlpha | 标准半透明混合 |
| ZWrite | Off | 2D 不写深度 |
| Cull | Off | 双面渲染 |

## Shader 代码解析

### Vertex Shader

- `TRANSFORM_TEX(v.uv, _MainTex)` — 主纹理 UV（含 Tiling/Offset）
- `TRANSFORM_TEX(v.uv, _DissolveTex)` — 噪声纹理 UV（独立 Tiling/Offset，可调缩放控制溶解细节密度）
- `o.color = v.color * _Color` — 顶点色 × Tint 合并

### Fragment Shader

**Step 1** — 采样精灵贴图，Alpha ≤ Cutoff 则 `discard`

**Step 2** — 采样噪声纹理 R 通道，`noise ≤ Threshold` 则 `discard`

**Step 3** — 计算 `edgeDist = noise - Threshold`（当前像素距溶解边界的距离）

**Step 4** — `edgeDist < EdgeWidth` 时为边缘区域：

```hlsl
float t = edgeDist / _DissolveEdgeWidth; // 0→1, 从内侧到外侧
fixed4 edge = lerp(_EdgeColor1, _EdgeColor2, t);
```

- `t ≈ 0`：紧邻溶解边界，使用 `_EdgeColor1`（暗红，模拟烧焦）
- `t ≈ 1`：边缘外侧，使用 `_EdgeColor2`（亮黄，模拟高温发光）

**Step 5** — 非边缘区域，返回原图色 `col * i.color`

## 使用方式

1. 创建材质，选择 Shader `Game/Dissolve2D`
2. 将材质赋给 SpriteRenderer
3. 将一张噪声贴图拖入 `Dissolve Texture`（推荐 Perlin / Simplex 噪声）
4. 通过代码或动画控制 `Dissolve Threshold`（0→1 实现溶解，1→0 实现恢复）
5. 调整 `Edge Width` 控制边缘发光范围
6. 调整 `Edge Color 1/2` 自定义边缘颜色

### 代码控制示例

```csharp
// 溶解动画
material.SetFloat("_DissolveThreshold", progress); // 0~1
```