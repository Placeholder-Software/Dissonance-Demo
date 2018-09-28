using System;
using System.Collections.Generic;
using System.Text;
using Byn.Common;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using Dissonance.Networking;
using UnityEngine;

namespace Dissonance.Integrations.PureP2P
{
    public class PureP2PCommsNetwork
        : BaseCommsNetwork<PureP2PServer, PureP2PClient, PureP2PPeer, string, string>
    {
        public string SessionId { get; private set; }

        public IEnumerable<IceServer> IceServers
        {
            get { return _iceServersList; }
        }

        [SerializeField] public string SignallingServer = "wss://because-why-not.com:12777/chatapp";
        [SerializeField] private List<IceServer> _iceServersList = new List<IceServer> {
            new IceServer("stun:because-why-not.com:12779"),
            new IceServer("stun:stun.l.google.com:19302")
        };

        protected override PureP2PServer CreateServer(string sessionId)
        {
            if (sessionId == null)
                throw new ArgumentNullException("sessionId");

            return new PureP2PServer(this, sessionId);
        }

        protected override PureP2PClient CreateClient(string sessionId)
        {
            if (sessionId == null)
                throw new ArgumentNullException("sessionId");

            return new PureP2PClient(this, sessionId);
        }

        protected override void Initialize()
        {
            ModeChanged += OnModeChanged;

            SLog.SetLogger(OnLog);

            base.Initialize();
        }

        private void OnLog(object msg, [NotNull] string[] tags)
        {
            if (!Log.IsTrace)
                return;

            var builder = new StringBuilder();

            //Start with message category
            for (var i = 0; i< tags.Length; i++)
            {
                if(i != 0)
                    builder.Append(",");
                builder.Append(tags[i]);
            }

            //Show time
            builder.Append(" (");
            builder.AppendFormat("{0:HH:mm:ss.fff}", DateTime.UtcNow);
            builder.Append(") ");

            //General webrtc name
            builder.Append("[WebRTC] ");

            //The message
            builder.Append(msg);

            //Not using standard Dissonance logger which introduces it's own message metatdata (category/time etc)
            Debug.Log(builder.ToString());
        }

        private void OnModeChanged(NetworkMode mode)
        {
            if (mode == NetworkMode.None)
                SessionId = null;
        }

        public void InitializeAsServer([NotNull] string sessionId)
        {
            if (sessionId == null)
                throw new ArgumentNullException("sessionId");
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("session ID cannot be null or empty", "sessionId");

            _loopbackToClient.Clear();
            _loopbackToServer.Clear();

            SessionId = sessionId;
            RunAsHost(sessionId, sessionId);
        }

        public void InitializeAsClient([NotNull] string sessionId)
        {
            if (sessionId == null)
                throw new ArgumentNullException("sessionId");
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("session ID cannot be null or empty", "sessionId");

            _loopbackToClient.Clear();
            _loopbackToServer.Clear();

            SessionId = sessionId;
            RunAsClient(sessionId);
        }

        private readonly ConcurrentPool<byte[]> _loopbackBuffers = new ConcurrentPool<byte[]>(8, () => new byte[1024]);
        private readonly List<KeyValuePair<PureP2PPeer, ArraySegment<byte>>> _loopbackToServer = new List<KeyValuePair<PureP2PPeer, ArraySegment<byte>>>();
        private readonly List<ArraySegment<byte>> _loopbackToClient = new List<ArraySegment<byte>>();

        protected override void Update()
        {
            base.Update();

            //Loop back packets from client to server
            var s = Server;
            if (s != null)
            {
                for (var i = 0; i < _loopbackToServer.Count; i++)
                {
                    var item = _loopbackToServer[i];
                    if (item.Value.Array != null)
                    {
                        s.NetworkReceivedPacket(item.Key, item.Value);
                        _loopbackBuffers.Put(item.Value.Array);
                    }
                }
                _loopbackToServer.Clear();
            }

            //Loop back packets from server to client
            var c = Client;
            if (c != null)
            {
                for (var i = 0; i < _loopbackToClient.Count; i++)
                {
                    var item = _loopbackToClient[i];
                    if (item.Array != null)
                    {
                        c.NetworkReceivedPacket(item);
                        _loopbackBuffers.Put(item.Array);
                    }
                }
                _loopbackToClient.Clear();
            }
        }

        internal bool IsServer
        {
            get { return Server != null; }
        }

        internal bool TryLoopbackToServer(ArraySegment<byte> packet)
        {
            if (IsServer)
            {
                var p = new PureP2PPeer(true);
                _loopbackToServer.Add(new KeyValuePair<PureP2PPeer, ArraySegment<byte>>(p, packet.CopyTo(_loopbackBuffers.Get())));
                return true;
            }
            return false;
        }

        internal void LoopbackToClient(ArraySegment<byte> packet)
        {
            if (Client != null)
                _loopbackToClient.Add(packet.CopyTo(_loopbackBuffers.Get()));
        }

        public void AddIceServer(IceServer server)
        {
            _iceServersList.Add(server);
        }

        public void RemoveIceServer(IceServer server)
        {
            _iceServersList.Remove(server);
        }

        [Serializable]
        public class IceServer
        {
            [UsedImplicitly, SerializeField] private string _url;
            [UsedImplicitly, SerializeField] private string _username;
            [UsedImplicitly, SerializeField] private string _password;

            public string URL
            {
                get { return _url; }
            }

            public string Username
            {
                get { return _username; }
            }

            public string Password
            {
                get { return _password; }
            }

            private Byn.Net.IceServer _byn;
            [NotNull] internal Byn.Net.IceServer BynIce
            {
                get
                {
                    if (_byn == null)
                        _byn = new Byn.Net.IceServer(URL, Username, Password);
                    return _byn;
                }
            }

            public IceServer(string url, string username = "", string password = "")
            {
                _url = url;
                _username = username;
                _password = password;
            }
        }
    }
}
