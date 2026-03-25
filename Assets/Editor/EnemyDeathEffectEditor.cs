using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyDeathEffect))]
public class EnemyDeathEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnemyDeathEffect effect = (EnemyDeathEffect)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Create Baked Mesh"))
        {
            effect.CreateBakedMeshObject();
        }
    }
}
