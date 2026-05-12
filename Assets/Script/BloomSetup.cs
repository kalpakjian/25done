using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_2020_1_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// 自動建立 URP Global Volume 含 Bloom，專為暗夜場景（深藍色背景）優化。
/// 
/// Built-in Pipeline：此腳本自動跳過（不影響任何設定）。
/// URP Pipeline：自動建立含 Bloom 的 Global Volume。
/// 
/// 若是 Built-in Pipeline 想要 Bloom，請安裝：
/// Package Manager → com.unity.postprocessing
/// </summary>
public class BloomSetup : MonoBehaviour
{
    [Header("Bloom Settings (Dark Night Scene)")]
    [Tooltip("Bloom 強度，暗場景建議 2.0~4.0")]
    public float intensity = 3.0f;

    [Tooltip("觸發 Bloom 的亮度門檻，暗場景建議 0.3~0.5（越低越敏感）")]
    public float threshold = 0.35f;

    [Tooltip("Bloom 擴散範圍 0~1")]
    public float scatter = 0.75f;

    [Tooltip("Bloom 顏色偏藍/青，配合 Portal 發光")]
    public Color tint = new Color(0.7f, 0.85f, 1.0f, 1.0f);

    private void Awake()
    {
        // ── 偵測目前是否使用 URP ──────────────────────────────────
        bool isURP = GraphicsSettings.renderPipelineAsset != null &&
                     GraphicsSettings.renderPipelineAsset.GetType().Name
                         .Contains("Universal");

        if (!isURP)
        {
            // Built-in Pipeline：點光源仍然有效，Bloom 需額外安裝
            Debug.Log("BloomSetup: Built-in Pipeline 模式 — Bloom 已跳過。\n" +
                      "Portal 的點光源效果（PortalLightEffect）仍正常運作。\n" +
                      "如需 Bloom，請安裝 com.unity.postprocessing。");
            return;
        }

#if UNITY_2020_1_OR_NEWER
        // ── URP 模式：建立 Global Volume with Bloom ───────────────
        Volume[] volumes = FindObjectsOfType<Volume>();
        foreach (var v in volumes)
        {
            if (v.profile != null && v.profile.Has<Bloom>())
            {
                Debug.Log("BloomSetup: 場景已有 Bloom Volume，跳過建立。");
                return;
            }
        }

        GameObject volumeObj = new GameObject("Global Post Processing Volume (Night Bloom)");
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.value  = intensity;
        bloom.threshold.value  = threshold;
        bloom.scatter.value    = scatter;
        bloom.tint.value       = tint;
        bloom.highQualityFiltering.value = true;

        Debug.Log("BloomSetup: URP Global Bloom Volume 已建立！");
#endif
    }
}
