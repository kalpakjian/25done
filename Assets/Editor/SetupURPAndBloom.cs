#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.IO;

/// <summary>
/// Portal 發光設定工具
/// - Tools → 🔙 Revert to Built-in Pipeline  ← 立刻還原，修復粒子/材質失常
/// - Tools → 🌀 Setup URP + Bloom for Portal  ← 若需要切回 URP
/// </summary>
public static class SetupURPAndBloom
{
    private const string URP_ASSET_PATH     = "Assets/Settings/UniversalRenderPipelineAsset.asset";
    private const string RENDERER_PATH      = "Assets/Settings/UniversalRenderPipelineAsset_Renderer.asset";
    private const string BLOOM_PROFILE_PATH = "Assets/Settings/NightBloomProfile.asset";

    // ════════════════════════════════════════════════════════════
    // 🔙 一鍵還原 Built-in Pipeline（優先顯示，priority 較小）
    // ════════════════════════════════════════════════════════════
    [MenuItem("Tools/🔙 Revert to Built-in Pipeline (Fix Particles)", priority = 1)]
    public static void RevertToBuiltIn()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "還原 Built-in Pipeline",
            "這會把 Graphics Settings 的 Render Pipeline Asset 清空，\n" +
            "還原為 Built-in Pipeline。\n\n" +
            "粒子效果和原本的材質會立即恢復正常。\n\n" +
            "確定還原嗎？",
            "是，還原", "取消");

        if (!confirm) return;

        // 清除 Graphics Settings 的 Pipeline Asset → 回到 Built-in
        GraphicsSettings.renderPipelineAsset = null;

        // 清除所有 Quality Level 的 Pipeline Asset
        int currentLevel = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = null;
        }
        QualitySettings.SetQualityLevel(currentLevel, false);

        AssetDatabase.SaveAssets();

        Debug.Log("[SetupURPAndBloom] ✅ 已還原為 Built-in Pipeline！");
        EditorUtility.DisplayDialog(
            "✅ 已還原 Built-in Pipeline！",
            "粒子效果和材質應已恢復正常。\n\n" +
            "Portal 仍保有：\n" +
            "• 點光源發光效果（PortalLightEffect）\n" +
            "• 加大的 EmissionBoost（Shader 更亮）\n\n" +
            "如需 Bloom 光暈，可安裝 Post Processing Stack v2\n" +
            "（Package Manager → com.unity.postprocessing）",
            "OK");
    }

    // ════════════════════════════════════════════════════════════
    // 🌀 設定 URP（若需要切回 URP）
    // ════════════════════════════════════════════════════════════
    [MenuItem("Tools/🌀 Setup URP + Bloom for Portal (切換至 URP)", priority = 2)]
    public static void SetupURP()
    {
        // 動態載入 URP 類型以避免在 Built-in 模式下報錯
        var urpAssetType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");

        if (urpAssetType == null)
        {
            EditorUtility.DisplayDialog("錯誤", "找不到 URP 套件！請確認已安裝 URP Package。", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");

        // 嘗試載入或建立 URP Asset
        var urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(URP_ASSET_PATH);
        if (urpAsset == null)
        {
            EditorUtility.DisplayDialog("提示",
                "請在 Project 視窗手動建立：\n" +
                "Assets/Settings 資料夾 → 右鍵 →\n" +
                "Create → Rendering → URP Asset (with Universal Renderer)\n\n" +
                "建立後再執行此選單。",
                "OK");
            return;
        }

        GraphicsSettings.renderPipelineAsset = urpAsset;

        int cur = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urpAsset;
        }
        QualitySettings.SetQualityLevel(cur, false);

        AssetDatabase.SaveAssets();
        Debug.Log("[SetupURPAndBloom] URP 已套用。");
        EditorUtility.DisplayDialog("✅ URP 套用成功", "已套用 URP Pipeline Asset。\n記得轉換 Material 並設定 Camera Post Processing。", "OK");
    }
}
#endif
