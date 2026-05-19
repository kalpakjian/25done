using UnityEngine;
using System.Collections;

public class Portal : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject portalVisual;       // The Procedural Portal Effect object
    [SerializeField] private Transform targetPoint;         // Destination position (empty GameObject)
    [SerializeField] private Portal destinationPortal;      // The portal at the destination (optional)
    [SerializeField] private bool isEndpoint = false;       // Is this an endpoint portal (no teleportation)

    [Header("Fog Override (地底房間請勾選)")]
    [SerializeField] private bool overrideFog = false;      // 傳送後是否覆蓋霧設定
    [SerializeField] private bool disableFogCompletely = true; // 直接停用霧組件（最強力）
    [SerializeField] private float targetFogHeight = -1000f;// 目的地的霧高度（地底設低於房間底部）
    [SerializeField] private float targetFogDensity = 0f;  // 目的地的霧密度（地底建議設為 0）

    [Header("Animation")]
    [SerializeField] private float scaleUpDuration = 1.5f;  // Time to grow from 0 to full size
    [SerializeField] private float scaleDownDuration = 1.5f;// Time to shrink from full size to 0
    [SerializeField] private float disappearDelay = 5f;     // Seconds to wait before shrinking

    private Vector3 originalScale;
    private bool playerTeleported = false;

    private void Awake()
    {
        if (portalVisual != null)
        {
            originalScale = portalVisual.transform.localScale;

            // 防止 Prefab 被儲存時縮放已是零，導致 ScaleUp 無效（Build 常見問題）
            if (originalScale == Vector3.zero)
            {
                originalScale = Vector3.one;
                Debug.LogWarning($"[Portal] {gameObject.name} 的 portalVisual 初始縮放為零，已自動重設為 Vector3.one。請檢查 Prefab 設定。");
            }

            portalVisual.transform.localScale = Vector3.zero;
            portalVisual.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[Portal] {gameObject.name} 的 portalVisual 未指定！請在 Inspector 中設定。");
        }
    }

    /// <summary>
    /// Called by the Boss's dieEvent to make the portal appear (scale up).
    /// Also activates the destination portal if assigned.
    /// </summary>
    public void ActivatePortal()
    {
        if (portalVisual != null)
        {
            portalVisual.SetActive(true);
            // 確保 originalScale 有效（防禦性檢查）
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
            StartCoroutine(ScaleUp());
            Debug.Log($"[Portal] {gameObject.name} is appearing! originalScale={originalScale}");
        }
        else
        {
            Debug.LogError($"[Portal] {gameObject.name} ActivatePortal 呼叫時 portalVisual 為 null！");
        }

        // Also activate the destination portal
        if (destinationPortal != null)
        {
            destinationPortal.ActivateAsDestination();
        }
    }

    /// <summary>
    /// Called internally to activate the destination portal (scale up only, no chain).
    /// </summary>
    public void ActivateAsDestination()
    {
        if (portalVisual != null)
        {
            portalVisual.SetActive(true);
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
            StartCoroutine(ScaleUp());
            Debug.Log($"[Portal] {gameObject.name} (destination) is appearing!");
        }
        else
        {
            Debug.LogError($"[Portal] {gameObject.name} ActivateAsDestination 呼叫時 portalVisual 為 null！");
        }
    }

    private IEnumerator ScaleUp()
    {
        float elapsed = 0f;
        portalVisual.transform.localScale = Vector3.zero;

        while (elapsed < scaleUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleUpDuration);
            // Ease out for a nice effect
            float eased = 1f - (1f - t) * (1f - t);
            portalVisual.transform.localScale = originalScale * eased;
            yield return null;
        }

        portalVisual.transform.localScale = originalScale;
    }

    private IEnumerator ScaleDown()
    {
        float elapsed = 0f;
        Vector3 startScale = portalVisual.transform.localScale;

        while (elapsed < scaleDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleDownDuration);
            // Ease in for a nice shrink
            float eased = 1f - t * t;
            portalVisual.transform.localScale = startScale * eased;
            yield return null;
        }

        portalVisual.transform.localScale = Vector3.zero;
        portalVisual.SetActive(false);
    }

    /// <summary>
    /// Start the disappear sequence: wait, then shrink.
    /// </summary>
    public void StartDisappear()
    {
        StartCoroutine(DisappearAfterDelay());
    }

    private IEnumerator DisappearAfterDelay()
    {
        yield return new WaitForSeconds(disappearDelay);
        yield return StartCoroutine(ScaleDown());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!playerTeleported && other.CompareTag("Player"))
        {
            if (isEndpoint)
            {
                // Endpoint portal: no teleportation, just mark as reached
                playerTeleported = true;
                Debug.Log("Player reached endpoint portal. No teleportation.");
            }
            else
            {
                if (targetPoint == null)
                {
                    Debug.LogError("Target Point is not assigned in Portal script!");
                    return;
                }

                playerTeleported = true;
                Debug.Log("Player entered portal. Teleporting to " + targetPoint.position);

                // Disable CharacterController so we can move the player directly
                CharacterController cc = other.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                other.transform.position = targetPoint.position;

                if (cc != null) cc.enabled = true;

                // 相機瞬間跳到新位置，避免 Lerp 緩移途中渲染黑暗空間
                CameraFollow cam = Camera.main?.GetComponent<CameraFollow>();
                if (cam != null) cam.SnapToPosition(targetPoint.position);

                // 覆蓋霧設定（地底房間用）
                if (overrideFog)
                {
                    PostProcessHeightFog fog = FindObjectOfType<PostProcessHeightFog>();
                    if (fog != null)
                    {
                        if (disableFogCompletely)
                        {
                            fog.enabled = false;  // 完全停用霧效果
                            Debug.Log("Portal: Height Fog 已停用");
                        }
                        else
                        {
                            fog.fogHeight = targetFogHeight;
                            fog.fogDensity = targetFogDensity;
                            fog.SetFogProperties();
                            Debug.Log("Portal: Height Fog 已更新");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Portal: 找不到 PostProcessHeightFog 組件！");
                    }
                }

                // 強制刷新 Light Probe，避免傳送後燈光全黑
                foreach (var renderer in other.GetComponentsInChildren<Renderer>())
                {
                    renderer.UpdateGIMaterials();
                }
                LightProbes.Tetrahedralize();
            }

            // After teleport or reaching endpoint: both portals wait, then shrink and disappear
            StartDisappear();
            if (destinationPortal != null)
            {
                destinationPortal.StartDisappear();
            }
        }
    }
}
