using UnityEngine;
using ZEngine.Manager.Pool;

namespace Main.FuncModule.Building
{
    public class PlacedObj
    {
        public string          ConfigId;
        public int             GridX;
        public int             GridY;
        public int             SizeX;
        public int             SizeY;
        public SpawnGameObject SpawnHandle; // 对象池句柄（池模式）
        public GameObject      Instance;    // 实际 GameObject（异步加载完成后填充）
    }
}
