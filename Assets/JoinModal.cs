using System.Collections;
using Dissonance.Integrations.PureP2P;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class JoinModal
    : MonoBehaviour
{
    public Text SessionIdTextInput;
    public Button JoinSessionButton;
    public Button CancelButton;
    public PlayableDirector JoiningProgressIndicator;
    public RectTransform MenuPanel;
    public RectTransform InSessionPanel;
    public RectTransform FailedModal;

    private bool _enableJoinButton = true;

    public void Update()
    {
        JoinSessionButton.interactable = _enableJoinButton && Validate(SessionIdTextInput.text);
    }

    private static bool Validate([CanBeNull] string text)
    {
        return !string.IsNullOrEmpty(text) && text.Contains("-");
    }

    public void ShowModal()
    {
        gameObject.SetActive(true);

        //Reset state
        JoiningProgressIndicator.Stop();
        _enableJoinButton = true;
        CancelButton.interactable = true;

        GetComponent<Animator>().Play("Fade-in");
    }

    public void HideModal()
    {
        GetComponent<Animator>().Play("Fade-out");
    }

    public void JoinSession()
    {
        StartCoroutine(CoJoinSession(SessionIdTextInput.text));
    }

    private IEnumerator CoJoinSession(string sessionId)
    {
        //Show an animation to indicate that joining is in progress
        JoiningProgressIndicator.Play();

        //Disable all the button
        _enableJoinButton = false;
        CancelButton.interactable = false;

        //connect
        var comms = FindObjectOfType<PureP2PCommsNetwork>();
        comms.InitializeAsClient(SessionIdTextInput.text);

        //Wait until session is ready
        yield return new WaitForSeconds(0.5f);
        var attempts = 0;
        do
        {
            attempts++;
            yield return new WaitForSeconds(0.5f);
        } while (comms.Status != Dissonance.Networking.ConnectionStatus.Connected && attempts < 17);

        if (comms.Status == Dissonance.Networking.ConnectionStatus.Connected)
        {
            HideModal();
            MenuPanel.gameObject.SetActive(false);
            InSessionPanel.gameObject.SetActive(true);
        }
        else
        {
            FailedModal.gameObject.SetActive(true);
        }
    }
}
