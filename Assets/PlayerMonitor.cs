using Dissonance;
using TMPro;
using UnityEngine;

public class PlayerMonitor
    : MonoBehaviour
{
    public RectTransform SpeakingIndicator;
    public TMP_Text PlayerId;

    public VoicePlayerState PlayerState { get; set; }

    private void Start()
    {
        if (PlayerState.IsSpeaking)
            StartedSpeaking(PlayerState);
        else
            StoppedSpeaking(PlayerState);

	    PlayerId.text = PlayerState.Name.MeaninglessString();

        PlayerState.OnStartedSpeaking += StartedSpeaking;
        PlayerState.OnStoppedSpeaking += StoppedSpeaking;
        PlayerState.OnLeftSession += LeftSession;
	}

    private void LeftSession(VoicePlayerState obj)
    {
        Destroy(gameObject);
    }

    private void StoppedSpeaking(VoicePlayerState obj)
    {
        SpeakingIndicator.gameObject.SetActive(false);
    }

    private void StartedSpeaking(VoicePlayerState obj)
    {
        SpeakingIndicator.gameObject.SetActive(true);
    }
}
