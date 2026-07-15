using Main.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Vectrosity;
using ZEngine.Manager.Log;
using ZEngine.Manager.Resource;

namespace Main.FuncModule.DrawLine
{
    /// <summary>
    /// 地图线条管理器
    ///
    /// 职责：
    ///   - 统一管理所有绘制到 3D 平面地图上的 Vectrosity 线条
    ///   - 支持按 ID 增删改查线条
    ///   - 在 Update 中驱动所有线条的流动动画
    ///
    /// 使用示例：
    /// <code>
    ///   // 添加一条从 A 到 B 的流动线
    ///   DrawLineMgr.Instance.AddLine(
    ///       "route_1",
    ///       new List&lt;Vector3&gt; { pointA, pointB, pointC },
    ///       arrowTexture,
    ///       segments: 64,
    ///       lineWidth: 3f,
    ///       flowMode: LineFlowMode.Forward,
    ///       flowSpeed: 0.8f,
    ///       color: Color.cyan);
    ///
    ///   // 移除
    ///   DrawLineMgr.Instance.RemoveLine("route_1");
    ///
    ///   // 全部清除
    ///   DrawLineMgr.Instance.ClearAll();
    /// </code>
    /// </summary>
    public class DrawLineMgr : BehaviourSingleton<DrawLineMgr>
    {

        [Header("默认线条配置")]
        [Tooltip("默认线条纹理（需要 Wrap Mode = Repeat 才能流动）")]
        public Texture defaultLineTexture;

        [Tooltip("默认线宽（像素）")]
        public float defaultLineWidth = 2f;

        [Tooltip("默认样条插值点数，越大越平滑")]
        public int defaultSegments = 64;

        [Tooltip("默认流动速度")]
        public float defaultFlowSpeed = 0.5f;

        [Tooltip("默认流动模式")]
        public LineFlowMode defaultFlowMode = LineFlowMode.Forward;

        [Tooltip("默认线条颜色")]
        public Color defaultColor = Color.white;

        // 线条数据管理
        private readonly Dictionary<string, LineData> _lines = new Dictionary<string, LineData>();



        #region 生命周期

        private void Awake()
        {
            if (defaultLineTexture == null)
            {
                //defaultLineTexture = ResourceManager.Instance.LoadAssetSync<Texture>(GameAssetPaths.TexturePath + "backImage").AssetObject as Texture;
                defaultLineTexture = Resources.Load<Texture>("Test/line");
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var line in _lines.Values)
                line.Tick(dt);
        }

        protected override void OnDestroy()
        {
            ClearAll();
            base.OnDestroy();
        }

        #endregion


        #region 添加线条

        /// <summary>
        /// 添加一条线条（使用默认配置）。
        /// </summary>
        /// <param name="id">唯一 ID；若已存在同 ID 线条则先移除旧的</param>
        /// <param name="waypoints">路径控制点列表（世界坐标，至少两个点）</param>
        public LineData AddLine(string id, List<Vector3> waypoints)
        {
            return AddLine(id, waypoints, defaultLineTexture, defaultSegments,
                           defaultLineWidth, false, defaultFlowMode, defaultFlowSpeed, defaultColor);
        }

        /// <summary>
        /// 添加一条线条（完整参数）。
        /// </summary>
        /// <param name="id">唯一 ID</param>
        /// <param name="waypoints">路径控制点（世界坐标）</param>
        /// <param name="texture">线条纹理（null 则使用默认纹理）</param>
        /// <param name="segments">样条点数</param>
        /// <param name="lineWidth">线宽（像素）</param>
        /// <param name="closedLoop">是否首尾相连</param>
        /// <param name="flowMode">流动动画模式</param>
        /// <param name="flowSpeed">流动速度</param>
        /// <param name="color">线条颜色</param>
        public LineData AddLine(
            string         id,
            List<Vector3>  waypoints,
            Texture        texture,
            int            segments  = 64,
            float          lineWidth = 2f,
            bool           closedLoop = false,
            LineFlowMode   flowMode  = LineFlowMode.Forward,
            float          flowSpeed = 0.5f,
            Color          color     = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                LogManager.Instance.Error("[DrawLineMgr] 线条 ID 不能为空");
                return null;
            }

            if (waypoints == null || waypoints.Count < 2)
            {
                LogManager.Instance.Error($"[DrawLineMgr] 线条 '{id}' 的路径点不足（至少需要 2 个）");
                return null;
            }

            // 同 ID 已存在则先清除
            if (_lines.ContainsKey(id))
                RemoveLine(id);

            Texture tex = texture != null ? texture : defaultLineTexture;
            Color col = color == default ? defaultColor : color;

            var data = new LineData(id, waypoints, tex, segments, lineWidth, closedLoop);
            data.SetFlowMode(flowMode);
            data.SetFlowSpeed(flowSpeed);
            data.SetColor(col);

            data.Create();
            _lines[id] = data;

            return data;
        }

        #endregion

        #region 移除线条

        /// <summary>
        /// 按 ID 移除并销毁线条。
        /// </summary>
        public bool RemoveLine(string id)
        {
            if (!_lines.TryGetValue(id, out LineData data))
                return false;

            data.Destroy();
            _lines.Remove(id);
            return true;
        }

        /// <summary>
        /// 清除所有线条。
        /// </summary>
        public void ClearAll()
        {
            foreach (var data in _lines.Values)
                data.Destroy();
            _lines.Clear();
        }

        #endregion

        #region 查询与修改

        /// <summary>
        /// 按 ID 获取线条数据（不存在返回 null）。
        /// </summary>
        public LineData GetLine(string id)
        {
            _lines.TryGetValue(id, out LineData data);
            return data;
        }

        /// <summary>
        /// 判断指定 ID 的线条是否存在。
        /// </summary>
        public bool HasLine(string id) => _lines.ContainsKey(id);

        /// <summary>
        /// 获取当前所有线条 ID。
        /// </summary>
        public IEnumerable<string> GetAllIDs() => _lines.Keys;


        /// <summary>
        /// 设置线条颜色。
        /// </summary>
        public void SetLineColor(string id, Color color)
        {
            if (_lines.TryGetValue(id, out LineData data))
                data.SetColor(color);
        }

        /// <summary>
        /// 设置线条宽度。
        /// </summary>
        public void SetLineWidth(string id, float width)
        {
            if (_lines.TryGetValue(id, out LineData data))
                data.SetWidth(width);
        }

        /// <summary>
        /// 设置贴图
        /// </summary>
        public void SetTexture(string id, Texture texture)
        {
            if (_lines.TryGetValue(id, out LineData data))
                data.SetTexture(texture);
        }

        /// <summary>
        /// 设置贴图重复次数（越大越密集）
        /// </summary>
        public void SetTextureScale(string id, float scale)
        {
            if (_lines.TryGetValue(id, out LineData data))
                data.SetTextureScale(scale);
        }

        /// <summary>
        /// 设置流动动画模式。
        /// </summary>
        public void SetFlowMode(string id, LineFlowMode mode)
        {
            if (_lines.TryGetValue(id, out LineData data))
                data.SetFlowMode(mode);
        }

        /// <summary>
        /// 设置流动速度。
        /// </summary>
        public void SetFlowSpeed(string id, float speed)
        {
            if (_lines.TryGetValue(id, out LineData data))
                data.SetFlowSpeed(speed);
        }

        /// <summary>
        /// 显示 / 隐藏线条。
        /// </summary>
        public void SetVisible(string id, bool visible)
        {
            if (_lines.TryGetValue(id, out LineData data))
                data.SetVisible(visible);
        }

        /// <summary>
        /// 更新线条路径并重新绘制。
        /// </summary>
        public void UpdateWaypoints(string id, List<Vector3> newWaypoints)
        {
            if (_lines.TryGetValue(id, out LineData data))
                data.UpdateWaypoints(newWaypoints);
        }

        /// <summary>
        /// 设置被选中的颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetSelectedColor(string id, Color color)
        {
            if(_lines.TryGetValue(id, out LineData data))
                data.SetSelectedColor(color);
        }

        /// <summary>
        /// 设置线条是否可选中（鼠标点击时）
        /// </summary>
        /// <param name="canSelected"></param>
        public void SetCanSelected(string id, bool canSelected)
        {
            if(_lines.TryGetValue(id, out LineData data))
                data.SetCanSelected(canSelected);
        }

        /// <summary>
        /// 设置选中时的额外距离（像素，增加线条的可选中范围）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="extraDistance"></param>
        public void SetExtraDistance(string id, int extraDistance)
        {
            if(_lines.TryGetValue(id, out LineData data))
                data.SetExtraDistance(extraDistance);
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 显示 / 隐藏所有线条。
        /// </summary>
        public void SetAllVisible(bool visible)
        {
            foreach (var data in _lines.Values)
                data.SetVisible(visible);
        }

        /// <summary>
        /// 暂停 / 恢复所有线条流动动画（将 FlowMode 切换为 None / 恢复原模式）。
        /// </summary>
        public void PauseAll(bool pause)
        {
            foreach (var data in _lines.Values)
                data.SetFlowMode(pause ? LineFlowMode.None : defaultFlowMode);
        }

        #endregion
    }
}
