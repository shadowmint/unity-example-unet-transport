using System;
using System.IO;
using System.Security.AccessControl;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Networking;

namespace Demo.Infrastructure
{
  /// <summary>
  /// It's hopelessly naive to assume that we read the entire message in a single chunk.
  /// This class wraps message chunks to convert them into a uniform byte stream.
  /// </summary>
  public class NetworkMessageReader
  {
    /// <summary>
    /// Number of extra padding bytes for size prefix.
    /// </summary>
    private const int SizeOfUInt32 = 3;

    private int _size = -1;

    private readonly MemoryStream _data = new MemoryStream();

    /// <summary>
    /// Without a hard max length we can arbitrarily grow memory for incoming data
    /// sequences from bad remote actors; cap the maximum message size to a fixed
    /// length.
    /// </summary>
    private readonly int _maxLength;

    public NetworkMessageReader(int maxMessageLength = 65535)
    {
      _maxLength = 65535;
    }

    public bool Read(byte[] data, int length)
    {
      if (_size == -1)
      {
        var reader = new NetworkReader(data);
        _size = (int) reader.ReadPackedUInt32();
        if (_size > _maxLength)
        {
          _size = _maxLength;
        }
        _data.Write(data, SizeOfUInt32, length); // Skip size in memory buffer
      }
      else
      {
        _data.Write(data, 0, length);
      }

      return _data.Length >= _size;
    }

    public byte[] Payload()
    {
      var reader = new NetworkReader(_data.ToArray());
      var bytes = reader.ReadBytes(_size);
      return bytes;
    }
  }
}