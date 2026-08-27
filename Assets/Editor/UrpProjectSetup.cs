#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MonsterMiner.Editor
{
    public static class UrpProjectSetup
    {
        const string SettingsDir = "Assets/Settings";
        const string RendererPath = SettingsDir + "/MonsterMiner_ForwardRenderer.asset";
        const string UrpAssetPath = SettingsDir + "/MonsterMiner_URP.asset";

        [InitializeOnLoadMethod]
        static void ScheduleAutoSetup()
        {
            EditorApplication.delayCall += () => EnsureConfigured();
        }

        [MenuItem("Monster Miner/Configure URP")]
        public static void ConfigureFromMenu()
        {
            EnsureConfigured(force: true);
        }

        public static void SetupFromBatch()
        {
            EnsureConfigured(force: true);
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        static void EnsureConfigured(bool force = false)
        {
            if (!force && GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset existing &&
                AssetDatabase.GetAssetPath(existing) == UrpAssetPath)
                return;

            if (!Directory.Exists(SettingsDir))
                Directory.CreateDirectory(SettingsDir);

            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (urpAsset == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);

                urpAsset = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(urpAsset, UrpAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;

            for (int i = 0; i < QualitySettings.count; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = urpAsset;
            }

            QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel(), applyExpensiveChanges: true);

            EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
