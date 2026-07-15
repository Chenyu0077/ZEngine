using MotionFramework.AI;
using UnityEngine;

namespace Hotfix.FuncModule.GridMap
{
    /// <summary>
    /// 将 MapLoader 中的单个格子封装为 AStarNode。
    /// Position 使用格子世界坐标中心，IsBlock 由 MapLoader.IsWalkable 决定。
    /// </summary>
    public class MapAStarNode : AStarNode
    {
        public readonly int GridX;
        public readonly int GridY;

        private readonly Vector3 _worldPos;
        private readonly bool    _isBlock;

        public override Vector3 Position  => _worldPos;
        public override bool    IsBlock() => _isBlock;

        public MapAStarNode(int x, int y, Vector3 worldPos, bool isBlock)
        {
            GridX     = x;
            GridY     = y;
            _worldPos = worldPos;
            _isBlock  = isBlock;
        }
    }
}
