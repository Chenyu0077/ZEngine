//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ZEngine.Utility;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// Buff工厂类
    /// </summary>
    public static class BuffFactory
    {
        private static Dictionary<BuffType, Type> buffTypeMap = new Dictionary<BuffType, Type>();

        static BuffFactory()
        {
            AutoRegisterBuffs();
        }

        /// <summary>
        /// 初始化自动注册Buff类型
        /// </summary>
        private static void AutoRegisterBuffs()
        {
            var buffBaseType = typeof(BuffBase);
            var types = AssemblyUtility.GetTypes("HotUpdate");

            foreach (var type in types)
            {
                if (!buffBaseType.IsAssignableFrom(type) || type.IsAbstract)
                    continue;

                var attrs = type.GetCustomAttributes<BuffTypeAttribute>();
                if(attrs != null)
                {
                    var attr = attrs.First();
                    buffTypeMap[attr.BuffType] = type;
                }
            }
        }

        /// <summary>
        /// 创建Buff实例
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="caster"></param>
        /// <param name="buffData"></param>
        /// <returns></returns>
        public static BuffBase CreateBuff(IBuffHandler handler, GameObject caster, BuffData buffData)
        {
            if(buffTypeMap.TryGetValue(buffData.buffType, out var type))
            {
                var buff = (BuffBase)Activator.CreateInstance(type, handler, caster, buffData);
                return buff;
            }
            else
            {
                Debug.LogWarning($"未注册的Buff类型：{buffData.buffType}, 使用默认的Buff");
                return new DefaultBuff(handler, caster, buffData);
            }
        }
    }
}
