//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Text;
using UnityEngine.Networking;
using ZEngine.Core;


public class StreamDownloadHandler : DownloadHandlerScript
{
    private readonly Action<string> _onChunk;
    private readonly StringBuilder _buffer = new StringBuilder();   //缓冲区


    public StreamDownloadHandler(Action<string> onChunk)
    {
        _onChunk = onChunk;
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength == 0) return false;

        string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
        _buffer.Append(chunk);
        //UnityEngine.Debug.Log($"Received chunk: {chunk}");
        ParseJson();

        return true;
    }

    /// <summary>
    /// 解析返回的Json数据
    /// data: {"id":"2026030200025655cfe7f675724002","created":1772380976,"object":"chat.completion.chunk","model":"glm-5","choices":[{"index":0,"delta":{"role":"assistant","content":"数据"}}]}
    /// </summary>
    private void ParseJson()
    {
        string content = _buffer.ToString();
        int index;

        while ((index = content.IndexOf("\n")) >= 0) // 只处理完整的行
        {
            string line = content.Substring(0, index).Trim();
            content = content.Substring(index + 1);

            if (line.StartsWith("data:"))
            {
                string json = line.Substring(5).Trim();
                if (json == "[DONE]")
                {
                    UnityEngine.Debug.Log("Stream completed.");
                    break;
                }

                _onChunk?.Invoke(json);
            }
        }

        _buffer.Clear();
        _buffer.Append(content);    // 保留未处理的部分
    }
}
