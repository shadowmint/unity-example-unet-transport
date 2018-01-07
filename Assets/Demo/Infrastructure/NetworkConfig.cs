using System.Collections.Generic;
using UnityEngine.Networking;

namespace Demo.Infrastructure
{
  /// <summary>
  /// This configuration must be shared between client and server, or nothing works.
  /// </summary>
  public static class NetworkConfig
  {
    private static ConnectionConfig _config;
    private static IDictionary<QosType, int> _channels;

    public static ConnectionConfig Config
    {
      get { return _config ?? (_config = LoadApplicationNetworkConfiguration()); }
    }

    public static IDictionary<QosType, int> Channels
    {
      get
      {
        if (_channels == null)
        {
          LoadApplicationNetworkConfiguration();
        }

        return _channels;
      }
    }

    private static ConnectionConfig LoadApplicationNetworkConfiguration()
    {
      var config = new ConnectionConfig();

      // Add a few network channels, which are common to all instances of the client and server.
      // Notice that it is meaningless to add multiple instances of the same channel type, as the
      // server will not be able to distinguish between them.
      _channels = new Dictionary<QosType, int>();
      _channels[QosType.Reliable] = config.AddChannel(QosType.Reliable);
      _channels[QosType.Unreliable] = config.AddChannel(QosType.Unreliable);

      return config;
    }
  }
}