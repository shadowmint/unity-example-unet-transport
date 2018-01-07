using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

namespace Demo.Infrastructure
{
  /// <summary>
  /// Due to poor design, there's no way to 'native' way to bind the incoming events 
  /// from calling `NetworkTransport.Receive` to a specific target.
  /// 
  /// This usually results in people running separate instances for server and client
  /// instances, as you can just assumed all responses are for the current client / server.
  /// 
  /// However, since we want to be a little less rubbish here, we actually delegate the
  /// events out to the specific INetworkEventHandler with the associated host id.
  /// </summary>
  public class NetworkHostManager : MonoBehaviour
  {
    private IDictionary<int, INetworkEventHandler> _hosts = new Dictionary<int, INetworkEventHandler>();

    public void AddHost(int id, INetworkEventHandler eventHandler)
    {
      _hosts[id] = eventHandler;
    }

    public void RemoveHost(int id)
    {
      if (_hosts.ContainsKey(id))
      {
        _hosts.Remove(id);
      }
    }

    public void Update()
    {
      byte[] buffer = new byte[1024];

      // Notice we process all network events until we get a 'Nothing' response here.
      // Often people just process a single event per frame, and that results in very poor performance.
      var noEventsLeft = false;
      while (!noEventsLeft)
      {
        byte errorCode;
        int dataSize;
        int channelId;
        int connectionId;
        int hostId;

        var netEventType = NetworkTransport.Receive(
          out hostId,
          out connectionId,
          out channelId,
          buffer,
          buffer.Length,
          out dataSize,
          out errorCode);

        var error = (NetworkError) errorCode;
        if (error != NetworkError.Ok)
        {
          Debug.Log($"NetworkTransport error: {error}");
          return;
        }

        switch (netEventType)
        {
          case NetworkEventType.Nothing:
            noEventsLeft = true;
            break;
          case NetworkEventType.ConnectEvent:
            OnConnectEvent(hostId, connectionId, channelId);
            break;
          case NetworkEventType.DataEvent:
            OnDataEvent(hostId, connectionId, channelId, buffer, dataSize);
            break;
          case NetworkEventType.DisconnectEvent:
            OnDisconnectEvent(hostId, connectionId, channelId);
            break;
        }
      }
    }

    private void OnConnectEvent(int hostId, int connectionId, int channelId)
    {
      if (!_hosts.ContainsKey(hostId))
      {
        Debug.Log($"HostManager: Unknown host id: {hostId}");
        return;
      }

      _hosts[hostId].OnConnectEvent(connectionId, channelId);
    }

    private void OnDataEvent(int hostId, int connectionId, int channelId, byte[] buffer, int dataSize)
    {
      if (!_hosts.ContainsKey(hostId))
      {
        Debug.Log($"HostManager: Unknown host id: {hostId}");
        return;
      }

      _hosts[hostId].OnDataEvent(connectionId, channelId, buffer, dataSize);
    }

    private void OnDisconnectEvent(int hostId, int connectionId, int channelId)
    {
      if (!_hosts.ContainsKey(hostId))
      {
        Debug.Log($"HostManager: Unknown host id: {hostId}");
        return;
      }

      _hosts[hostId].OnDisconnectEvent(connectionId, channelId);
    }
  }
}