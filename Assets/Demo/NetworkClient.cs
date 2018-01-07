using System;
using Demo.Infrastructure;
using UnityEngine;
using UnityEngine.Networking;

namespace Demo
{
  public class NetworkClient : MonoBehaviour, INetworkEventHandler
  {
    [Tooltip("The connection manager to use")]
    public NetworkHostManager HostManager;

    [Tooltip("The remote server address to connect to")]
    public string RemoteHost = "127.0.0.1";

    [Tooltip("The remote server port to bind connect to")]
    public int RemotePort = 8080;

    [Tooltip("This is the current state of the client")]
    public bool Running;

    [Tooltip("Change this to change the active state of the client")]
    public bool ShouldBeRunning;

    private readonly NetworkConnectionManager _connectionManager = new NetworkConnectionManager();

    private bool _running;

    private int _clientId = -1;

    /// <summary>
    /// This is probably the most poorly understood part of the transport API.
    /// As a client, we must explicitly open a local server port to receive messages from the server from.
    /// In this case we pick max connections as 1 (server we connect to) and port 0 (arbitrary local port).
    /// </summary>
    void StartServer()
    {
      try
      {
        // Notice that the NeworkEventHandler has already called NetworkTransport.Init()
        var maxIncomingConnections = 1; // Only one server can ever connect to us.
        var hostTopology = new HostTopology(NetworkConfig.Config, maxIncomingConnections);
        _clientId = NetworkTransport.AddHost(hostTopology, 0);
        HostManager?.AddHost(_clientId, this);
        Debug.Log($"Client running on localhost");
      }
      catch (Exception error)
      {
        Debug.LogError(error);
        StopServer();
      }

      _running = true;
    }

    /// <summary>
    /// Stop this receiving port for this client.
    /// Close all connections targeting this port.
    /// </summary>
    void StopServer()
    {
      try
      {
        NetworkTransport.RemoveHost(_clientId);
      }
      catch (Exception error)
      {
        Debug.LogError(error);
      }

      HostManager?.RemoveHost(_clientId);
      _connectionManager.Clear();
      _clientId = -1;
      _running = false;
      ShouldBeRunning = false;
      Debug.Log("Client halted.");
    }

    private void ConnectToRemoteHost()
    {
      try
      {
        byte errorCode;
        NetworkTransport.Connect(_clientId, RemoteHost, RemotePort, 0, out errorCode);
        var error = (NetworkError) errorCode;
        if (error != NetworkError.Ok)
        {
          Debug.Log($"Connection failed: {error}");
          StopServer();
        }
      }
      catch (Exception error)
      {
        Debug.Log(error);
        StopServer();
      }
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
        ConnectToRemoteHost();
      }
      else
      {
        StopServer();
      }
    }

    public void OnConnectEvent(int connectionId, int channelId)
    {
      _connectionManager.Manage<NetworkClientConnection>(_clientId, connectionId, channelId);
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