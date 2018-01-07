using System.IO;
using UnityEngine.Networking;

namespace Demo.Infrastructure
{
  /// <summary>
  /// Convert a byte stream into a byte stream with a leading size.
  /// </summary>
  public class NetworkMessageWriter
  {
    private readonly MemoryStream _data = new MemoryStream();

    public void Write(byte[] data, int length)
    {
      _data.Write(data, 0, length);
    }

    public byte[] Payload()
    {
      var data = _data.ToArray();
      var writer = new NetworkWriter();
      writer.WritePackedUInt32((uint) data.Length);
      writer.WriteBytesFull(data);
      return writer.ToArray();
    }
  }
}