namespace Demo.Infrastructure
{
  public interface INetworkEventHandler
  {
    void OnConnectEvent(int connectionId, int channelId);
    void OnDataEvent(int connectionId, int channelId, byte[] buffer, int dataSize);
    void OnDisconnectEvent(int connectionId, int channelId);
  }
}