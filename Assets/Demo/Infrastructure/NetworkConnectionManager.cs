using System;
using System.Collections.Generic;

namespace Demo.Infrastructure
{
  public class NetworkConnectionManager
  {
    private readonly IDictionary<string, INetworkProtocol> _connections = new Dictionary<string, INetworkProtocol>();

    public void Clear()
    {
      _connections.Clear();
    }

    public void Manage<T>(int hostId, int connectionId, int channelId) where T : INetworkProtocol
    {
      var instance = Activator.CreateInstance<T>() as INetworkProtocol;
      _connections[Key(connectionId, channelId)] = instance;
      instance.Channel = new NetworkChannel(hostId, connectionId, channelId, () => Remove(connectionId, channelId));
      instance.OnConnected();
    }

    public void Disconnect(int connectionId, int channelId)
    {
      var key = Key(connectionId, channelId);
      if (!_connections.ContainsKey(key)) return;
      var instance = _connections[key];
      Remove(connectionId, channelId);
      instance.OnDisconnected();
    }

    private void Remove(int connectionId, int channelId)
    {
      var key = Key(connectionId, channelId);
      if (!_connections.ContainsKey(key)) return;
      _connections.Remove(key);
    }

    public void Update()
    {
      foreach (var instance in _connections.Values)
      {
        instance.Update();
      }
    }

    public void DataReceived(int connectionId, int channelId, byte[] buffer, int dataSize)
    {
      var key = Key(connectionId, channelId);
      if (!_connections.ContainsKey(key)) return;
      var instance = _connections[key];
      instance.OnDataReceived(buffer, dataSize);
    }

    private string Key(int connectionId, int channelId)
    {
      return $"{channelId}:{connectionId}";
    }
  }
}