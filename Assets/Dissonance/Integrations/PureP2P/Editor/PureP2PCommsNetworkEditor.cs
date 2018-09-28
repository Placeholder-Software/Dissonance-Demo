using Dissonance.Editor;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Integrations.PureP2P.Editor
{
    [CustomEditor(typeof(PureP2PCommsNetwork))]
    public class PureP2PCommsNetworkEditor
        : BaseDissonnanceCommsNetworkEditor<PureP2PCommsNetwork, PureP2PServer, PureP2PClient, PureP2PPeer, string, string>
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!Application.isPlaying)
            {
                var signal = serializedObject.FindProperty("SignallingServer");
                EditorGUILayout.PropertyField(signal);

                var ices = serializedObject.FindProperty("_iceServersList");
                EditorGUILayout.PropertyField(ices, true);
            }

            Undo.FlushUndoRecordObjects();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
