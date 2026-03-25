using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UpgradeItemDatabase))]
public class UpgradeItemDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        UpgradeItemDatabase database = (UpgradeItemDatabase)target;

        GUILayout.Label("Setup");
        GUILayout.Space(5f);
        if (GUILayout.Button("Setup Database"))
        {
            SerializedObject sO = new SerializedObject(database);

            var type = database.GetType();

            MethodInfo method = type.GetMethod("SetupDatabase",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (method != null)
            {
                method.Invoke(database, null);
            }
            else
            {
                Debug.LogError("Метод не найден!");
            }

        }

        GUILayout.Space(20f);

        DrawDefaultInspector();
    }
}
