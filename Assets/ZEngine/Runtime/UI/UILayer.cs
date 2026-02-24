namespace ZEngine.UI
{
    /// <summary>
    /// Defines the display layers for UI panels.
    /// Higher values are rendered on top.
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        Normal = 100,
        Popup = 200,
        Toast = 300,
        Loading = 400,
        Top = 500
    }
}
