using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PulseFactor.EditorTools
{
    // Configuracao unica do projeto. Pode rodar em batchmode:
    // Unity.exe -batchmode -quit -projectPath ... -executeMethod PulseFactor.EditorTools.ProjectSetup.ConfigureUrp
    public static class ProjectSetup
    {
        const string UrpFolder = "Assets/_Project/Settings/URP";
        const string PipelinePath = UrpFolder + "/PulseFactor_URP.asset";
        const string RendererPath = UrpFolder + "/PulseFactor_Renderer.asset";

        [MenuItem("PulseFactor/Setup/Configure URP")]
        public static void ConfigureUrp()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);

                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            pipeline.supportsHDR = true;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);

            AssetDatabase.SaveAssets();
            Debug.Log("[PulseFactor] URP configurado: " + PipelinePath);
        }
    }
}
