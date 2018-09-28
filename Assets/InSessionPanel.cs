using Dissonance;
using Dissonance.Integrations.PureP2P;
using TMPro;
using UnityEngine;

public class InSessionPanel
    : MonoBehaviour
{
    private DissonanceComms _comms;

    public PlayerMonitor PlayerMonitorPrefab;
    public RectTransform PlayerContainer;

    public TMP_Text[] SessionIdLabels;

    private void Start()
	{
	    _comms = FindObjectOfType<DissonanceComms>();

	    //Update labels with session ID
	    var sessionId = FindObjectOfType<PureP2PCommsNetwork>().SessionId;
	    if (SessionIdLabels != null)
	        for (var i = 0; i < SessionIdLabels.Length; i++)
	            SessionIdLabels[i].text = sessionId;

        //Add all existing players
	    foreach (var player in _comms.Players)
            if (player.Name != _comms.LocalPlayerName)
	            PlayerJoined(player);

        _comms.OnPlayerJoinedSession += PlayerJoined;
	}

    private void PlayerJoined(VoicePlayerState player)
    {
        var monitor = Instantiate(PlayerMonitorPrefab, PlayerContainer);
        monitor.PlayerState = player;
    }

    public void VolumeSliderChanged(float value)
    {
        _comms.RemoteVoiceVolume = Mathf.Clamp01(value);
    }
}
