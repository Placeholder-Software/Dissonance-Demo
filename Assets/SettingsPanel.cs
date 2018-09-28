using System.Linq;
using Dissonance.Integrations.PureP2P;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class SettingsPanel
    : MonoBehaviour
{
    public TimelineAsset AnimateOn;
    public TimelineAsset AnimateOff;

    public InputField SignallingInput;
    public InputField Ice1Input;
    public InputField Ice2Input;
    public InputField Ice3Input;

    private bool _shown;
    private PureP2PCommsNetwork _comms;

    public void Start()
    {
        //Set up the input field initial values
        _comms = FindObjectOfType<PureP2PCommsNetwork>();
        SignallingInput.text = _comms.SignallingServer;

        SignallingInput.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        Ice1Input.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        Ice2Input.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        Ice3Input.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;

        InitializeIceInputs();

        //React to the input field edits
        SignallingInput.onEndEdit.AddListener(a => _comms.SignallingServer = a);
        Ice1Input.onEndEdit.AddListener(a => ReplaceIce(0, a));
        Ice2Input.onEndEdit.AddListener(a => ReplaceIce(1, a));
        Ice3Input.onEndEdit.AddListener(a => ReplaceIce(2, a));
    }

    private void InitializeIceInputs()
    {
        var ice = _comms.IceServers.ToArray();
        if (ice.Length > 0) Ice1Input.text = ice[0].URL;
        if (ice.Length > 1) Ice2Input.text = ice[1].URL;
        if (ice.Length > 2) Ice3Input.text = ice[2].URL;
    }

    private void ReplaceIce(int index, string url)
    {
        //Get the existing ice servers
        var ice = _comms.IceServers.ToList();

        //Remove them all
        foreach (var iceServer in ice)
            _comms.RemoveIceServer(iceServer);

        //Add the new one
        var newIce = new PureP2PCommsNetwork.IceServer(url);
        if (ice.Count <= index)
            ice.Add(newIce);
        else
            ice[index] = newIce;

        //Add them all
        foreach (var iceServer in ice)
            _comms.AddIceServer(iceServer);

        //Update all the text inputs
        InitializeIceInputs();
    }

    public void Toggle()
    {
        if (_shown)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);

        _shown = true;
        var playable = GetComponent<PlayableDirector>();
        playable.Play(AnimateOn, DirectorWrapMode.Hold);
    }

    public void Hide()
    {
        _shown = false;
        var playable = GetComponent<PlayableDirector>();
        playable.Play(AnimateOff, DirectorWrapMode.Hold);
    }
}
