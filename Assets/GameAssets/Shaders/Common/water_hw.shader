// ============================================================
//  water2d/waterg_hw  ——  2D 水体手写 Shader（URP Sprite Lit，3D URP 渲染器版）
//
//  从 Shader Graph 生成代码反向重构，保留所有功能，去除冗余代码。
//  主要功能层：
//    1. 像素完美 UV & 视差滚动
//    2. 扭曲（流动贴图 + 模拟法线）
//    3. 三层梯度噪声泡沫
//    4. 太阳光条纹（焦散效果）
//    5. 水体模拟（涟漪法线 & 泡沫）
//    6. 障碍物描边
//    7. 反射（横版翻转 / 俯视角 / 光线步进）
//    8. 深度颜色混合
//    9. 水面下背景贴图 & 水面纹理
//   10. 动态波浪边缘
// ============================================================
Shader "water2d/waterg_hw"
{
    Properties
    {
        // ── 基础贴图 ──────────────────────────────────────
        [NoScaleOffset] _MainTex       ("主贴图（用于像素完美UV计算）", 2D) = "white" {}
        [NoScaleOffset] _alphaTexture  ("透明度遮罩贴图", 2D) = "white" {}

        // ── 颜色与深度 ────────────────────────────────────
        _color          ("水面颜色（浅处/边缘）", Color)   = (0.4986205, 0.7830189, 0.7474691, 0)
        _deep_color     ("水面颜色（深处/中心）", Color)   = (0.06274509, 0.1895248, 0.4039216, 0)
        _deep_minmax    ("深度映射范围 (浅端值, 深端值)", Vector) = (0.29, 0.99, 0, 0)
        _surfaceAlpha   ("水面整体透明度", Float) = 1
        // 颜色模式：2 = Y轴线性渐变；其他 = 从中心径向渐变
        _color_type     ("颜色模式 (2=Y轴, 其他=径向)", Float) = 3
        [NoScaleOffset] _colorGradient ("深度颜色渐变贴图（depthFromObstructors模式）", 2D) = "white" {}
        _depthMlp       ("深度贴图亮度倍率", Float) = 16

        // ── UV 与像素完美 ──────────────────────────────────
        _tiling         ("水体UV平铺 (X, Y)", Vector) = (1, 1, 0, 0)
        _num_of_pixels  ("像素完美分辨率（像素数量）", Float) = 128
        [ToggleUI] _pixel_perfect ("开启像素完美模式", Float) = 1

        // ── 玩家视差滚动 ──────────────────────────────────
        _playerPosition ("玩家世界坐标（由脚本注入）", Vector) = (0, 0, 0, 0)
        _scrStrength    ("玩家视差强度", Float) = 1
        [ToggleUI] _enable_scrolling ("开启玩家视差滚动", Float) = 1

        // ── 扭曲 ──────────────────────────────────────────
        [NoScaleOffset] _distortion_tex ("扭曲贴图（RG通道驱动XY偏移）", 2D) = "white" {}
        _distortion_tiling    ("扭曲UV平铺 (X, Y)", Vector) = (1, 1, 0, 0)
        _distortion_speed     ("扭曲流动速度 (X, Y)", Vector) = (0.1, 0, 0, 0)
        _distortion_strength  ("扭曲强度 (X, Y)", Vector) = (0.1, 0.05, 0, 0)
        _distortion_color     ("扭曲高光颜色（水面顶部光效）", Color) = (0, 0, 0, 0)
        _distortion_minmax    ("扭曲高光显示范围 (最小, 最大)", Vector) = (0, 0.1, 0, 0)
        // FPRH = Flatten/Prevent Reflection Height：防止反射UV超出水面顶部
        [ToggleUI] _distortionFPRH ("扭曲FPRH模式（防穿透水面顶部）", Float) = 0

        // ── 泡沫 ──────────────────────────────────────────
        _foam_color   ("泡沫颜色", Color) = (0.8679245, 0.749199, 0.749199, 0)
        _foam_size    ("泡沫大小（梯度噪声频率倍率）", Float) = 1
        _foam_density ("泡沫密度", Range(0, 1)) = 0.4
        _foam_alpha   ("泡沫透明度", Range(0, 1)) = 0.7
        _foam_speed   ("泡沫流动速度 (X, Y)", Vector) = (0.3, 0.1, 0, 0)

        // ── 太阳光条纹 ────────────────────────────────────
        [NoScaleOffset] _sun_strips ("太阳光条纹贴图", 2D) = "white" {}
        _strips_density        ("条纹密度（梯度噪声缩放）", Float) = 6
        _strips_size           ("条纹大小", Float) = 1
        _strips_speed          ("条纹速度倍率", Float) = 3
        _strips_scrolling_speed("条纹独立滚动速度", Float) = 0
        _strips_alpha          ("条纹透明度", Range(0, 1)) = 1

        // ── 水体模拟 ──────────────────────────────────────
        [NoScaleOffset] _simTex        ("模拟贴图（由系统写入波纹数据）", 2D) = "white" {}
        _simFoamColor                  ("模拟波浪泡沫颜色", Color) = (1, 1, 1, 1)
        _simMinMaxWavesHeightFoam      ("模拟泡沫高度范围 (最小, 最大)", Vector) = (0.2, 0.6, 0, 0)
        _normStr       ("模拟法线强度（影响扭曲扰动量）", Float) = 1
        // _simUvs 由系统脚本注入，指定模拟贴图在水体UV空间中的覆盖范围
        [HideInInspector] _enable_sim  ("开启水体模拟", Float) = 1

        // ── 反射 ──────────────────────────────────────────
        _reflectionY      ("横版反射Y翻转点（0=底部, 1=顶部）", Float) = 0.7
        _reflectionsColor ("反射整体色调", Color) = (1, 1, 1, 1)
        [ToggleUI] _enable_ref ("开启反射", Float) = 1
        [ToggleUI] _enable_rm  ("开启光线步进反射", Float) = 0
        _raymarchSteps         ("光线步进步数（越大越精确）", Float) = 32
        [ToggleUI] _rm_type2   ("光线步进模式2", Float) = 0
        _raymarchFalloffStart  ("光线步进衰减起始（0~1）", Float) = 1
        _raymarchFalloffEnd    ("光线步进衰减终止（0~1）", Float) = 1
        _refTexRes             ("反射贴图分辨率 (宽, 高)", Vector) = (1024, 1024, 0, 0)
        [ToggleUI] _enable_td  ("开启俯视角反射", Float) = 0
        [ToggleUI] _enable_pl  ("开启横版反射（平台跳跃模式）", Float) = 1
        _ref_transform         ("反射贴图变换 (scaleX, scaleY, offsetX, offsetY)", Vector) = (0, 0, 0, 0)
        [ToggleUI] _enableFalloff ("开启反射边缘衰减", Float) = 0
        _falloffStrength       ("反射衰减强度", Float) = 0
        _falloffStart          ("反射衰减起始Y位置（负数=从底部往上计算）", Float) = -0.3

        // ── 障碍物描边 ────────────────────────────────────
        [ToggleUI] _enable_obs   ("开启障碍物描边", Float) = 1
        _obstruction_color       ("障碍物描边颜色", Color) = (0.9009434, 1, 0.9942852, 0)
        _obstruction_width       ("障碍物描边宽度", Range(0, 1)) = 0
        _obstruction_alpha       ("障碍物描边透明度", Range(0, 1)) = 1
        _obs_transform           ("障碍物贴图变换 (scaleX, scaleY, offsetX, offsetY)", Vector) = (0, 0, 0, 0)
        [ToggleUI] _depthFromObstructors ("从障碍物深度贴图驱动颜色渐变", Float) = 0

        // ── 水面以下背景贴图 ──────────────────────────────
        [NoScaleOffset] _belowWaterTex ("水面以下背景贴图（相机渲染纹理）", 2D) = "white" {}
        // UV范围 (xMin, xMax, yMin, yMax)：控制贴图映射到水面UV的哪个区域
        _belowWaterTexUV                ("水面以下贴图UV范围", Vector) = (0, 0, 1, 1)
        _belowWaterTexAlpha             ("水面以下贴图透明度", Float) = 1
        _belowWaterTexDistortionStrength("水面以下贴图扭曲强度", Float) = 0

        // ── 水面表面纹理 ──────────────────────────────────
        [NoScaleOffset] _surfaceTex ("水面表面纹理（水波花纹）", 2D) = "white" {}
        _surfaceTexAlpha              ("水面纹理透明度", Float) = 1
        _surfaceTexTiling             ("水面纹理平铺 (X, Y)", Vector) = (1, 1, 0, 0)
        _surfaceTexSpeed              ("水面纹理滚动速度 (X, Y)", Vector) = (0.2, 0, 0, 0)
        // 若开启则使用泡沫速度驱动水面纹理，否则用 _surfaceTexSpeed
        _useFoamSpeedForST            ("使用泡沫速度驱动水面纹理", Float) = 0
        _surfaceTexUV                 ("水面纹理UV范围（保留参数）", Vector) = (0, 1, 0, 1)

        // ── 动态波浪边缘 ──────────────────────────────────
        _edgeSize               ("波浪边缘宽度", Float) = 0.05
        _edgeColor              ("波浪边缘颜色", Color) = (0.9245283, 0.8329477, 0.8329477, 0)
        // 1 = 边缘区域无视水体透明度，始终显示边缘颜色；0 = 跟随水体透明度
        _edgeIgnoreTransparency ("边缘忽略透明度", Float) = 1

        // ── 由系统脚本运行时注入（请勿手动修改）────────────
        [HideInInspector] _RFalpha     ("反射贴图Alpha倍率", Float) = 1
        [HideInInspector] _RForgColor  ("使用原始场景色比例（0=纯反射色）", Float) = 0
        [HideInInspector][NoScaleOffset] unity_Lightmaps    ("unity_Lightmaps",    2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks  ("unity_ShadowMasks",  2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"         = "UniversalPipeline"
            "RenderType"             = "Transparent"
            "UniversalMaterialType"  = "Lit"
            "Queue"                  = "Transparent"
        }

        // ================================================================
        //  Pass 1：主渲染
        //  LightMode 说明：
        //    - "UniversalForward"  适用于 URP 3D 渲染器（UniversalRenderer）
        //    - "Universal2D"       适用于 URP 2D 渲染器（Renderer2DData）
        //  当前项目使用 3D 渲染器，故使用 UniversalForward。
        //  如果切换为 2D 渲染器，将此处改为 "Universal2D"。
        // ================================================================
        Pass
        {
            Name "Sprite Lit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma exclude_renderers d3d11_9x
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ────────────────────────────────────────────
            // 材质常量缓冲区
            // ────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
            // 颜色与深度
            float4 _color;
            float4 _deep_color;
            float2 _deep_minmax;
            float  _surfaceAlpha;
            float  _color_type;
            float4 _colorGradient_TexelSize;
            float  _depthMlp;
            // UV 与像素完美
            float2 _tiling;
            float  _num_of_pixels;
            float  _pixel_perfect;
            // 玩家视差滚动
            float3 _playerPosition;
            float  _scrStrength;
            float  _enable_scrolling;
            // 扭曲
            float2 _distortion_tiling;
            float2 _distortion_speed;
            float2 _distortion_strength;
            float4 _distortion_color;
            float2 _distortion_minmax;
            float  _distortionFPRH;
            // 泡沫
            float4 _foam_color;
            float  _foam_size;
            float  _foam_density;
            float  _foam_alpha;
            float2 _foam_speed;
            // 太阳光条纹
            float  _strips_density;
            float  _strips_size;
            float  _strips_speed;
            float  _strips_scrolling_speed;
            float  _strips_alpha;
            // 水体模拟（Material 属性部分）
            float  _enable_sim;
            float4 _simFoamColor;
            float2 _simMinMaxWavesHeightFoam;
            float  _normStr;
            // 反射
            float  _reflectionY;
            float4 _reflectionsColor;
            float  _enable_ref;
            float  _enable_rm;
            float  _raymarchSteps;
            float  _rm_type2;
            float  _raymarchFalloffStart;
            float  _raymarchFalloffEnd;
            float2 _refTexRes;
            float  _enable_td;
            float  _enable_pl;
            float4 _ref_transform;
            float  _enableFalloff;
            float  _falloffStrength;
            float  _falloffStart;
            // 障碍物描边
            float  _enable_obs;
            float4 _obstruction_color;
            float  _obstruction_width;
            float  _obstruction_alpha;
            float4 _obs_transform;
            float  _depthFromObstructors;
            // 水面以下贴图
            float4 _belowWaterTexUV;
            float  _belowWaterTexAlpha;
            float  _belowWaterTexDistortionStrength;
            // 水面表面纹理
            float  _surfaceTexAlpha;
            float2 _surfaceTexTiling;
            float2 _surfaceTexSpeed;
            float  _useFoamSpeedForST;
            float4 _surfaceTexUV;
            // 动态波浪边缘
            float  _edgeSize;
            float4 _edgeColor;
            float  _edgeIgnoreTransparency;
            // 相机矩阵（由脚本设置）
            float4x4 _projectionMatrix;
            float4x4 _worldToCamMatrix;
            // Texel Size（Unity 自动填充）
            float4 _MainTex_TexelSize;
            float4 _distortion_tex_TexelSize;
            float4 _simTex_TexelSize;
            float4 _surfaceTex_TexelSize;
            float4 _belowWaterTex_TexelSize;
            float4 _alphaTexture_TexelSize;
            CBUFFER_END

            // ────────────────────────────────────────────
            // 全局 uniform（由水体系统脚本在运行时注入，不属于 Material 属性）
            // ────────────────────────────────────────────
            float4 _simUvs;      // 模拟贴图在水体UV空间的覆盖范围 (xMin,yMin,xMax,yMax)
            float4 _RFcolor;     // 反射色调（系统注入）
            float  _RFalpha;     // 反射整体透明度（系统注入）
            float  _RForgColor;  // 原始场景色保留比例（系统注入）
            float4 _camRect;     // 相机矩形范围（系统注入）
            float  _dwaves;      // 动态波浪开关（系统注入）

            // ────────────────────────────────────────────
            // 内联采样器状态（Unity 自动生成对应采样器）
            // ────────────────────────────────────────────
            SAMPLER(SamplerState_Linear_Clamp);
            SAMPLER(SamplerState_Linear_Repeat);
            SAMPLER(SamplerState_Trilinear_Repeat);

            // ────────────────────────────────────────────
            // 全局贴图声明（由系统/脚本在运行时绑定）
            // ────────────────────────────────────────────
            TEXTURE2D(_MainTex);              SAMPLER(sampler_MainTex);
            TEXTURE2D(_alphaTexture);         SAMPLER(sampler_alphaTexture);
            TEXTURE2D(_distortion_tex);       SAMPLER(sampler_distortion_tex);
            TEXTURE2D(_sun_strips);           SAMPLER(sampler_sun_strips);
            TEXTURE2D(_simTex);               SAMPLER(sampler_simTex);
            TEXTURE2D(_colorGradient);        SAMPLER(sampler_colorGradient);
            TEXTURE2D(_DepthTexture);         SAMPLER(sampler_DepthTexture);
            TEXTURE2D(_OBStexture);           SAMPLER(sampler_OBStexture);
            TEXTURE2D(_RFreflectionsTexture);  SAMPLER(sampler_RFreflectionsTexture);
            TEXTURE2D(_RFreflectionsTexture2); SAMPLER(sampler_RFreflectionsTexture2);
            TEXTURE2D(_RFreflectionsTexture3); SAMPLER(sampler_RFreflectionsTexture3);
            TEXTURE2D(_surfaceTex);           SAMPLER(sampler_surfaceTex);
            TEXTURE2D(_belowWaterTex);        SAMPLER(sampler_belowWaterTex);
            TEXTURE2D(_wavesHeight);          SAMPLER(sampler_wavesHeight);

            // ────────────────────────────────────────────
            // 顶点/片元结构体
            // ────────────────────────────────────────────
            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 uv0        : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;   // 原始UV0
                float4 color      : COLOR;
                float3 positionWS : TEXCOORD1;   // 世界坐标（用于投影到屏幕UV）
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ════════════════════════════════════════════
            // 噪声工具函数（自包含实现，无外部依赖）
            // 不依赖 Hashes.hlsl 中的 Hash_LegacyMod / Hash_Tchou 函数，
            // 避免不同 Unity 版本间的兼容性问题。
            // ════════════════════════════════════════════

            // 内部用：2D→2D 伪随机哈希，输出方向向量
            float2 _GradHash(float2 p, float salt)
            {
                // 基于数学乘法哈希，无需外部函数
                p = float2(dot(p, float2(127.1, 311.7) + salt),
                           dot(p, float2(269.5, 183.3) + salt));
                return frac(sin(p) * 43758.5453123) * 2.0 - 1.0;
            }

            // 梯度噪声（Perlin-like），返回 [0, 1]
            // salt 参数：给同一张噪声传不同 salt 产生视觉上不同的图案
            float GradNoise(float2 uv, float scale, float salt)
            {
                float2 p  = uv * scale;
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(_GradHash(ip,                salt), fp);
                float d01 = dot(_GradHash(ip + float2(0,1), salt), fp - float2(0,1));
                float d10 = dot(_GradHash(ip + float2(1,0), salt), fp - float2(1,0));
                float d11 = dot(_GradHash(ip + float2(1,1), salt), fp - float2(1,1));
                // 六次平滑插值（Smootherstep）
                fp = fp * fp * fp * (fp * (fp * 6.0 - 15.0) + 10.0);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) * 0.5 + 0.5;
            }

            // 泡沫用噪声（对应原 Hash_LegacyMod 版本）
            float GradNoiseLegacy(float2 uv, float scale)
            {
                return GradNoise(uv, scale, 0.0);
            }

            // 光条纹用噪声（对应原 Hash_Tchou 版本，不同 salt 产生不同图案）
            float GradNoiseDet(float2 uv, float scale)
            {
                return GradNoise(uv, scale, 53.7);
            }

            // ════════════════════════════════════════════
            // UV 工具函数
            // ════════════════════════════════════════════

            // 像素完美UV：将连续UV量化到最近像素格，消除亚像素抖动
            float2 PixelPerfectUV(float2 uv, float numPixels)
            {
                // 从模型矩阵提取对象X/Y轴世界缩放
                float scaleX = length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x));
                float scaleY = length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y));
                // 按缩放比例调整像素数（保持各方向像素密度一致）
                float2 pixelCount = float2(scaleX / scaleY * numPixels, numPixels);
                return floor(uv * pixelCount) / pixelCount;
            }

            // ════════════════════════════════════════════
            // 综合水体UV计算
            // 流程：UV0 → 平铺 + 玩家偏移 → 宽高比修正 → 像素完美
            // ════════════════════════════════════════════
            float2 WaterUV(float2 rawUV)
            {
                // 玩家位置产生的视差X偏移（水面跟随玩家轻微位移）
                float scrollX = (_enable_scrolling > 0.5)
                                ? _playerPosition.x * 0.05 * _scrStrength
                                : 0.0;

                // 应用平铺（_tiling + (0,1) 对应原始Shader Graph中+1的偏移）
                float2 tiled = rawUV * (_tiling + float2(0, 1)) + float2(scrollX, 0);

                // 修正Y方向的宽高比缩放（使UV看起来不会因对象缩放而拉伸）
                float scaleX = length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x));
                float scaleY = length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y));
                float2 corrected = float2(tiled.x, tiled.y * scaleY / scaleX);

                // 像素完美吸附（仅在开启时生效）
                if (_pixel_perfect > 0.5)
                    corrected = PixelPerfectUV(corrected, _num_of_pixels);

                return corrected;
            }

            // ════════════════════════════════════════════
            // 屏幕空间UV（自定义相机矩阵投影）
            // 将世界坐标投影到 [0,1] 屏幕UV（用于反射/障碍物贴图采样）
            // 当矩阵未被水体系统初始化时（w=0），返回安全值 (0.5, 0.5)
            // ════════════════════════════════════════════
            float2 WorldToScreenUV(float3 worldPos)
            {
                float4x4 vp    = mul(_projectionMatrix, _worldToCamMatrix);
                float4 clipPos = mul(vp, float4(worldPos, 1.0));
                // 防止 w=0 导致除零产生 NaN（矩阵未初始化时 w=0）
                if (abs(clipPos.w) < 1e-5) return float2(0.5, 0.5);
                float2 ndc = clipPos.xy / clipPos.w;
                return saturate((ndc + 1.0) * 0.5);
            }

            // 像素完美屏幕UV（将屏幕UV也量化到像素格）
            float2 PixelPerfectScreenUV(float2 screenUV, float numPixels)
            {
                float scaleX = length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x));
                float scaleY = length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y));
                float2 pixelCount = float2(numPixels, numPixels);
                return floor(screenUV * pixelCount) / pixelCount;
            }

            // ════════════════════════════════════════════
            // 水体模拟：法线与泡沫
            // 从模拟贴图（_simTex）读取波纹高度，生成扰动法线和泡沫颜色
            // ════════════════════════════════════════════
            void SimNormalsAndFoam(float2 rawUV, float3 worldPos,
                                   out float3 simNormal, out float4 simFoam)
            {
                simNormal = float3(0, 0, 0);
                simFoam   = float4(0, 0, 0, 0);
                if (_enable_sim < 0.5) return;

                // _simUvs 由水体系统脚本注入，若系统未运行则为 (0,0,0,0)。
                // 此时范围 xMax-xMin=0，除以零会产生 NaN，需提前退出。
                float rangeX = _simUvs.z - _simUvs.x;
                float rangeY = _simUvs.w - _simUvs.y;
                if (abs(rangeX) < 1e-5 || abs(rangeY) < 1e-5) return;

                // 将原始UV重映射到模拟贴图的UV范围（由系统注入的 _simUvs 定义覆盖范围）
                // _simUvs = (xMin, yMin, xMax, yMax)
                float2 simUV = float2(
                    (rawUV.x - _simUvs.x) / rangeX,
                    (rawUV.y - _simUvs.y) / rangeY
                );
                float4 simSample = SAMPLE_TEXTURE2D(_simTex, SamplerState_Trilinear_Repeat, simUV);

                // 从模拟贴图R通道提取高度值，转为法线用于UV扰动
                float height = simSample.r / 8.0 * -1.0;
                float str    = _normStr * 0.025;

                // 用屏幕空间导数近似生成扰动方向（偏移量很小，用于UV扰动即可）
                float dHdx = ddx(height);
                float dHdy = ddy(height);
                // 简化法线：XY分量作为UV扰动偏移
                simNormal = float3(dHdx * str, dHdy * str, 0);

                // 根据扰动强度（abs(x)+abs(y)）判断泡沫可见性
                float heightAbs = abs(simNormal.x) + abs(simNormal.y);
                float foamT = saturate(
                    (heightAbs - _simMinMaxWavesHeightFoam.x) /
                    max(0.0001, _simMinMaxWavesHeightFoam.y - _simMinMaxWavesHeightFoam.x)
                );
                // 模拟泡沫颜色（高波浪处出现泡沫）
                simFoam = lerp(float4(_simFoamColor.rgb, 0), _simFoamColor, foamT);
            }

            // ════════════════════════════════════════════
            // 扭曲偏移计算
            // 从扭曲贴图 RG 通道提取XY偏移，叠加模拟法线扰动
            // 输出：UV偏移量 + 水面顶部高光颜色
            // ════════════════════════════════════════════
            float2 CalcDistortion(float2 waterUV, float3 simNormal,
                                  out float3 distColorTop)
            {
                // 扭曲贴图UV（时间驱动流动）
                float2 distUV  = waterUV * _distortion_tiling + _Time.y * _distortion_speed;
                float4 distTex = SAMPLE_TEXTURE2D(_distortion_tex, SamplerState_Linear_Repeat, distUV);

                // RG通道分别提供XY扰动（减去中性值0.2/0.21使其双向偏移）
                float2 rawOffset = float2(
                    _distortion_strength.x * (distTex.r - 0.2),
                    _distortion_strength.y * (distTex.g - 0.21)
                ) + simNormal.xy;

                float2 distOffset = rawOffset;

                // FPRH 模式：防止反射UV越过水面顶部
                // 仅当 _enable_pl 时对Y方向做截断（clamp到负值=只允许向上偏移）
                if (_enable_pl > 0.5)
                    distOffset.y = clamp(distOffset.y, -100.0, 0.0);

                // FPRH 进阶模式：在 Y=0 附近平滑放大偏移（让过渡更自然）
                if (_distortionFPRH > 0.5)
                {
                    float2 smth    = smoothstep(float2(-0.005, -0.005), float2(0, 0), distOffset);
                    float  remap   = lerp(1.0, 4.0, smth.y);
                    distOffset.y  *= remap;
                }

                // 计算扭曲高光（扰动强度越大，高光越明显）
                float distMag   = abs(rawOffset.x) + abs(rawOffset.y);
                float colorT    = saturate(
                    (distMag - _distortion_minmax.x) /
                    max(0.0001, _distortion_minmax.y * 0.1 - _distortion_minmax.x)
                );
                distColorTop = lerp(float3(0, 0, 0), _distortion_color.rgb, colorT);

                return distOffset;
            }

            // ════════════════════════════════════════════
            // 太阳光条纹（水下焦散光斑效果）
            // 原理：梯度噪声 + Twirl扭曲 → 生成放射状光条
            // ════════════════════════════════════════════
            float SunStrips(float2 waterUV, float2 animOffset)
            {
                // 基础UV（加条纹平铺）
                float2 baseTiled = waterUV * _tiling;

                // 添加速度驱动的偏移（X轴为主流方向）
                float2 stripOffset = float2(animOffset.x * _strips_speed, animOffset.y);
                float2 scrolled    = baseTiled * float2(1.0, 1.25) + stripOffset;

                // Twirl（旋转扭曲）：原Shader Graph中固定参数
                float2 twirlCenter  = float2(0.27, -0.42);
                float2 twirlOffset  = float2(0.82, -0.17);
                float2 delta        = scrolled - twirlCenter;
                // strength=0 时 sin/cos 退化为 (0, 1)，实际无旋转但保留坐标变换
                float2 twirled = float2(delta.x + twirlCenter.x + twirlOffset.x,
                                        delta.y + twirlCenter.y + twirlOffset.y);

                // 用确定性梯度噪声生成条纹形状
                float gn    = GradNoiseDet(twirled, _strips_density);
                float shape = smoothstep(0.61, 0.63, gn);

                // 采样条纹贴图（按大小和速度调整UV）
                float  invSize   = 1.0 / max(0.001, _strips_size);
                float2 stripTile = float2(invSize * 3.0, invSize);
                float2 stripUV   = waterUV * (_tiling + float2(-0.15, 0)) * stripTile + stripOffset;
                float  stripTex  = SAMPLE_TEXTURE2D(_sun_strips, SamplerState_Linear_Repeat, stripUV).a;

                // 阈值化（step产生硬边条纹）后乘以透明度
                return step(0.62, shape * stripTex) * _strips_alpha;
            }

            // ════════════════════════════════════════════
            // 单层泡沫
            // 基于梯度噪声生成柔和的圆形气泡轮廓
            // ════════════════════════════════════════════
            float3 FoamLayer(float2 uv, float2 uvOffset, float tileMult,
                             float alpha, float sharedNoise)
            {
                float2 foamUV  = uv * tileMult + uvOffset;
                // 梯度噪声的4次方产生更集中的圆形形状
                float  wave    = pow(GradNoiseLegacy(foamUV, _foam_size * 16.0), 4.0) * 0.4;
                // 用共享噪声调制密度，产生不均匀分布
                float  mask    = smoothstep(0.005, 0.01, wave * sharedNoise * _foam_density);
                return _foam_color.rgb * alpha * mask;
            }

            // ════════════════════════════════════════════
            // 三层叠加泡沫
            // 三层使用不同UV偏移和平铺，产生丰富的层次感
            // animOffset: 当前帧的时间驱动动画偏移
            // ════════════════════════════════════════════
            float3 TripleFoam(float2 waterUV, float2 animOffset)
            {
                // 共享噪声：决定泡沫的宏观分布图案
                float2 noiseUV = waterUV * 1.2 + float2(0.5, -0.3);
                float  sharedGN = GradNoiseLegacy(noiseUV, _foam_size * 8.0);

                // 三层：不同偏移产生不同相位，alpha依次减小使远层泡沫更透
                float3 f1 = FoamLayer(waterUV, animOffset + float2( 0.2,  0.5), 1.00, 1.0, sharedGN);
                float3 f2 = FoamLayer(waterUV, animOffset + float2(-0.2,  0.3), 1.05, 0.6, sharedGN);
                float3 f3 = FoamLayer(waterUV, animOffset + float2(-0.1,  0.7), 1.30, 0.2, sharedGN);

                // *2 对应原Shader Graph中 foam_alpha * 2 的放大
                return (f1 + f2 + f3) * _foam_alpha * 2.0;
            }

            // ════════════════════════════════════════════
            // 障碍物描边
            // 向下偏移采样障碍物贴图，产生障碍物下边缘的发光轮廓
            // ════════════════════════════════════════════
            float4 ObstructionColor(float2 screenUV, out float rawAlpha)
            {
                rawAlpha = 0;
                if (_enable_obs < 0.5) return float4(0, 0, 0, 0);

                // 将屏幕UV映射到障碍物贴图空间
                // _obs_transform: (scaleX, scaleY, offsetX, offsetY) → obs贴图坐标 = offset + scale * screenUV
                float2 obsUV = _obs_transform.wz + _obs_transform.xy * screenUV;

                // 向下偏移一个宽度距离采样（比较上下两个点的遮挡差，检测轮廓边缘）
                float  width  = _obstruction_width / 50.0;
                float  above  = SAMPLE_TEXTURE2D(_OBStexture, sampler_OBStexture, obsUV + float2(0, width)).r;
                float  here   = SAMPLE_TEXTURE2D(_OBStexture, sampler_OBStexture, obsUV).r;
                float  edge   = saturate(above - here);

                rawAlpha = edge * _obstruction_alpha;
                return _obstruction_color * rawAlpha;
            }

            // ════════════════════════════════════════════
            // 反射 — 横版平台模式
            // 将屏幕UV的Y轴关于 reflectionY 翻转，从平台反射贴图采样
            // ════════════════════════════════════════════
            float4 PlatformerReflection(float2 screenUV)
            {
                if (_enable_pl < 0.5) return float4(0, 0, 0, 0);

                // Y坐标缩放到0.66666（对应原Shader中的固定系数，匹配反射贴图的宽高比）
                float sy    = screenUV.y * 0.66666;
                float ry    = _reflectionY * 0.66666;
                // 翻转Y（关于反射基准线对称）
                float flipY = ry - (sy - ry);
                float2 reflUV = float2(screenUV.x, flipY);

                float4 refSample = SAMPLE_TEXTURE2D(_RFreflectionsTexture2, SamplerState_Linear_Clamp, reflUV);

                // 混合反射颜色与场景色调（RForgColor=1时保留原场景色，=0时用RFcolor替换）
                float4 blended = lerp(refSample, _RFcolor, 1.0 - _RForgColor);
                blended.a      = saturate(refSample.a);

                // 竖向衰减：横版模式下反射从reflY往上逐渐消隐
                float falloff = 1.0;
                if (_enableFalloff > 0.5)
                    falloff = saturate((screenUV.y + _falloffStart) * (1.0 - _falloffStrength));

                blended.a *= falloff * _RFalpha;
                return blended;
            }

            // ════════════════════════════════════════════
            // 反射 — 俯视角模式
            // 直接采样俯视角反射贴图（相机渲染的反射纹理）
            // ════════════════════════════════════════════
            float4 TopDownReflection(float2 screenUV)
            {
                if (_enable_td < 0.5) return float4(0, 0, 0, 0);
                return SAMPLE_TEXTURE2D(_RFreflectionsTexture, SamplerState_Linear_Clamp, screenUV);
            }

            // ════════════════════════════════════════════
            // 反射 — 光线步进
            // 沿屏幕Y轴向上步进，找到第一个不透明像素后采样其翻转位置
            // 用于捕捉运动物体或传统相机无法捕捉的反射
            // ════════════════════════════════════════════
            float4 RaymarchReflection(float2 screenUV)
            {
                if (_enable_rm < 0.5) return float4(0, 0, 0, 0);

                // 缩放UV到反射贴图空间（0.66666系数同横版模式）
                float2 uv     = float2(screenUV.x, screenUV.y * 0.66666);
                float2 stepS  = 1.0 / _refTexRes;
                float  y0     = uv.y;
                float  y1     = 0;
                int    hitIdx = 0;

                int steps = (int)_raymarchSteps;
                for (int j = 0; j < steps; j++)
                {
                    uv.y += stepS.y;
                    // 找到第一个不透明像素（Alpha > 0.2 认为有内容）
                    float4 s = SAMPLE_TEXTURE2D(_RFreflectionsTexture3, SamplerState_Linear_Clamp, uv);
                    if (s.a > 0.2 && hitIdx == 0)
                    {
                        y1     = uv.y;
                        hitIdx = j;
                    }
                }

                if (y1 == 0) return float4(0, 0, 0, 0);

                // 将命中点关于出发点做Y翻转（反射原理：出发→命中的距离再往上等距）
                float y2      = (y1 - y0) + y1;
                float4 result = SAMPLE_TEXTURE2D(_RFreflectionsTexture3, SamplerState_Linear_Clamp, float2(uv.x, y2));

                // 边缘渐隐（步数越多的命中点越淡）
                if (_raymarchFalloffEnd != _raymarchFalloffStart)
                {
                    float t   = saturate(
                        ((float(hitIdx) / _raymarchSteps) - _raymarchFalloffStart) /
                        (_raymarchFalloffEnd - _raymarchFalloffStart)
                    );
                    result.a *= (1.0 - t);
                }
                return result;
            }

            // ════════════════════════════════════════════
            // 深度颜色混合
            // 根据UV坐标（径向距离或Y轴）在浅色与深色之间插值
            // ════════════════════════════════════════════
            float4 DepthColor(float2 uv)
            {
                float dist;
                if (_color_type != 2.0)
                    // 径向模式：从UV中心 (0.5,0.5) 的距离
                    dist = distance(uv, float2(0.5, 0.5));
                else
                    // Y轴模式：从底部 (y=0) 的距离
                    dist = distance(uv.y, 0.0);

                // 将距离映射到 [deep_minmax.x, deep_minmax.y]，再反转（中心/底部为深色）
                float mask = lerp(_deep_minmax.x, _deep_minmax.y, dist);
                float t    = 1.0 - saturate(mask);
                // 混合浅色与深色，最后乘以整体透明度
                return lerp(_color, _deep_color, t) * _surfaceAlpha;
            }

            // ════════════════════════════════════════════
            // 叠加混合（AddOnTop）
            // 用 Top.a 将 Top.rgb 叠加到 Bottom.rgb 上，输出 alpha=1
            // ════════════════════════════════════════════
            float4 AddOnTop(float4 bottom, float4 top)
            {
                return float4(lerp(bottom.rgb, top.rgb, top.a), 1.0);
            }

            // ════════════════════════════════════════════
            // 顶点着色器
            // ════════════════════════════════════════════
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.uv         = IN.uv0.xy;
                OUT.color      = IN.color;
                return OUT;
            }

            // ════════════════════════════════════════════
            // 片元着色器：主渲染逻辑
            // ════════════════════════════════════════════
            float4 frag(Varyings IN) : SV_Target
            {
                float2 rawUV = IN.uv;

                // ──────────────────────────────────────
                // 步骤 1：计算各类 UV
                // ──────────────────────────────────────
                float2 waterUV  = WaterUV(rawUV);
                float2 screenUV = WorldToScreenUV(IN.positionWS);
                // 像素完美处理屏幕UV（保持反射贴图采样对齐像素）
                float2 screenUVpp = (_pixel_perfect > 0.5)
                                  ? PixelPerfectScreenUV(screenUV, _num_of_pixels)
                                  : screenUV;

                // ──────────────────────────────────────
                // 步骤 2：水体模拟（法线 & 泡沫颜色）
                // ──────────────────────────────────────
                float3 simNormal;
                float4 simFoamColor;
                SimNormalsAndFoam(rawUV, IN.positionWS, simNormal, simFoamColor);

                // ──────────────────────────────────────
                // 步骤 3：泡沫动画偏移
                // 基于泡沫速度和时间计算流动方向，并叠加模拟法线扰动
                // ──────────────────────────────────────
                float  stripScrollX = (_foam_speed.x + 0.1) * 0.15 * _Time.y;
                float2 animOffset   = float2(stripScrollX, 0) + simNormal.xy;

                // ──────────────────────────────────────
                // 步骤 4：扭曲偏移
                // ──────────────────────────────────────
                float3 distColorTop;
                float2 distOffset = CalcDistortion(waterUV, simNormal, distColorTop);
                // 综合动画偏移（泡沫流速 + 扭曲）
                float2 foamAnimOffset = float2(0.15, 0.15) * _foam_speed * _Time.y + distOffset;

                // ──────────────────────────────────────
                // 步骤 5：太阳光条纹
                // ──────────────────────────────────────
                float strips = SunStrips(waterUV, animOffset);

                // ──────────────────────────────────────
                // 步骤 6：三层泡沫
                // ──────────────────────────────────────
                float3 foam = TripleFoam(waterUV, foamAnimOffset);

                // 条纹 + 泡沫 = 最终泡沫叠加层（用于颜色和Alpha）
                float3 foamTotal = float3(strips, strips, strips) + foam;

                // ──────────────────────────────────────
                // 步骤 7：障碍物描边
                // ──────────────────────────────────────
                float  obsRawAlpha;
                float4 obsColor = ObstructionColor(screenUVpp, obsRawAlpha);

                // ──────────────────────────────────────
                // 步骤 8：反射合成
                // 计算带FPRH扭曲偏移的反射UV，然后采样各层反射
                // ──────────────────────────────────────
                float2 distFPRH;
                if (_distortionFPRH > 0.5)
                {
                    // FPRH 模式：将扭曲向量转化为反射贴图的采样偏移
                    float2 smth   = smoothstep(float2(-0.005, -0.005), float2(0, 0), distOffset);
                    float  remap  = lerp(1.0, 4.0, smth.y);
                    distFPRH = float2(distOffset.x, distOffset.y * remap);
                }
                else
                    distFPRH = distOffset;

                float2 refSampleUV = screenUVpp + distFPRH;  // 反射贴图最终采样UV

                float4 rmRef = RaymarchReflection(refSampleUV);
                float4 plRef = PlatformerReflection(screenUVpp + distFPRH);
                float4 tdRef = TopDownReflection(refSampleUV);

                // 组合反射层：俯视角 / 横版 / 光线步进，按启用状态选择
                float4 refCombined;
                if (_enable_pl > 0.5 && _enable_td > 0.5)
                    refCombined = AddOnTop(plRef, tdRef);
                else if (_enable_pl > 0.5)
                    refCombined = plRef;
                else
                    refCombined = tdRef;

                // 光线步进反射叠加（以 rmRef.a 做 overwrite blend）
                if (_enable_rm > 0.5 && rmRef.a > 0)
                    refCombined = lerp(refCombined, rmRef, rmRef.a);

                // 反射总开关
                if (_enable_ref < 0.5) refCombined = float4(0, 0, 0, 0);

                // 横版反射衰减（Y 方向边缘消退）
                float falloffMult = 1.0;
                if (_enable_pl > 0.5 && _enableFalloff > 0.5)
                    falloffMult = saturate((rawUV.y + _falloffStart) * (1.0 - _falloffStrength));

                float4 refFinal = _reflectionsColor * refCombined;
                refFinal.a     *= falloffMult * _RFalpha;

                // ──────────────────────────────────────
                // 步骤 9：基础水体颜色
                // ──────────────────────────────────────
                float4 baseColor = DepthColor(rawUV); // 默认初始化，防止未定义值
                if (_depthFromObstructors > 0.5)
                {
                    // 从障碍物/深度贴图的B通道驱动颜色渐变
                    float2 depthUV = _obs_transform.wz + _obs_transform.xy * refSampleUV;
                    float  depth   = SAMPLE_TEXTURE2D(_DepthTexture, SamplerState_Linear_Clamp, depthUV).b;
                    // 深度乘以倍率后在颜色渐变贴图上采样
                    baseColor = SAMPLE_TEXTURE2D(_colorGradient, SamplerState_Linear_Clamp,
                                                 float2(depth * _depthMlp, 0.5));
                }
                else
                {
                    baseColor = DepthColor(rawUV);
                }

                // ──────────────────────────────────────
                // 步骤 10：水面以下贴图
                // 用 belowWaterTexUV 范围将原始UV映射到背景贴图，支持扭曲
                // ──────────────────────────────────────
                float2 belowU = float2(lerp(_belowWaterTexUV.x, _belowWaterTexUV.y, rawUV.x),
                                       lerp(_belowWaterTexUV.z, _belowWaterTexUV.w, rawUV.y));
                float2 belowFinalUV  = belowU + distOffset * _belowWaterTexDistortionStrength;
                float4 belowTexSample = SAMPLE_TEXTURE2D(_belowWaterTex, sampler_belowWaterTex, belowFinalUV);

                // ──────────────────────────────────────
                // 步骤 11：水面纹理
                // 可以用扭曲偏移速度或独立速度驱动滚动
                // ──────────────────────────────────────
                float2 stSpeed = (_useFoamSpeedForST > 0.5)
                               ? distOffset
                               : _surfaceTexSpeed * _Time.y;
                float2 stUV   = (waterUV + distOffset) * _surfaceTexTiling + stSpeed;
                float4 surfTexSample = SAMPLE_TEXTURE2D(_surfaceTex, SamplerState_Linear_Repeat, stUV);

                // ──────────────────────────────────────
                // 步骤 12：颜色合成
                // 从基础色开始，逐层叠加各效果
                // ──────────────────────────────────────

                // a) 基础色 × (1 - belowWaterTexAlpha)（让 below 贴图透出时削弱基础色）
                float4 waterBase = baseColor * (1.0 - _belowWaterTexAlpha);
                waterBase.a = 1.0;

                // b) 叠加水面纹理（Overwrite blend，用 surfaceTexAlpha 控制权重）
                float4 withSurf = lerp(waterBase, surfTexSample, _surfaceTexAlpha);
                withSurf.a = 1.0;

                // c) 叠加 below 贴图（取 belowTex.a 和 surfaceTexAlpha 较大值作为 blend 权重）
                float belowBlend  = _belowWaterTexAlpha * max(belowTexSample.a, _surfaceTexAlpha);
                float4 withBelow  = lerp(withSurf, belowTexSample, belowBlend);
                withBelow.a = 1.0;

                // d) AddOnTop：反射叠加
                float4 withRef = AddOnTop(withBelow, refFinal);

                // e) 叠加障碍物颜色 + 泡沫 + 扭曲高光（直接相加）
                float3 withFoam = obsColor.rgb + withRef.rgb + foamTotal + distColorTop;

                // f) AddOnTop：模拟波浪泡沫颜色叠加
                float4 finalColor = AddOnTop(float4(withFoam, 1.0), simFoamColor);

                // ──────────────────────────────────────
                // 步骤 13：动态波浪边缘（_dwaves 模式）
                // 读取波浪高度贴图，在波浪边缘叠加边缘颜色（Screen Blend）
                // ──────────────────────────────────────
                float waveHeight  = 0;
                float inWaveMask  = 1.0;    // 1 = 在水体有效区域内
                float edgeOneMinus = 1.0;   // 用于 Screen blend 遮罩

                if (_dwaves > 0.5)
                {
                    waveHeight = SAMPLE_TEXTURE2D(_wavesHeight, SamplerState_Linear_Repeat, rawUV).r;
                    float dist = abs(waveHeight - rawUV.y);

                    // 边缘区域：平滑步进产生柔和过渡
                    float edgeMask = 1.0 - smoothstep(_edgeSize - 0.01, _edgeSize, dist);
                    float4 edgeCol = _edgeColor * edgeMask;

                    // step 判断是否在边缘宽度内
                    float inEdge   = 1.0 - step(_edgeSize, dist);
                    edgeOneMinus   = 1.0 - inEdge;

                    // Screen Blend：1 - (1-Base) * (1-Blend)，产生提亮叠加
                    finalColor.rgb = 1.0 - (1.0 - finalColor.rgb) * (1.0 - edgeCol.rgb * inEdge);

                    // 波浪遮罩：1-waveHeight 与 1-rawUV.y 比较，决定哪些像素在水体内
                    inWaveMask = step(1.0 - waveHeight, 1.0 - rawUV.y);
                }

                // ──────────────────────────────────────
                // 步骤 14：计算最终 Alpha
                // foamTotal（泡沫+条纹）× 2 后 smoothstep → 加障碍物 + 基础透明度 → saturate → × 遮罩贴图
                // ──────────────────────────────────────

                // 泡沫 Alpha（用 foamTotal+foamTotal 相当于原始的 strips+foam×2 共2份）
                float3 foamAlpha = smoothstep(0.0, 0.1, foamTotal + foam);

                // 障碍物存在时强制不透明（step(0.01, alpha)）
                float  obsStep      = step(0.01, obsRawAlpha);
                // 汇总并 saturate 到 [0,1]
                float3 combinedAlpha = saturate(foamAlpha + obsStep + _surfaceAlpha);

                // 乘以透明度遮罩贴图（_alphaTexture 决定水体形状）
                float  alphaMask = SAMPLE_TEXTURE2D(_alphaTexture, sampler_alphaTexture, rawUV).a;
                float3 maskedAlpha = combinedAlpha * alphaMask;

                // 动态波浪模式下进一步裁剪 Alpha
                float finalAlpha;
                if (_dwaves > 0.5)
                {
                    // 波浪内部 Alpha × 波浪遮罩；边缘区域按 edgeIgnoreTransparency 单独计算
                    float inEdge = 1.0 - edgeOneMinus;
                    float3 alphaWithWave = maskedAlpha * inWaveMask + inEdge * _edgeIgnoreTransparency;
                    finalAlpha = alphaWithWave.x;
                }
                else
                {
                    finalAlpha = maskedAlpha.x;
                }

                // 步骤 15：乘以顶点色（SpriteRenderer.color 通过顶点色传入）
                // IN.color.rgb：SpriteRenderer 的颜色色调（乘法叠加到水体颜色上）
                // IN.color.a：SpriteRenderer 的透明度（同样影响最终 Alpha）
                finalColor.rgb *= IN.color.rgb;
                finalAlpha     *= IN.color.a;

                return float4(finalColor.rgb, finalAlpha);
            }
            ENDHLSL
        }

        // ================================================================
        //  Pass 2：法线 Pass（Sprite Normal — 为 2D 点光照提供法线信息）
        // ================================================================
        Pass
        {
            Name "Sprite Normal"
            Tags { "LightMode" = "NormalsRendering" }

            Cull Off
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert_normal
            #pragma fragment frag_normal

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float  _enable_sim;
            float  _normStr;
            CBUFFER_END

            // _simUvs 由系统注入，不在 per-material CBUFFER 中
            float4 _simUvs;

            TEXTURE2D(_simTex); SAMPLER(sampler_simTex);

            struct AttrN { float3 positionOS : POSITION; float4 uv0 : TEXCOORD0; };
            struct VaryN  { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            VaryN vert_normal(AttrN IN)
            {
                VaryN OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv0.xy;
                return OUT;
            }

            float4 frag_normal(VaryN IN) : SV_Target
            {
                float3 normal = float3(0.5, 0.5, 1.0); // 默认上方法线（打包到[0,1]）
                if (_enable_sim > 0.5)
                {
                    // 从模拟贴图提取高度，用屏幕导数生成切线空间法线
                    float2 simUV = float2(
                        (IN.uv.x - _simUvs.x) / (_simUvs.z - _simUvs.x),
                        (IN.uv.y - _simUvs.y) / (_simUvs.w - _simUvs.y)
                    );
                    float h = SAMPLE_TEXTURE2D(_simTex, sampler_simTex, simUV).r / 8.0 * -_normStr * 0.025;
                    float3 dX = ddx(float3(IN.uv, h));
                    float3 dY = ddy(float3(IN.uv, h));
                    float3 n  = normalize(cross(dY, dX));
                    normal = n * 0.5 + 0.5; // 打包到[0,1]以便存储
                }
                return float4(normal, 1.0);
            }
            ENDHLSL
        }
    }
}
