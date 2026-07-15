//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using MessagePack;
using ZEngine.Module.Archive;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// 存档基类
    /// </summary>
    [MessagePackObject]
    [Union(1, typeof(SystemArchive))]
    public abstract class ArchiveBase : IArchiveBase
    {
        [Key(0)]
        public string InitVersion { get; set; } // 创建时的版本号
        [Key(1)]
        public string CurVersion { get; set; } // 当前版本号
        [Key(2)]
        public int ArchiveVersion { get; set; } // 存档Schema版本号（用来处理存档结构的兼容性）


        /// <summary>
        /// Schema 迁移函数注册表
        /// </summary>
        private Dictionary<(int from, int to), Action> migrations = new Dictionary<(int from, int to), Action>();



        public void Init(string gameVersion, int schemaVersion)
        {
            InitVersion = gameVersion;
            CurVersion = gameVersion;
            ArchiveVersion = schemaVersion;
            InitOnce();
        }

        /// <summary>
        /// 新建存档时调用一次
        /// </summary>
        public abstract void InitOnce();

        /// <summary>
        /// 反序列化调用（自动迁移 + 数据修复）
        /// </summary>
        /// <param name="currentGameVersion"></param>
        /// <param name="lastSchemaVersion"></param>
        public void AfterDeserialize(string currentGameVersion, int lastSchemaVersion)
        {
            // 1.检查游戏版本升级
            OnApplicationVersionChange(CurVersion, currentGameVersion);
            CurVersion = currentGameVersion;

            // 2.Schema迁移
            if(ArchiveVersion < lastSchemaVersion)
            {
                RunMigrations(ArchiveVersion, lastSchemaVersion);
                ArchiveVersion = lastSchemaVersion;
            }

            OnAfterDeserialize();
        }

        public void BeforeSerialize()
        {
            OnBeforeSerialize();
        }

        /// <summary>
        /// 游戏版本升级时触发，用来作数据修复
        /// </summary>
        protected virtual void OnApplicationVersionChange(string oldVersion, string newVersion) { }


        /// <summary>
        /// 注册迁移函数
        /// </summary>
        protected void RegisterMigration(int fromVersion, int toVersion, Action migrationAction)
        {
            migrations[(fromVersion, toVersion)] = migrationAction;
        }


        /// <summary>
        /// 运行迁移函数
        /// </summary>
        public void RunMigrations(int fromVersion, int toVersion)
        {
            for (int v = fromVersion; v < toVersion; v++)
            {
                if (migrations.TryGetValue((v, v + 1), out var action))
                {
                    action.Invoke();
                }
                else
                {
                    throw new Exception($"缺少从 {v} 到 {v + 1} 的迁移函数!");
                }
            }
            ArchiveVersion = toVersion;
        }

        public virtual void OnBeforeSerialize()
        {
            
        }

        public virtual void OnAfterDeserialize()
        {
            
        }
    }
}
