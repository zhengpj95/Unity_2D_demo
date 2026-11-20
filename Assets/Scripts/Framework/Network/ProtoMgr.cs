using System;
using System.Collections.Generic;
using Google.Protobuf;

public static class ProtoMgr
{
  /// <summary>
  /// Cmd -> MessageParser
  /// </summary>
  private static readonly Dictionary<uint, MessageParser> s_parsers = new();


  #region Register

  /// <summary>
  /// 注册 Cmd 对应的 Parser
  /// </summary>
  public static void Register(uint cmd, MessageParser parser)
  {
    if (parser == null)
    {
      UnityEngine.Debug.LogError($"[ProtoMgr] 注册失败：Cmd={cmd} 的 Parser 为空。");
      return;
    }

    if (s_parsers.ContainsKey(cmd))
    {
      UnityEngine.Debug.LogError($"[ProtoMgr] 注册失败：Cmd={cmd} 已重复注册，已忽略本次注册。");
      return;
    }

    s_parsers.Add(cmd, parser);
  }


  /// <summary>
  /// 移除注册
  /// </summary>
  public static bool Unregister(uint cmd)
  {
    return s_parsers.Remove(cmd);
  }


  /// <summary>
  /// 判断 Cmd 是否已经注册
  /// </summary>
  public static bool Contains(
      uint cmd)
  {
    return s_parsers.ContainsKey(cmd);
  }


  /// <summary>
  /// 清空所有注册
  /// </summary>
  public static void Clear()
  {
    s_parsers.Clear();
  }

  #endregion


  #region Encode

  /// <summary>
  /// Protobuf Message -> byte[]
  /// </summary>
  public static byte[] Encode(IMessage message)
  {
    if (message == null)
    {
      UnityEngine.Debug.LogError("[ProtoMgr] Encode 失败：message 为空。");
      return null;
    }

    return message.ToByteArray();
  }


  /// <summary>
  /// 泛型 Encode
  /// </summary>
  public static byte[] Encode<T>(T message) where T : IMessage
  {
    if (message == null)
    {
      UnityEngine.Debug.LogError("[ProtoMgr] Encode<T> 失败：message 为空。");
      return null;
    }

    return message.ToByteArray();
  }

  #endregion


  #region Decode

  /// <summary>
  /// 根据 Cmd 自动找到 Parser 并解析
  /// </summary>
  public static IMessage Decode(uint cmd, byte[] data)
  {
    if (data == null)
    {
      UnityEngine.Debug.LogError($"[ProtoMgr] Decode 失败：Cmd={cmd} 的 data 为空。");
      return null;
    }

    if (!s_parsers.TryGetValue(cmd, out MessageParser parser))
    {
      UnityEngine.Debug.LogError($"[ProtoMgr] Decode 失败：Cmd={cmd} 未注册 Parser。");
      return null;
    }

    return parser.ParseFrom(data);
  }


  /// <summary>
  /// 使用指定 Parser 解析
  /// </summary>
  public static T Decode<T>(byte[] data, MessageParser<T> parser) where T : IMessage<T>
  {
    if (data == null)
    {
      UnityEngine.Debug.LogError($"[ProtoMgr] Decode<T> 失败：data 为空。");
      return default;
    }

    if (parser == null)
    {
      UnityEngine.Debug.LogError($"[ProtoMgr] Decode<T> 失败：parser 为空。");
      return default;
    }

    return parser.ParseFrom(data);
  }

  #endregion
}