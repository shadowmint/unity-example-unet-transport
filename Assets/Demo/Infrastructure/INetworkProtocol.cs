namespace Demo.Infrastructure
{
  public interface INetworkProtocol
  {
    void OnConnected();
    void OnDisconnected();
    void Update();
    void OnDataReceived(byte[] buffer, int dataSize);
    NetworkChannel Channel { get; set; }
  }
}