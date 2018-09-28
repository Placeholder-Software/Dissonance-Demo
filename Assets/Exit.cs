using UnityEngine;

public class Exit
    : MonoBehaviour
{
    public void DoExit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
