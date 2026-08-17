using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public readonly struct Packet
{
  public readonly uint Cmd;
  public readonly byte[] Body;

  public Packet(uint cmd, byte[] body)
  {
    Cmd = cmd;
    Body = body;
  }
}

public static class PacketCodec
{
  private const int CmdSize = sizeof(uint);

  /// <summary>
  /// Cmd + Body -> byte[]
  /// </summary>
  public static byte[] Encode(uint cmd, byte[] body)
  {
    int bodyLength = body?.Length ?? 0;

    byte[] data = new byte[CmdSize + bodyLength];

    // Cmd
    WriteUInt32LE(data, 0, cmd);

    // Body
    if (bodyLength > 0)
    {
      Buffer.BlockCopy(body, 0, data, CmdSize, bodyLength);
    }

    return data;
  }


  /// <summary>
  /// byte[] -> Packet
  /// </summary>
  public static Packet Decode(byte[] data)
  {
    if (data == null)
    {
      throw new ArgumentNullException(nameof(data));
    }

    if (data.Length < CmdSize)
    {
      throw new ArgumentException($"Packet data too small. " + $"Length: {data.Length}");
    }

    uint cmd = ReadUInt32LE(data, 0);

    int bodyLength = data.Length - CmdSize;

    byte[] body = new byte[bodyLength];

    if (bodyLength > 0)
    {
      Buffer.BlockCopy(data, CmdSize, body, 0, bodyLength);
    }

    return new Packet(cmd, body);
  }


  private static void WriteUInt32LE(byte[] buffer, int offset, uint value)
  {
    buffer[offset] = (byte)(value & 0xFF);

    buffer[offset + 1] = (byte)((value >> 8) & 0xFF);

    buffer[offset + 2] = (byte)((value >> 16) & 0xFF);

    buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
  }


  private static uint ReadUInt32LE(byte[] buffer, int offset)
  {
    return (uint)buffer[offset] |
        ((uint)buffer[offset + 1] << 8) |
        ((uint)buffer[offset + 2] << 16) |
        ((uint)buffer[offset + 3] << 24);
  }
}
