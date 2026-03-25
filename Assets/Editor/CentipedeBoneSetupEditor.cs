using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CentipedeBoneSetup))]
public class CentipedeBoneSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CentipedeBoneSetup bonesController = (CentipedeBoneSetup)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Setup Bones"))
        {
            bonesController.SetupBones();
        }
    }
}
