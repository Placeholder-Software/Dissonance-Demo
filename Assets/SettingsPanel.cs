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

    public InputField _signallingInput;
    public InputField _ice1Input;
    public InputField _ice2Input;
    public InputField _ice3Input;

    private bool _shown;
    private PureP2PCommsNetwork _comms;

    public void Start()
    {
        //Set up the input field initial values
        _comms = FindObjectOfType<PureP2PCommsNetwork>();
        _signallingInput.text = _comms.SignallingServer;

        _signallingInput.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        _ice1Input.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        _ice2Input.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        _ice3Input.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;

        InitializeIceInputs();

        //React to the input field edits
        _signallingInput.onEndEdit.AddListener(a => _comms.SignallingServer = a);
        _ice1Input.onEndEdit.AddListener(a => ReplaceIce(0, a));
        _ice2Input.onEndEdit.AddListener(a => ReplaceIce(1, a));
        _ice3Input.onEndEdit.AddListener(a => ReplaceIce(2, a));
    }

    private void InitializeIceInputs()
    {
        var ice = _comms.IceServers.ToArray();
        if (ice.Length > 0) _ice1Input.text = ice[0].URL;
        if (ice.Length > 1) _ice2Input.text = ice[1].URL;
        if (ice.Length > 2) _ice3Input.text = ice[2].URL;
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
