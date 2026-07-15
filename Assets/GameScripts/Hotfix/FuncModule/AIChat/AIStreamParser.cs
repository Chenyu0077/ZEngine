//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using ZEngine.Core;

/// <summary>
/// 提供从流式 AI JSON 响应中解析和累积文本内容的功能
/// </summary>
/// <remarks>
/// 使用此类处理以 JSON 格式接收的增量 AI 响应数据，并在数据到达时累积生成的文本。
/// 解析器维护一个内部缓冲区，该缓冲区可以被清空或完整检索。此类不是线程安全的；如果在多个线程中使用，需要外部同步。
/// </remarks>
public class AIStreamParser
{
    private StringBuilder _buffer = new StringBuilder();

    public string Append(string json)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<AIResponse>(json);

            var choice = data?.choices?[0];

            if (choice == null)
                return null;

            // 结束判断（finish_reason == "Stop"表示结束）
            if (choice.finish_reason != null)
                return null;

            var delta = choice.delta?.content;

            if (!string.IsNullOrEmpty(delta))
            {
                _buffer.Append(delta);
                return delta;
            }
        }
        catch(Exception ex)
        {
            ZEngineLog.Warning($"解析流式数据时发生异常: {ex.Message} \n {json}");
        }

        return null;
    }

    public string GetFullText()
    {
        return _buffer.ToString();
    }

    public void Clear()
    {
        _buffer.Clear();
    }
}



#region Response结构
/// <summary>
/// 流式返回的AI响应数据结构
/// {"id":"2026030200025655cfe7f675724002","created":1772380976,"object":"chat.completion.chunk","model":"glm-5","choices":[{"index":0,"delta":{"role":"assistant","content":"数据"}}]}
/// </summary>
public class AIResponse
{
    public string id;
    public long created;
    public string @object;
    public string model;
    public List<Choice> choices;
}


public class Choice
{
    public int index;
    public Delta delta;
    public string finish_reason;
}


public class Delta
{
    public string role;
    public string content;

    // 未来扩展（函数调用）
    public ToolCall[] tool_calls;
}


public class ToolCall
{
    public string id;
    public string type;
    public Function function;
}


public class Function
{
    public string name;
    public string arguments;
}

#endregion


#region Request结构
/// <summary>
/// 请求体数据结构
/// </summary>
public class ChatRequest
{
    public string model;
    public List<ChatMessage> messages;
    public bool stream = true;
}

[MessagePackObject]
public class ChatMessage
{
    [Key(0)] public string role;
    [Key(1)] public string content;
    public ChatMessage() { }

    // role: system, user, assistant
    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

#endregion


#region History结构
[MessagePackObject]
public class ChatHistory
{
    [Key(0)] public string id;   // 会话ID
    [Key(1)] public List<ChatHistoryMessage> messages;
}

[MessagePackObject]
public class ChatHistoryMessage
{
    [Key(0)] public string timeStamp;
    [Key(1)] public ChatMessage message;

    public ChatHistoryMessage() { }

    public ChatHistoryMessage(string timeStamp = "", ChatMessage message = null)
    {
        if(string.IsNullOrEmpty(timeStamp))
            this.timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        else
            this.timeStamp = timeStamp;
        this.message = message;
    }
}

#endregion