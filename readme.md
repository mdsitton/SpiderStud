SpiderStud
===

SpiderStud is a WebSocket server implementation in C#.

This project started out as a fork of [Facepunch/Fleck](https://github.com/facepunch/Fleck) an optimized fork of fleck. Fleck itself started as a fork of the now defunct Nugget project.

However this project has since significantly diverged from the original Fleck project and because of that has been renamed to SpiderStud. The name SpiderStud is a play on the word WebSocket.

The goal of this project is to have an extremely low/no allocation websocket server for use in games or other allocation sensitive environments. It also uses more modern .net features and is targeting .net standard 2.1, the async server implementation uses async await instead of older style ContinueWith() style code.

Unlike Fleck, SpiderStud uses a class implementing the IWebSocketClientHandler interface for callbacks instead of delegates. This is so you can easily implement multiple different message handlers for different api endpoints. Though and api has not yet been implemented for this.
This model is much closer to the original Nugget library than Fleck is.

Another goal is full websocket standards complience so along with that this fork removes all of the draft standards implementations, as the current standard has been in place since 2011 and is unlikely to see major changes as http2 and http3 fill many similar roles as websockets do these days.

I am planning on validating the implementation against the [autobahn test suite](https://github.com/crossbario/autobahn-testsuite) once complete

Example
---

The following is an example that will echo to a client.

```c#
    class ClientHandler : IWebSocketClientHandler
    {
        private IWebSocketConnection clientConnection;
        private WebSocketServer server;

        public void OnConfig(WebSocketServer wsServer, IWebSocketConnection connection)
        {
            clientConnection = connection;
            server = wsServer;
        }

        public void OnError(Exception e) => Console.WriteLine($"Error! {e}");

        public void OnMessage(FrameType type, bool endOfMessage, ReadOnlySpan<byte> data)
        {
            if (type == FrameType.Text)
            {
                string messageText = Encoding.UTF8.GetString(data);
                Console.WriteLine(messageText);
                clientConnection.SendMesage(FrameType.Text, data, endOfMessage: true);
            }
        }

        public void OnOpen() => Console.WriteLine("Open!");
        public void OnClose() => Console.WriteLine("Close!");

        public static IWebSocketClientHandler Create()
        {
            return new ClientHandler();
        }
    }

    static class Program
    {
        public static void Main()
        {
            var server = new WebSocketServer("ws://0.0.0.0:8181");
            server.Start(ClientHandler.Create);
        }
    }      
```

Supported WebSocket Versions
---

This fork of Fleck supports only standard rfc6455 WebSockets for modern web browsers and tools, and does not support any older draft specifications.

Secure WebSockets (wss://)
---

Enabling secure connections requires two things: using the scheme `wss` instead
of `ws`, and pointing the server to an x509 certificate containing a public and
private key

```cs
var server = new WebSocketServer("wss://0.0.0.0:8431");
server.Certificate = new X509Certificate2("MyCert.pfx");
server.Start(ClientHandler.Create);
```

Having issues making a certificate? See this
[guide to creating an x509](https://github.com/statianzo/Fleck/issues/214#issuecomment-364413879)
by [@AdrianBathurst](https://github.com/AdrianBathurst)

SubProtocol Negotiation
---

Currently SpiderStud does not support SubProtocols support will be added soon

below is the old fleck info for this WIP will be updated soon:

To enable negotiation of subprotocols, specify the supported protocols on
the `WebSocketServer.SupportedSubProtocols` property. The negotiated
subprotocol will be available on the socket's `ConnectionInfo.NegotiatedSubProtocol`.

If no supported subprotocols are found on the client request (the
Sec-WebSocket-Protocol header), the connection will be closed.

```cs
var server = new WebSocketServer("ws://0.0.0.0:8181");
server.SupportedSubProtocols = new []{ "superchat", "chat" };
server.Start(socket =>
{
  //socket.ConnectionInfo.NegotiatedSubProtocol is populated
});
```

Custom Logging
---

Fleck can log into Log4Net or any other third party logging system. Just override the `FleckLog.LogAction` property with the desired behavior.

```cs
ILog logger = LogManager.GetLogger(typeof(FleckLog));

FleckLog.LogAction = (level, message, ex) => {
  switch(level) {
    case LogLevel.Debug:
      logger.Debug(message, ex);
      break;
    case LogLevel.Error:
      logger.Error(message, ex);
      break;
    case LogLevel.Warn:
      logger.Warn(message, ex);
      break;
    default:
      logger.Info(message, ex);
      break;
  }
};

```

Disable Nagle's Algorithm
---

Set `NoDelay` to `true` on the `WebSocketConnection.ListenerSocket`

```cs
var server = new WebSocketServer("ws://0.0.0.0:8181");
server.ListenerSocket.NoDelay = true;
server.Start(socket =>
{
  //Child connections will not use Nagle's Algorithm
});
```

Auto Restart After Listen Error
---

Set `RestartAfterListenError` to `true` on the `WebSocketConnection`

```cs
var server = new WebSocketServer("ws://0.0.0.0:8181");
server.RestartAfterListenError = true;
```
