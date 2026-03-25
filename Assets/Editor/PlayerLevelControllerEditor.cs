using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerLevelController))]
public class PlayerLevelControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerLevelController levelCoontroller = (PlayerLevelController)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Show Level Requirements"))
        {
            levelCoontroller.LogLevelRequirements();
        }
    }
}
