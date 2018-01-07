using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Demo.Infrastructure
{
  public class NetworkChannel
  {
    private readonly int _hostId;
    private readonly int _connectionId;
    private readonly int _channelId;
    private readonly Action _onClose;

    public NetworkChannel(int hostId, int connectionId, int channelId, Action onClose)
    {
      _hostId = hostId;
      _connectionId = connectionId;
      _channelId = channelId;
      _onClose = onClose;
    }

    public void Close()
    {
      try
      {
        byte errorCode;
        NetworkTransport.Disconnect(_hostId, _connectionId, out errorCode);
        var error = (NetworkError) errorCode;
        if (error != NetworkError.Ok)
        {
          Debug.Log($"NetworkTransport error: {error}");
        }
      }
      catch (Exception error)
      {
        Debug.LogError(error);
      }

      _onClose();
    }

    public void Send(byte[] buffer, int length)
    {
      try
      {
        byte errorCode;

        var message = new NetworkMessageWriter();
        message.Write(buffer, length);
        var messageBuffer = message.Payload();

        NetworkTransport.Send(_hostId, _connectionId, _channelId, messageBuffer, messageBuffer.Length, out errorCode);
        var error = (NetworkError) errorCode;
        if (error != NetworkError.Ok)
        {
          Debug.Log($"NetworkTransport error: {error}");
        }
      }
      catch (Exception error)
      {
        Debug.LogError(error);
      }
    }
  }
}