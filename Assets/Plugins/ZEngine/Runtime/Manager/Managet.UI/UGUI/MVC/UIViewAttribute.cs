using System;

namespace ZEngine.Manager.UI.UGUI
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UIViewAttribute : Attribute
    {
        public string Location { get; }
        public UUILayer Layer { get; }
        public bool IsSingleton { get; }
        public bool IsFullScreen { get; }

        public UIViewAttribute(
            string location,
            UUILayer layer = UUILayer.Bottom_Layer,
            bool isSingleton = true,
            bool isFullScreen = false)
        {
            Location = location;
            Layer = layer;
            IsSingleton = isSingleton;
            IsFullScreen = isFullScreen;
        }
    }
}
