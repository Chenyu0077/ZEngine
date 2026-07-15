using Main.Core;
using Hotfix.FuncModule;
using UnityEngine;

namespace Hotfix.Main.Logic
{
    public class GameManager : Singleton<GameManager>
    {
        /// <summary>
        /// 获取随机出生点（Map上可行走的格子）
        /// </summary>
        /// <returns></returns>
        public Vector3 GetRandomSpawnPos()
        {
            var loader = MapLoader.Instance;

            // 优先使用地图中标记的 npc 出生点
            var spawnPoints = loader.GetSpawnPointsByType("npc");
            if (spawnPoints != null && spawnPoints.Count > 0)
            {
                var sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
                return loader.GetSpawnWorldPos(sp);
            }

            // 回退：随机取可行走格
            for (int i = 0; i < 200; i++)
            {
                int x = UnityEngine.Random.Range(0, loader.MapWidth);
                int y = UnityEngine.Random.Range(0, loader.MapHeight);
                if (loader.IsWalkable(x, y))
                    return loader.GridToWorld(x, y);
            }

            return loader.GridToWorld(loader.MapWidth / 2, loader.MapHeight / 2);
        }
        
    }
}