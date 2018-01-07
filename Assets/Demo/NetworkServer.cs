using System;
using Demo.Infrastructure;
using UnityEngine;
using UnityEngine.Networking;

namespace Demo
{
  public class NetworkServer : MonoBehaviour, INetworkEventHandler
  {
    [Tooltip("The connection manager to use")]
    public NetworkHostManager HostManager;

    [Tooltip("The local server port to bind to")]
    public int Port = 8080;

    [Tooltip("This is the current state of the server")]
    public bool Running;

    [Tooltip("Change this to change the active state of the server")]
    public bool ShouldBeRunning;

    private readonly NetworkConnectionManager _connectionManager = new NetworkConnectionManager();

    private bool _running;

    private int _serverId = -1;

    void StartServer()
    {
      try
      {
        // Notice that the NeworkEventHandler has already called NetworkTransport.Init()
        var maxIncomingConnections = 10;
        var hostTopology = new HostTopology(NetworkConfig.Config, maxIncomingConnections);
        _serverId = NetworkTransport.AddHost(hostTopology, Port);
        HostManager?.AddHost(_serverId, this);
        Debug.Log($"Server running localhost:{Port}");
      }
      catch (Exception error)
      {
        Debug.LogError(error);
        StopServer();
      }

      _running = true;
    }

    void StopServer()
    {
      try
      {
        NetworkTransport.RemoveHost(_serverId);
      }
      catch (Exception error)
      {
        Debug.LogError(error);
      }

      HostManager?.RemoveHost(_serverId);
      _connectionManager.Clear();
      _serverId = -1;
      _running = false;
      ShouldBeRunning = false;
      Debug.Log("Server halted.");
    }

    void Update()
    {
      Running = _running;

      // If we have any open connections, process them.
      if (_running)
      {
        _connectionManager.Update();
      }

      // Start or stop depending on target state      
      if (ShouldBeRunning == _running) return;
      if (ShouldBeRunning)
      {
        StartServer();
      }
      else
      {
        StopServer();
      }
    }

    public void OnConnectEvent(int connectionId, int channelId)
    {
      _connectionManager.Manage<NetworkServerConnection>(_serverId, connectionId, channelId);
    }

    public void OnDataEvent(int connectionId, int channelId, byte[] buffer, int dataSize)
    {
      _connectionManager.DataReceived(connectionId, channelId, buffer, dataSize);
    }

    public void OnDisconnectEvent(int connectionId, int channelId)
    {
      _connectionManager.Disconnect(connectionId, channelId);
    }
  }
}