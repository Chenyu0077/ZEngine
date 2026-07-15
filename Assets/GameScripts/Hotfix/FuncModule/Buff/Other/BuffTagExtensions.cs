//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

namespace Hotfix.FuncModule
{
    /// <summary>
    /// BuffTag 枚举的扩展方法（位运算辅助）
    /// </summary>
    public static class BuffTagExtensions
    {
        /// <summary>
        /// 判断当前标签是否包含指定标签
        /// </summary>
        public static bool HasTag(this BuffTag self, BuffTag checkedTag)
        {
            return (self & checkedTag) != 0;
        }

        /// <summary>
        /// 添加标签
        /// </summary>
        public static BuffTag AddTag(this BuffTag self, BuffTag addTag)
        {
            return self | addTag;
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        public static BuffTag RemoveTag(this BuffTag self, BuffTag removeTag)
        {
            return self & ~removeTag;
        }
    }
}
