//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;

namespace ZEngine.Extension
{
    public static class ListExtension
    {
        public static List<T> Filter<T>(this List<T> list, Func<T, bool> predicate)
        {
            var result = new List<T>();
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    result.Add(list[i]);
                }
            }
            return result;
        }

        public static List<TResult> Map<T, TResult>(this List<T> list, Func<T, TResult> selector)
        {
            var result = new List<TResult>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                result.Add(selector(list[i]));
            }
            return result;
        }

        public static List<TResult> FlatMap<T, TResult>(this List<T> list, Func<T, List<TResult>> selector)
        {
            var result = new List<TResult>();
            for (int i = 0; i < list.Count; i++)
            {
                List<TResult> sub = selector(list[i]);
                if (sub != null)
                {
                    for (int j = 0; j < sub.Count; j++)
                    {
                        result.Add(sub[j]);
                    }
                }
            }
            return result;
        }

        public static T Reduce<T>(this List<T> list, Func<T, T, T> func)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("集合为空");
            }
            T acc = list[0];
            for (int i = 1; i < list.Count; i++)
            {
                acc = func(acc, list[i]);
            }
            return acc;
        }

        public static TAccumulate Reduce<T, TAccumulate>(
            this List<T> list, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func)
        {
            TAccumulate acc = seed;
            for (int i = 0; i < list.Count; i++)
            {
                acc = func(acc, list[i]);
            }
            return acc;
        }

        public static void ForEach<T>(this List<T> list, Action<T> action)
        {
            for (int i = 0; i < list.Count; i++)
            {
                action(list[i]);
            }
        }

        public static void ForEach<T>(this List<T> list, Action<T, int> action)
        {
            for (int i = 0; i < list.Count; i++)
            {
                action(list[i], i);
            }
        }

        public static bool IsEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static bool IsNotEmpty<T>(this List<T> list)
        {
            return list != null && list.Count > 0;
        }

        public static T GetOrDefault<T>(this List<T> list, int index)
        {
            if (index >= 0 && index < list.Count)
            {
                return list[index];
            }
            return default(T);
        }

        public static T GetOr<T>(this List<T> list, int index, T defaultValue)
        {
            if (index >= 0 && index < list.Count)
            {
                return list[index];
            }
            return defaultValue;
        }

        public static List<T> Slice<T>(this List<T> list, int start, int end)
        {
            int lower = start < 0 ? 0 : start;
            int upper = end > list.Count ? list.Count : end;
            var result = new List<T>(upper - lower);
            for (int i = lower; i < upper; i++)
            {
                result.Add(list[i]);
            }
            return result;
        }

        public static List<T> Sorted<T>(this List<T> list)
        {
            var result = new List<T>(list);
            result.Sort();
            return result;
        }

        public static List<T> Sorted<T>(this List<T> list, Comparison<T> comparison)
        {
            var result = new List<T>(list);
            result.Sort(comparison);
            return result;
        }

        public static List<T> Sorted<T, TKey>(this List<T> list, Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            var result = new List<T>(list);
            result.Sort((a, b) => keySelector(a).CompareTo(keySelector(b)));
            return result;
        }

        public static List<T> ReverseList<T>(this List<T> list)
        {
            var result = new List<T>(list.Count);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                result.Add(list[i]);
            }
            return result;
        }

        public static List<T> DistinctList<T>(this List<T> list)
        {
            var result = new List<T>();
            for (int i = 0; i < list.Count; i++)
            {
                if (!result.Contains(list[i]))
                {
                    result.Add(list[i]);
                }
            }
            return result;
        }

        public static List<T> DistinctBy<T, TKey>(this List<T> list, Func<T, TKey> keySelector)
        {
            var seen = new HashSet<TKey>();
            var result = new List<T>();
            for (int i = 0; i < list.Count; i++)
            {
                TKey key = keySelector(list[i]);
                if (seen.Add(key))
                {
                    result.Add(list[i]);
                }
            }
            return result;
        }

        public static T MinBy<T, TKey>(this List<T> list, Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("集合为空");
            }
            T min = list[0];
            TKey minKey = keySelector(list[0]);
            for (int i = 1; i < list.Count; i++)
            {
                TKey key = keySelector(list[i]);
                if (key.CompareTo(minKey) < 0)
                {
                    minKey = key;
                    min = list[i];
                }
            }
            return min;
        }

        public static T MaxBy<T, TKey>(this List<T> list, Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("集合为空");
            }
            T max = list[0];
            TKey maxKey = keySelector(list[0]);
            for (int i = 1; i < list.Count; i++)
            {
                TKey key = keySelector(list[i]);
                if (key.CompareTo(maxKey) > 0)
                {
                    maxKey = key;
                    max = list[i];
                }
            }
            return max;
        }

        public static bool RemoveWhere<T>(this List<T> list, Func<T, bool> predicate)
        {
            bool removed = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                {
                    list.RemoveAt(i);
                    removed = true;
                }
            }
            return removed;
        }

        public static int IndexOf<T>(this List<T> list, Func<T, bool> predicate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public static int LastIndexOf<T>(this List<T> list, Func<T, bool> predicate)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public static List<T> AddRangeItems<T>(this List<T> list, params T[] items)
        {
            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    list.Add(items[i]);
                }
            }
            return list;
        }

        public static List<T> ShallowCopy<T>(this List<T> list)
        {
            return new List<T>(list);
        }

        public static T[] ToArraySafe<T>(this List<T> list)
        {
            if (list == null)
            {
                return new T[0];
            }
            return list.ToArray();
        }

        public static string JoinStr<T>(this List<T> list, string separator)
        {
            if (list == null || list.Count == 0)
            {
                return string.Empty;
            }
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(separator);
                }
                sb.Append(list[i] == null ? "null" : list[i].ToString());
            }
            return sb.ToString();
        }
    }
}