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
      throw new ArgumentNullException(nameof(parser));
    }

    if (s_parsers.ContainsKey(cmd))
    {
      throw new InvalidOperationException($"Proto cmd already registered: {cmd}");
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
      throw new ArgumentNullException(nameof(message));
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
      throw new ArgumentNullException(nameof(message));
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
      throw new ArgumentNullException(nameof(data));
    }

    if (!s_parsers.TryGetValue(cmd, out MessageParser parser))
    {
      throw new KeyNotFoundException($"Proto parser not found. Cmd: {cmd}");
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
      throw new ArgumentNullException(nameof(data));
    }

    if (parser == null)
    {
      throw new ArgumentNullException(nameof(parser));
    }

    return parser.ParseFrom(data);
  }

  #endregion
}