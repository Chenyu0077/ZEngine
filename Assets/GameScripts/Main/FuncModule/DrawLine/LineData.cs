
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

namespace Main.FuncModule.DrawLine
{
    /// <summary>
    /// 线条流动动画模式
    /// </summary>
    public enum LineFlowMode
    {
        None,       // 无动画
        Forward,    // 正向流动
        Backward,   // 反向流动
        Pulse,      // 脉冲（来回）
    }

    /// <summary>
    /// 单条线条数据与运行时状态
    /// </summary>
    public class LineData
    {
        #region 配置

        public string              ID            { get; private set; }
        public List<Vector3>       Waypoints     { get; private set; }
        public Texture             LineTexture   { get; private set; }
        public Color               LineColor     { get; private set; } = Color.white;
        public float               LineWidth     { get; private set; } = 2f;
        public int                 Segments      { get; private set; }
        public bool                ClosedLoop    { get; private set; }
        public float               TextureScale   { get; private set; } = 1.0f;         // 纹理重复次数（越大越密集）
        public LineFlowMode        FlowMode      { get; private set; } = LineFlowMode.Forward;
        public float               FlowSpeed     { get; private set; } = 0.5f;


        public bool                CanSelected    { get; private set; } = false;    // 是否可选中（鼠标点击时）
        public Color               SelectedColor  { get; private set; } = Color.green; // 选中时的颜色
        public int                 ExtraDistance { get; private set; } = 2;         // 选中时的额外距离（像素，增加线条的可选中范围）

        // ---------- 运行时 ----------
        public VectorLine          VectorLine    { get; private set; }
        public bool                IsActive      { get; private set; }
        public bool                IsSelected    { get; private set; }  // 线是否被选中

        private float _textureOffset;
        private float _pulseDir = 1f;   // 脉冲方向（1:正向 -1:反向）

        #endregion
        
        
        /// <param name="id">唯一标识</param>
        /// <param name="waypoints">路径控制点（世界坐标，至少两个）</param>
        /// <param name="texture">线条纹理（需 Wrap Mode = Repeat）</param>
        /// <param name="segments">样条插值点数（越大越平滑）</param>
        /// <param name="lineWidth">线宽（像素）</param>
        /// <param name="closedLoop">是否首尾相连</param>
        public LineData(
            string         id,
            List<Vector3>  waypoints,
            Texture        texture,
            int            segments  = 64,
            float          lineWidth = 2f,
            bool           closedLoop = false)
        {
            ID          = id;
            Waypoints   = waypoints;
            LineTexture = texture;
            Segments    = segments;
            LineWidth   = lineWidth;
            ClosedLoop  = closedLoop;
        }
        

        /// <summary>
        /// 创建 VectorLine 并首次绘制
        /// </summary>
        public void Create()
        {
            if (VectorLine != null) return;
            if (Waypoints == null || Waypoints.Count < 2) return;

            var pts = new List<Vector3>(Segments + 1);
            VectorLine = new VectorLine(ID, pts, LineTexture, LineWidth, LineType.Continuous);
            VectorLine.color = new Color32(
                (byte)(LineColor.r * 255),
                (byte)(LineColor.g * 255),
                (byte)(LineColor.b * 255),
                (byte)(LineColor.a * 255));
            VectorLine.textureScale = TextureScale;

            VectorLine.MakeSpline(Waypoints.ToArray(), Segments, ClosedLoop);
            VectorLine.Draw3D();
            IsActive = true;
        }

        /// <summary>
        /// 销毁 VectorLine
        /// </summary>
        public void Destroy()
        {
            if (VectorLine != null)
            {
                VectorLine line = VectorLine;
                VectorLine.Destroy(ref line);
                VectorLine = null;
            }
            IsActive = false;
            IsSelected = false;
        }



        /// <summary>
        /// 每帧调用，更新流动动画
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsActive || VectorLine == null) return;

            UpdateFlowAnimation(deltaTime);
            UpdateSelectedState(Input.mousePosition, ExtraDistance);
        }

        /// <summary>
        /// 设置线的原始颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(Color color)
        {
            LineColor = color;
            VectorLine?.SetColor(color);
        }

        /// <summary>
        /// 设置线宽
        /// </summary>
        /// <param name="width"></param>
        public void SetWidth(float width)
        {
            LineWidth = width;
            VectorLine?.SetWidth(width);
        }

        /// <summary>
        /// 修改路径并重新绘制样条
        /// </summary>
        public void UpdateWaypoints(List<Vector3> newWaypoints)
        {
            Waypoints = newWaypoints;
            if (VectorLine == null || !IsActive) return;
            VectorLine.MakeSpline(Waypoints.ToArray(), Segments, ClosedLoop);
            VectorLine.Draw3D();
        }

        /// <summary>
        /// 设置贴图
        /// </summary>
        /// <param name="texture"></param>
        public void SetTexture(Texture texture)
        {
            LineTexture = texture;
            if (VectorLine == null) return;
            VectorLine.texture = texture;
        }

        /// <summary>
        /// 设置贴图重复次数（越大越密集）
        /// </summary>
        /// <param name="scale"></param>
        public void SetTextureScale(float scale)
        {
            TextureScale = scale;
            if (VectorLine == null) return;
            VectorLine.textureScale = scale;
        }

        /// <summary>
        /// 设置流动动画模式。
        /// </summary>
        public void SetFlowMode(LineFlowMode mode)
        {
            FlowMode = mode;
        }

        /// <summary>
        /// 设置流动速度。
        /// </summary>
        public void SetFlowSpeed(float speed)
        {
            FlowSpeed = speed;
        }

        /// <summary>
        /// 显示 / 隐藏线条
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (VectorLine == null) return;
            VectorLine.active = visible;
        }
        
        /// <summary>
        /// 设置被选中的颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetSelectedColor(Color color)
        {
            SelectedColor = color;
        }

        /// <summary>
        /// 设置线条是否可选中（鼠标点击时）
        /// </summary>
        /// <param name="canSelected"></param>
        public void SetCanSelected(bool canSelected)
        {
            CanSelected = canSelected;
        }

        /// <summary>
        /// 设置选中时的额外距离（像素，增加线条的可选中范围）
        /// </summary>
        /// <param name="extraDistance"></param>
        public void SetExtraDistance(int extraDistance)
        {
            ExtraDistance = extraDistance;
        }




        private void UpdateFlowAnimation(float deltaTime)
        {
            switch (FlowMode)
            {
                case LineFlowMode.None:
                    break;

                case LineFlowMode.Forward:
                    _textureOffset -= deltaTime * FlowSpeed;
                    if (_textureOffset < 0f) _textureOffset += 1f;
                    VectorLine.textureOffset = _textureOffset;
                    break;

                case LineFlowMode.Backward:
                    _textureOffset += deltaTime * FlowSpeed;
                    if (_textureOffset > 1f) _textureOffset -= 1f;
                    VectorLine.textureOffset = _textureOffset;
                    break;

                case LineFlowMode.Pulse:
                    _textureOffset += deltaTime * FlowSpeed * _pulseDir;
                    if (_textureOffset >= 1f)
                    {
                        _textureOffset = 1f;
                        _pulseDir = -1f;
                    }
                    else if (_textureOffset <= 0f)
                    {
                        _textureOffset = 0f;
                        _pulseDir = 1f;
                    }
                    VectorLine.textureOffset = _textureOffset;
                    break;
            }
        }

        private void UpdateSelectedState(Vector3 screenPos, int extraDistance)
        {
            int index = -1;
            if (!CanSelected) return;
            if (VectorLine.Selected(screenPos, extraDistance, out index))
            {
                // 选中状态（可自定义颜色或其他效果）
                VectorLine.SetColor(SelectedColor);
                IsSelected = true;
            }
            else
            {
                // 非选中状态恢复原色
                VectorLine.SetColor(LineColor);
                IsSelected = false;
            }
        }
    }
}
