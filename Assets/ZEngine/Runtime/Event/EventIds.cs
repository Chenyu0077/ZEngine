namespace ZEngine.Event
{
    /// <summary>
    /// Defines built-in framework event IDs for ZEngine.
    /// Game-specific events should start at EventIds.GameEventStart.
    /// </summary>
    public static class EventIds
    {
        // ---- Scene Events ----
        public const int SceneLoadStart   = 1001;
        public const int SceneLoadFinish  = 1002;
        public const int SceneUnloadStart = 1003;
        public const int SceneUnloadFinish = 1004;

        // ---- UI Events ----
        public const int UIPanelOpen  = 2001;
        public const int UIPanelClose = 2002;

        // ---- Resource Events ----
        public const int ResourceLoadStart   = 3001;
        public const int ResourceLoadFinish  = 3002;
        public const int ResourceUnloadStart = 3003;

        // ---- Audio Events ----
        public const int AudioPlayMusic  = 4001;
        public const int AudioStopMusic  = 4002;
        public const int AudioPlaySound  = 4003;

        // ---- Network Events ----
        public const int NetworkConnected    = 5001;
        public const int NetworkDisconnected = 5002;
        public const int NetworkError        = 5003;

        /// <summary>
        /// Game-specific events should use IDs starting from this value.
        /// </summary>
        public const int GameEventStart = 10000;
    }
}
