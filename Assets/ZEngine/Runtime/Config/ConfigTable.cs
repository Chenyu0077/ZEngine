using UnityEngine;

namespace ZEngine.Config
{
    /// <summary>
    /// Base class for ScriptableObject-based configuration tables.
    /// </summary>
    public abstract class ConfigTable : ScriptableObject
    {
        public abstract void OnLoad();
    }
}
