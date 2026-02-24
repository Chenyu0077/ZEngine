using UnityEditor;
using UnityEngine;

namespace ZEngine.Editor
{
    /// <summary>
    /// ZEngine framework editor utilities and menu items.
    /// </summary>
    public static class ZEngineEditor
    {
        private const string MenuRoot = "ZEngine/";

        [MenuItem(MenuRoot + "Create GameRoot")]
        public static void CreateGameRoot()
        {
            var existing = Object.FindObjectOfType<Core.GameRoot>();
            if (existing != null)
            {
                Debug.Log("[ZEngine] GameRoot already exists in the scene.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var go = new GameObject("[GameRoot]");
            go.AddComponent<Core.GameRoot>();
            go.AddComponent<UI.UIManager>();
            go.AddComponent<Audio.AudioManager>();
            go.AddComponent<Timer.TimerManager>();
            go.AddComponent<Scene.SceneManager>();
            go.AddComponent<Resource.ResourceManager>();

            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create GameRoot");
            Debug.Log("[ZEngine] GameRoot created in scene.");
        }

        [MenuItem(MenuRoot + "Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/Chenyu0077/ZEngine");
        }
    }
}
