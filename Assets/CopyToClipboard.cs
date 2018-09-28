using TMPro;
using UnityEngine;

public class CopyToClipboard
    : MonoBehaviour
{
    public TMP_Text TextSource;

    public void CopyText()
    {
        GUIUtility.systemCopyBuffer = TextSource.text;
    }
}
