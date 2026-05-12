using UnityEngine;

/// <summary>
/// 掛在 Portal GameObject 上，當 Portal 啟動時自動建立脈動點光源，
/// 讓 Portal 在暗夜場景中照亮周遭環境，產生真實的發光感。
/// </summary>
[RequireComponent(typeof(Portal))]
public class PortalLightEffect : MonoBehaviour
{
    [Header("Point Light Settings")]
    [Tooltip("Portal 發光顏色（建議青藍色配合 Portal 主色）")]
    public Color lightColor = new Color(0.3f, 0.75f, 1.0f, 1.0f);

    [Tooltip("點光源最大強度")]
    public float maxIntensity = 4.0f;

    [Tooltip("點光源照射半徑（場景單位）")]
    public float lightRange = 8.0f;

    [Header("Pulse Animation")]
    [Tooltip("脈動速度（Hz）")]
    public float pulseSpeed = 1.2f;

    [Tooltip("脈動幅度（佔最大強度的比例 0~1）")]
    [Range(0f, 0.5f)]
    public float pulseAmount = 0.25f;

    [Header("Fade In/Out")]
    [Tooltip("Portal 出現時光源淡入時間（秒）")]
    public float fadeInDuration = 1.5f;

    [Tooltip("Portal 消失時光源淡出時間（秒）")]
    public float fadeOutDuration = 1.5f;

    // ── 內部狀態 ──────────────────────────────────────────────
    private Light _pointLight;
    private float _fadeT   = 0f;   // 0 = 完全熄滅, 1 = 完全亮起
    private bool  _fadingIn  = false;
    private bool  _fadingOut = false;

    // ─────────────────────────────────────────────────────────
    private void Awake()
    {
        CreatePointLight();
    }

    /// <summary>建立子物件 Point Light（初始關閉）</summary>
    private void CreatePointLight()
    {
        GameObject lightObj = new GameObject("PortalPointLight");
        lightObj.transform.SetParent(transform, worldPositionStays: false);
        lightObj.transform.localPosition = Vector3.zero;

        _pointLight            = lightObj.AddComponent<Light>();
        _pointLight.type       = LightType.Point;
        _pointLight.color      = lightColor;
        _pointLight.intensity  = 0f;
        _pointLight.range      = lightRange;
        _pointLight.shadows    = LightShadows.None; // 避免效能開銷
        _pointLight.enabled    = false;             // 預設關閉，待 Portal 啟動
    }

    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 由 Portal.ActivatePortal() 呼叫（或由 Portal 腳本中的事件觸發）。
    /// 也可以直接在 Inspector 中把此方法掛到 UnityEvent。
    /// </summary>
    public void StartFadeIn()
    {
        if (_pointLight == null) return;
        _pointLight.enabled = true;
        _fadingOut = false;
        _fadingIn  = true;
    }

    /// <summary>
    /// 由 Portal.StartDisappear() 呼叫，開始淡出。
    /// </summary>
    public void StartFadeOut()
    {
        if (_pointLight == null) return;
        _fadingIn  = false;
        _fadingOut = true;
    }

    private void Update()
    {
        if (_pointLight == null || !_pointLight.enabled) return;

        // ── 淡入 ──
        if (_fadingIn)
        {
            _fadeT += Time.deltaTime / Mathf.Max(fadeInDuration, 0.01f);
            if (_fadeT >= 1f)
            {
                _fadeT    = 1f;
                _fadingIn = false;
            }
        }

        // ── 淡出 ──
        if (_fadingOut)
        {
            _fadeT -= Time.deltaTime / Mathf.Max(fadeOutDuration, 0.01f);
            if (_fadeT <= 0f)
            {
                _fadeT             = 0f;
                _fadingOut         = false;
                _pointLight.enabled = false;
                return;
            }
        }

        // ── 脈動 ──
        float pulse = 1f - pulseAmount + pulseAmount * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
        _pointLight.intensity = maxIntensity * _fadeT * pulse;
    }

    // ─────────────────────────────────────────────────────────
    // 方便從 Portal.cs 呼叫而不需額外 GetComponent
    // ─────────────────────────────────────────────────────────

    /// <summary>在 Editor 中即時預覽發光（Gizmo 圓圈）</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.3f);
        Gizmos.DrawWireSphere(transform.position, lightRange);
    }
}
