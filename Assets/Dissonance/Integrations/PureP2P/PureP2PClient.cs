using System;
using System.Collections.Generic;
using System.Linq;
using Byn.Net;
using Dissonance.Networking;
using Dissonance.Networking.Client;

namespace Dissonance.Integrations.PureP2P
{
    public class PureP2PClient
        : BaseClient<PureP2PServer, PureP2PClient, PureP2PPeer>
    {
        [NotNull] private readonly PureP2PCommsNetwork _network;
        [NotNull] private readonly string _sessionId;

        private IBasicNetwork _webrtcNetwork;
        private ConnectionId _serverConnectionId;
        private byte[] _peerHandshake;

        public PureP2PClient([NotNull] PureP2PCommsNetwork network, [NotNull] string sessionId)
            : base(network)
        {
            if (network == null)
                throw new ArgumentNullException("network");
            if (network == null)
                throw new ArgumentNullException("sessionId");

            _network = network;
            _sessionId = sessionId;
        }

        public override void Connect()
        {
            _webrtcNetwork = WebRtcNetworkFactory.Instance.CreateDefault(
                _network.SignallingServer,
                _network.IceServers.Select(a => a.BynIce).ToArray()
            );

            // Connect to the master peer
            if (_network.IsServer)
            {
                Log.Debug("Connecting to WebRTC Session: {0} (loopback)", _sessionId);
                Connected();
            }
            else
            {
                Log.Debug("Connecting to WebRTC Session: {0}", _sessionId);
                _serverConnectionId = _webrtcNetwork.Connect(_sessionId);
            }

            // Start a server to listen for direct p2p connections using `SessionID:PlayerName` as the session name
            var mySession = UniquePlayerSessionId(_network.PlayerName);
            _webrtcNetwork.StartServer(mySession);
            Log.Debug("Hosting WebRTC Session: {0}", mySession);

            PlayerJoined += OnPlayerJoined;
        }

        [NotNull] private string UniquePlayerSessionId(string playerName)
        {
            return string.Format("{0}:{1}", _sessionId, playerName);
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

        #region send
        private void Send(ConnectionId connection, ArraySegment<byte> packet, bool reliable)
        {
            _webrtcNetwork.SendData(connection, packet.Array, packet.Offset, packet.Count, reliable);
        }

        private void SendP2P([NotNull] IList<ClientInfo<PureP2PPeer?>> destinations, ArraySegment<byte> packet, bool reliable)
        {
            for (var i = destinations.Count - 1; i >= 0; i--)
            {
                var dest = destinations[i];

                //Skip destinations which we cannot contact directly
                if (!dest.Connection.HasValue)
                    continue;

                //Send the packet directly
                Send(dest.Connection.Value.ConnectionId, packet, reliable);

                //Packet has been sent successfully - remove from list of destinations so that it is not server relayed
                destinations.RemoveAt(i);
            }
        }

        protected override void SendReliable(ArraySegment<byte> packet)
        {
            if (!_network.TryLoopbackToServer(packet))
                Send(_serverConnectionId, packet, true);
        }

        protected override void SendReliableP2P(List<ClientInfo<PureP2PPeer?>> destinations, ArraySegment<byte> packet)
        {
            SendP2P(destinations, packet, true);

            base.SendReliableP2P(destinations, packet);
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            if (!_network.TryLoopbackToServer(packet))
                Send(_serverConnectionId, packet, false);
        }

        protected override void SendUnreliableP2P(List<ClientInfo<PureP2PPeer?>> destinations, ArraySegment<byte> packet)
        {
            SendP2P(destinations, packet, false);

            base.SendReliableP2P(destinations, packet);
        }
        #endregion

        public override ClientStatus Update()
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
                        var id = NetworkReceivedPacket(new ArraySegment<byte>(evt.MessageData.Buffer, evt.MessageData.Offset, evt.MessageData.ContentLength));
                        if (id.HasValue)
                            ReceiveHandshakeP2P(id.Value, new PureP2PPeer(evt.ConnectionId));
                        break;

                    case NetEventType.NewConnection:
                        if (evt.ConnectionId == _serverConnectionId)
                            Connected();
                        else
                            SendHandshakeP2P(evt.ConnectionId);
                        break;

                    case NetEventType.ServerClosed:
                        Disconnect();
                        break;

                    case NetEventType.Disconnected:
                        if (evt.ConnectionId == _serverConnectionId)
                            Disconnect();
                        break;

                    case NetEventType.ConnectionFailed:
                        if (evt.ConnectionId == _serverConnectionId)
                            FatalError("Failed to connect to server");
                        else
                            Log.Warn("Failed connection: {0}", evt.ConnectionId);
                        break;

                    case NetEventType.ServerInitFailed:
                    case NetEventType.FatalError:
                        FatalError(string.Format("WebRTC Fatal event: `{0}`", evt.Type));
                        break;

                    case NetEventType.ServerInitialized:
                    case NetEventType.Invalid:
                    case NetEventType.Warning:
                    case NetEventType.Log:
                        Log.Debug("Ignoring WebRTC `{0}` message", evt.Type);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void OnPlayerJoined(string name, CodecSettings _)
        {
            if (name == _network.PlayerName)
                return;

            // Try to connect directly to this peer if their ID comes after yours
            // This ensures that connections are only established in one direction between any two given peers.
            if (_peerHandshake != null && string.Compare(name, _network.PlayerName, StringComparison.Ordinal) > 0)
            {
                var s = UniquePlayerSessionId(name);
                Log.Debug("Trying to establish p2p connection to `{0}`", s);
                _webrtcNetwork.Connect(s);
            }
        }

        private void SendHandshakeP2P(ConnectionId connectionId)
        {
            if (_peerHandshake != null)
            {
                Log.Debug("Sending P2P handshake to `{0}`", connectionId.id);
                Send(connectionId, new ArraySegment<byte>(_peerHandshake), true);
            }
        }

        protected override void OnServerAssignedSessionId(uint session, ushort id)
        {
            base.OnServerAssignedSessionId(session, id);

            // Save a p2p handshake so that we have something to send to peers later when we want to introduce ourselves
            _peerHandshake = WriteHandshakeP2P(session, id);

            Log.Debug("Prepared Handshake P2P");
        }
    }
}
