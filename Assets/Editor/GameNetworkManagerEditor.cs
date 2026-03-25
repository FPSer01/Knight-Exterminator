using Unity.Netcode.Editor;
using UnityEditor;

[CustomEditor(typeof(GameNetworkManager))]
[CanEditMultipleObjects]
public class GameNetworkManagerEditor : NetworkManagerEditor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Custom Settings", EditorStyles.boldLabel);
        DrawDefaultInspector();
        EditorGUILayout.Space();

        base.OnInspectorGUI();
    }
}
