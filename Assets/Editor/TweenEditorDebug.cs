using DG.DOTweenEditor;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

public class TweenEditorDebug : EditorWindow
{
    private Transform targetTransform;

    [MenuItem("Tools/Tween Editor Preview")]
    public static void ShowWindow()
    {
        GetWindow<TweenEditorDebug>("DOTween Editor Preview");
    }

    private void OnGUI()
    {
        targetTransform = (Transform)EditorGUILayout.ObjectField("Target Transform", targetTransform, typeof(Transform), true);

        if (targetTransform == null)
        {
            EditorGUILayout.HelpBox("Assign a Target Transform to preview tweens.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Start Preview"))
        {
            // Stop any existing preview before starting a new one
            DOTweenEditorPreview.Stop();

            // Prepare for editor preview
            //DOTweenEditorPreview.PrepareTweenForPreview(myTween);

            // Start the preview
            DOTweenEditorPreview.Start();
        }

        if (GUILayout.Button("Stop Preview"))
        {
            DOTweenEditorPreview.Stop();
        }
    }

    private void OnDisable()
    {
        // Ensure the preview is stopped when the editor window is closed
        DOTweenEditorPreview.Stop();
    }
}
