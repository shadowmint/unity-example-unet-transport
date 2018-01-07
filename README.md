# Unity UNET Transport API example

A single process server / client example of using the UNET Transport API, see https://docs.unity3d.com/Manual/UNetUsingTransport.html

Requires the 4.6 experimental scripting api under Edit > Project Settings > Player > Other Settings > Scripting Runtime Version

## TLDR?

Client:

    public class NetworkClientConnection : INetworkProtocol
    {
      private string _id;

      private float _elapsed;

      private NetworkMessageReader _message;

      public NetworkChannel Channel { get; set; }

      public void OnConnected()
      {
        _id = Guid.NewGuid().ToString();
        Debug.Log($"Client {_id}: Connected");
      }

      public void OnDisconnected()
      {
        Debug.Log($"Client {_id}: Disconnected");
      }

      public void Update()
      {
        _elapsed += Time.deltaTime;
        if (_elapsed > 2.0f)
        {
          _elapsed = 0f;
          PingServer();
        }
      }

      private void PingServer()
      {
        var bytes = Encoding.UTF8.GetBytes("PING");
        Channel.Send(bytes, bytes.Length);
        Debug.Log($"Client {_id}: sent PING");
      }

      public void OnDataReceived(byte[] buffer, int dataSize)
      {
        if (_message == null)
        {
          _message = new NetworkMessageReader();
        }

        if (_message.Read(buffer, dataSize))
        {
          var bytes = _message.Payload();
          var output = Encoding.UTF8.GetString(bytes);
          Debug.Log($"Client {_id}: got: {output}");
          _message = null;
        }
      }
    }

Server:

    public class NetworkServerConnection : INetworkProtocol
    {
      private NetworkMessageReader _message;
      private string _id;

      public void OnConnected()
      {
        _id = Guid.NewGuid().ToString();
        Debug.Log($"Server: Client {_id} connected");
      }

      public void OnDisconnected()
      {
        Debug.Log($"Server: Client {_id} disconnected");
      }

      public void Update()
      {
      }

      public void OnDataReceived(byte[] buffer, int dataSize)
      {
        if (_message == null)
        {
          _message = new NetworkMessageReader();
        }

        if (_message.Read(buffer, dataSize))
        {
          var bytes = _message.Payload();
          var output = Encoding.UTF8.GetString(bytes);
          Debug.Log($"Server: Received: {output}");
          if (output == "PING")
          {
            RespondToClient();
          }

          _message = null;
        }
      }

      private void RespondToClient()
      {
        var bytes = Encoding.UTF8.GetBytes("PONG");
        Channel.Send(bytes, bytes.Length);
        Debug.Log($"Server: Sent: PONG");
      }

      public NetworkChannel Channel { get; set; }
  }

It's a bit more complicated than that, but really, using the UNet transport API isn't that hard.

This is just a basic example of using the transport API to send arbitrary messages back and forth,
but for some reason, there are very few examples of this that actually work.

So, here you go~

## Transport API Notes

The UNet Transport API is poorly documentated and badly explained in many places.

The following provides a high level summary of how it works:

### High level overview

#### UNet Servers

A UNet transport layer API server is much like a 'traditional' server.

You bind a local port (eg. 8080) on a local network interface (eg. 10.0.0.1) and wait for incoming connections.

Using `NetworkTransport.Receive` you can be notified when:

- Clients connect
- Clients disconnect
- Data is received from a specific client

Notice that `NetworkTransport.Receive` must be called in order to 'pump' events through the network subsystem.

If `Receive` is not called, no events will be processed on the server, and the client will never register an 
active connection.

#### UNet Clients

A UNet client is created using `NetworkTransport.Connect` to open a connection to a remote server.

However, *unlike* typical TCP based connection technology, there are two significant points that must
be highlighted:

- A client cannot be connected if it does not first create a local server.
- Client requests will never be processed until `NetworkTransport.Receive` is called.

This is a significant and fundamental misundestanding about the UNet networking API in most documentation
on the subject.

The UNet transport API is UDP based; as such, there is no implicit creation of a local receiving port
to receive messages *back from the server on*.

As such, the simplest way to understand the client/server relationship would be:

    class NetworkClient : NetworkServer { ... }
    
Which is to say, a network client must do *all of the work that a network server does* and **also**
do the work that a client needs to do.

Typically this is done via:

    NetworkTransport.AddHost(hostTopology, 0);

When the port 0 is used, to indicate that the system should just choose an arbitrary local port to receive
responses on. This is basically what happens implicitly with TCP based networking.

Futhermore, attempting to open a connection will always succeed. 

This is because calling `Connect` basically *doesnt do anything*, until the next call to `NetworkTransport.Receive`,
when a message is fired off to attempt handshake for connection discovery.

### Custom protocol

UNet uses a custom networking protocol. 

A custom UDP server can receive messages, but you'll have to reverse engineer the protocol to see what they
mean and how to deal with them.

### Connection handling

A typical transport layer API example might read something like:

    void Update()
    {
        int recHostId; 
        int connectionId; 
        int channelId; 
        byte[] recBuffer = new byte[1024]; 
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
            case NetworkEventType.Nothing:         //1
                break;
            case NetworkEventType.ConnectEvent:    //2
                break;
            case NetworkEventType.DataEvent:
                ProcessDataEvent(...)
                break;
            case NetworkEventType.DisconnectEvent: //4
                break;
        }
    }
    
Don't do this.

Why? The answer is that you are processing *one network message per frame*.

You should invoke `NetworkTransport.Receive` until it returns `NetworkEventType.Nothing` or some threshold limit
is reached.
 
You'll also see this example:

    connectionId = NetworkTransport.Connect(hostId, "192.16.7.21", 8888, 0, out error);
    NetworkTransport.Disconnect(hostId, connectionId, out error);
    NetworkTransport.Send(hostId, connectionId, myReiliableChannelId, buffer, bufferLength,  out error);

**Don't do this**.

Until a `NetworkEventType.ConnectEvent` has been received you are *not connected* to the server, it may not 
even be running.

### A note about WebSocket servers...

It is worth noting that although the transport API is supported on WebGL, that *does not mean* that you can
use an arbitrary 3rd party websocket server to accept connections.

Rather it means that if you configure your local sockets for the server and client as WebSocket sockets,
your server and client can talk to each other via the websocket protocol.

Specifically, this requires that the unity server is *not running in WebGL mode*.

However, it must still be *configured* to use websockets.

To share both 'websocket' and 'non-websocket' connections on a single unity server, you must bind
multiple ports on the server, one for each.

### Broadcast

UDP support broadcast modes which is useful for service discovery. 

This example does not use broadcast discovery; it's a flakey hardcoded implementation and you're better off
rolling your own.

For more see: https://bitbucket.org/Unity-Technologies/networking/src/78ca8544bbf4e87c310ce2a9a3fc33cdad2f9bb1/Runtime/NetworkDiscovery.cs?at=5.3&fileviewer=file-view-default#NetworkDiscovery.cs-252

### Reference

See https://bitbucket.org/Unity-Technologies/networking

## License

MIT
