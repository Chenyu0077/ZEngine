namespace ZEngine.Core
{
    /// <summary>
    /// Interface for all ZEngine modules.
    /// </summary>
    public interface IModule
    {
        /// <summary>Called when the module is registered with GameRoot.</summary>
        void OnCreate();

        /// <summary>Called every frame by GameRoot.</summary>
        void OnUpdate(float deltaTime);

        /// <summary>Called every fixed frame by GameRoot.</summary>
        void OnFixedUpdate(float fixedDeltaTime);

        /// <summary>Called when the module is unregistered or the application quits.</summary>
        void OnDispose();
    }
}
