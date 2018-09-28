using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dissonance.Integrations.PureP2P.Demo
{
    public class StateManager : MonoBehaviour
    {
        private interface IState
        {
            void Awake();
            [NotNull] IState Update();        
        }

        private class LoadWorld : IState
        {
            private readonly IState _nextState;

            public LoadWorld(IState nextState)
            {
                _nextState = nextState;
            }

            public void Awake()
            {
                SceneManager.LoadScene("Pure P2P Game World");
            }

            public IState Update()
            {
                return _nextState;
            }
        }

        private class UnloadWorld : IState
        {
            private readonly IState _nextState;

            public UnloadWorld(IState nextState)
            {
                _nextState = nextState;
            }

            public void Awake()
            {
                SceneManager.LoadScene("Pure P2P Demo");
            }

            public IState Update()
            {
                return _nextState;
            }
        }

        private class InMenu : IState
        {
            private readonly Camera _camera;

            private string _sessionId = Guid.NewGuid().ToString();

            public InMenu(Camera camera)
            {
                _camera = camera;
            }

            public void Awake()
            {
                _camera.enabled = true;
            }

            public IState Update()
            {
                using (new GUILayout.AreaScope(new Rect(20, 20, 400, 200)))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Session ID:", GUILayout.Width(75));
                        _sessionId = GUILayout.TextField(_sessionId, GUILayout.Width(310));
                    }

                    GUILayout.Space(10);

                    if (GUILayout.Button("Create"))
                    {
                        _camera.enabled = false;
                        return new LoadWorld(new Server(_sessionId, _camera));
                    }

                    if (GUILayout.Button("Connect"))
                    {
                        _camera.enabled = false;
                        return new LoadWorld(new Client(_sessionId, _camera));
                    }
                }

                return this;
            }
        }

        private class Server : IState
        {
            private readonly string _sessionId;

            private PureP2PCommsNetwork _net;
            private DissonanceComms _comms;
            private readonly Camera _camera;

            public Server(string sessionId, Camera camera)
            {
                _sessionId = sessionId;
                _camera = camera;
            }

            public void Awake()
            {
                _net = FindObjectOfType<PureP2PCommsNetwork>();
                _net.InitializeAsServer(_sessionId);

                _comms = FindObjectOfType<DissonanceComms>();
            }

            public IState Update()
            {
                using (new GUILayout.AreaScope(new Rect(20, 20, 400, 600)))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Running server");

                        if (GUILayout.Button("Disconnect"))
                        {
                            _net.Stop();
                            return new UnloadWorld(new InMenu(_camera));
                        }
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        for (var i = 0; i < _comms.Players.Count; i++)
                        {
                            var player = _comms.Players[i];
                            var local = player.Name == _comms.LocalPlayerName;

                            GUILayout.Label(
                                player.Name +
                                (local ? " (Local Player)" : "") +
                                (player.IsSpeaking ? " (Speaking)" : "")
                            );
                        }
                    }

                    return this;
                }
            }
        }

        private class Client : IState
        {
            private readonly string _sessionId;

            private PureP2PCommsNetwork _net;
            private DissonanceComms _comms;
            private readonly Camera _camera;

            public Client(string sessionId, Camera camera)
            {
                _sessionId = sessionId;
                _camera = camera;
            }        

            public void Awake()
            {
                _net = FindObjectOfType<PureP2PCommsNetwork>();
                _net.InitializeAsClient(_sessionId);

                _comms = FindObjectOfType<DissonanceComms>();
            }

            public IState Update()
            {
                using (new GUILayout.AreaScope(new Rect(20, 20, 400, 600)))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(string.Format("Session '{0}'/{1}", _net.SessionId, _net.Status));

                        if (GUILayout.Button("Disconnect"))
                        {
                            _net.Stop();
                            return new UnloadWorld(new InMenu(_camera));
                        }
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        for (var i = 0; i < _comms.Players.Count; i++)
                        {
                            var player = _comms.Players[i];
                            var local = player.Name == _comms.LocalPlayerName;

                            GUILayout.Label(
                                player.Name +
                                (local ? " (Local Player)" : "") +
                                (player.IsSpeaking ? " (Speaking)" : "")
                            );
                        }
                    }

                    return this;
                }
            }
        }

        private IState _state;
        private IState _nextState;

        private static bool _destroy = false;

        [UsedImplicitly] private void Awake()
        {
            if (!_destroy)
            {
                DontDestroyOnLoad(gameObject);
                _destroy = true;
            }
            else
            {
                Destroy (gameObject);
            }

            _state = new InMenu(GetComponent<Camera>());
            _nextState = _state;
        }

        [UsedImplicitly] private void OnGUI()
        {
            _nextState = _state.Update();
        }

        [UsedImplicitly] private void Update()
        {
            if (_state != _nextState)
            {
                _nextState.Awake();
                _state = _nextState;
            }
        }
    }
}
