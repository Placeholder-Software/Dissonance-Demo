using System;
using System.Linq;
using Byn.Net;
using Dissonance.Networking;
using Dissonance.Networking.Server;

namespace Dissonance.Integrations.PureP2P
{
    public class PureP2PServer
        : BaseServer<PureP2PServer, PureP2PClient, PureP2PPeer>
    {
        private readonly string _sessionId;
        private readonly PureP2PCommsNetwork _network;

        private IBasicNetwork _webrtcNetwork;

        public PureP2PServer(PureP2PCommsNetwork network, string sessionId)
        {
            _network = network;
            _sessionId = sessionId;
        }

        public override void Connect()
        {
            _webrtcNetwork = WebRtcNetworkFactory.Instance.CreateDefault(
                _network.SignallingServer,
                _network.IceServers.Select(a => a.BynIce).ToArray()
            );
            _webrtcNetwork.StartServer(_sessionId);

            Log.Debug("Hosting WebRTC Session: {0}", _sessionId);

            base.Connect();
        }

        public override void Disconnect()
        {
            if (_webrtcNetwork != null)
            {
                _webrtcNetwork.Dispose();
                _webrtcNetwork = null;
            }

            base.Disconnect();
        }

        protected override void SendReliable(PureP2PPeer connection, ArraySegment<byte> packet)
        {
            if (connection.IsLocalLoopback)
                _network.LoopbackToClient(packet);
            else
                _webrtcNetwork.SendData(connection.ConnectionId, packet.Array, packet.Offset, packet.Count, true);
        }

        protected override void SendUnreliable(PureP2PPeer connection, ArraySegment<byte> packet)
        {
            if (connection.IsLocalLoopback)
                _network.LoopbackToClient(packet);
            else
                _webrtcNetwork.SendData(connection.ConnectionId, packet.Array, packet.Offset, packet.Count, false);
        }

        public override ServerState Update()
        {
            if (_webrtcNetwork != null)
                _webrtcNetwork.Flush();

            return base.Update();
        }

        protected override void ReadMessages()
        {
            if (_webrtcNetwork == null)
                return;
            _webrtcNetwork.Update();

            NetworkEvent evt;
            while (_webrtcNetwork != null && _webrtcNetwork.Dequeue(out evt))
            {
                switch (evt.Type)
                {
                    case NetEventType.UnreliableMessageReceived:
                    case NetEventType.ReliableMessageReceived:
                        var buffer = evt.MessageData;
                        NetworkReceivedPacket(new PureP2PPeer(evt.ConnectionId), new ArraySegment<byte>(buffer.Buffer, buffer.Offset, buffer.ContentLength));
                        break;

                    case NetEventType.FatalError:
                    case NetEventType.ServerInitFailed:
                        FatalError(string.Format("WebRTC `{0}`", evt.Type));
                        break;

                    case NetEventType.ConnectionFailed:
                    case NetEventType.Disconnected:
                        ClientDisconnected(new PureP2PPeer(evt.ConnectionId));
                        break;

                    case NetEventType.NewConnection:
                    case NetEventType.Invalid:
                    case NetEventType.ServerInitialized:
                    case NetEventType.Warning:
                    case NetEventType.Log:
                        Log.Debug("Ignoring `{0}` message type", evt.Type);
                        break;

                    case NetEventType.ServerClosed:
                        throw Log.CreatePossibleBugException(string.Format("Server received `{0}` event", evt.Type), "9576541B-13E4-432F-A8BF-5FD730AAD2D9");

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
