//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Main.Core;
using MessagePack;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 对话历史管理器（对历史记录条数做了限制）
/// </summary>
public class ChatHistoryMgr : Singleton<ChatHistoryMgr>
{
    /// <summary>
    /// 对话历史记录（初始时从本地文件中读取，对话过程中会改变，每次对话结束保存到本地文件）
    /// </summary>
    private Dictionary<string, ChatHistory> _chatHistories = new Dictionary<string, ChatHistory>();
    private readonly object _historyLock = new object();
    private const int MaxHistoryCount = 100;    // 最大历史记录数量，超过后会删除最旧的记录（可根据需要调整）
    private string _chatHistoryDir = Application.persistentDataPath + "/ChatHistories/";    // 历史记录保存目录
    private string _chatHistoryFile = "chat_histories.bytes";    // 历史记录文件名（目前写死）


    public ChatHistoryMgr() : base()
    {
        InitChatHistory();
    }


    protected override void DestroySingleton()
    {
        lock (_historyLock)
        {
            _chatHistories.Clear();
        }

        base.DestroySingleton();
    }


    #region 历史记录相关

    /// <summary>
    /// 初始化对话历史记录
    /// </summary>
    private void InitChatHistory()
    {
        lock (_historyLock)
        {
            if (!Directory.Exists(_chatHistoryDir))
                Directory.CreateDirectory(_chatHistoryDir);

            string path = Path.Combine(_chatHistoryDir, _chatHistoryFile);
            if (!File.Exists(path))
            {
                List<ChatHistory> histories = new List<ChatHistory>();
                _chatHistories.ForEach(pair => histories.Add(pair.Value));
                // 初始化创建文件
                File.WriteAllBytes(path, MessagePackSerializer.Serialize<List<ChatHistory>>(histories));
            }
            else
            {
                try
                {
                    var bytes = File.ReadAllBytes(path);
                    var histories = MessagePackSerializer.Deserialize<List<ChatHistory>>(bytes);
                    _chatHistories.Clear();
                    histories.ForEach(h => _chatHistories[h.id] = h);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"加载对话历史记录失败: {e}");
                }
            }
        }
    }

    /// <summary>
    /// 获取对话历史记录
    /// </summary>
    /// <param name="chatId"></param>
    /// <returns></returns>
    public ChatHistory GetChatHistory(string chatId)
    {
        lock (_historyLock)
        {
            if (_chatHistories.TryGetValue(chatId, out var history))
            {
                return new ChatHistory
                {
                    id = history.id,
                    messages = new List<ChatHistoryMessage>(history.messages)
                };
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 添加对话历史记录
    /// </summary>
    /// <param name="chatId"></param>
    /// <param name="messages"></param>
    public void AddChatHistory(string chatId, List<ChatHistoryMessage> messages)
    {
        lock (_historyLock)
        {
            if (_chatHistories.TryGetValue(chatId, out var history))
            {
                history.messages.AddRange(messages);
            }
            else
            {
                history = new ChatHistory { id = chatId, messages = new List<ChatHistoryMessage>(messages) };
                _chatHistories[chatId] = history;
            }

            if(history.messages.Count > MaxHistoryCount)
            {
                history.messages.RemoveRange(0, history.messages.Count - MaxHistoryCount);
            }
        }
    }

    /// <summary>
    /// 删除对话历史记录
    /// </summary>
    /// <param name="chatId"></param>
    /// <returns></returns>
    public bool RemoveChatHistory(string chatId)
    {
        lock (_historyLock)
        {
            return _chatHistories.Remove(chatId);
        }
    }

    /// <summary>
    /// 保存对话历史记录到本地文件
    /// </summary>
    /// <returns></returns>
    public bool SaveChatHistory()
    {
        lock (_historyLock)
        {
            string path = Path.Combine(_chatHistoryDir, _chatHistoryFile);
            try
            {
                if (!Directory.Exists(_chatHistoryDir))
                    Directory.CreateDirectory(_chatHistoryDir);

                List<ChatHistory> histories = new List<ChatHistory>();
                _chatHistories.ForEach(pair => histories.Add(pair.Value));
                File.WriteAllBytes(path, MessagePackSerializer.Serialize<List<ChatHistory>>(histories));
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"保存对话历史记录失败: {e}");
                return false;
            }
        }
    }
    #endregion
}
