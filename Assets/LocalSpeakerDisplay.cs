using Dissonance;
using Dissonance.Extensions;
using TMPro;
using UnityEngine;

public class LocalSpeakerDisplay
    : MonoBehaviour
{
    public TMP_Text NameText;
    public RectTransform SpeakingText;

    private DissonanceComms _comms;
    private VoicePlayerState _player;

    private void Start()
    {
        _comms = FindObjectOfType<DissonanceComms>();
        _player = _comms.FindPlayer(_comms.LocalPlayerName);

        // ReSharper disable once PossibleNullReferenceException
        NameText.text = _player.Name.MeaninglessString();
    }

    private void Update ()
    {
        SpeakingText.gameObject.SetActive(_player.IsSpeaking);
    }
}

public static class StringExtensions
{
    [NotNull]
    public static string MeaninglessString(this string str)
    {
        return unchecked((uint)str.GetFnvHashCode()).MeaninglessString();
    }
}