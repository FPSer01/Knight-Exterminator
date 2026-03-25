using UnityEngine;
using UnityEngine.Rendering;

namespace PAI_Demo
{
    [ExecuteInEditMode]
    public class CrossPipelineShaderSwitcher : MonoBehaviour
    {
        void Start()
        {
            string shaderName = GetShaderNameForPipeline();
            Shader targetShader = Shader.Find(shaderName);

            if (targetShader == null)
            {
                Debug.LogWarning("Shader not found: " + shaderName);
                return;
            }

            foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null && mat.shader.name != shaderName)
                        mat.shader = targetShader;
                }
            }
        }

        string GetShaderNameForPipeline()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null)
                return "Standard";
            if (pipeline.GetType().Name.Contains("Universal"))
                return "Universal Render Pipeline/Lit";
            if (pipeline.GetType().Name.Contains("HD"))
                return "HDRP/Lit";
            return "Standard";
        }
    }
}
