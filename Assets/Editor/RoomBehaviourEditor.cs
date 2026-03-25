using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomBehaviour))]
public class RoomBehaviourEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomBehaviour roomBehaviour = (RoomBehaviour)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Switch Walls"))
        {
            roomBehaviour.SwitchWalls();
        }
    }
}
