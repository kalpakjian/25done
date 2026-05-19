using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraObstructionFader_ModularWalls : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;
    public Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Camera-side Sampling")]
    public float horizontalOriginOffset = 0.45f;
    public float verticalOriginOffset = 0.3f;
    public float sphereCastRadius = 0.22f;

    [Header("Fade")]
    [Range(0.05f, 1f)] public float fadedAlpha = 0.3f;
    public float fadeSpeed = 8f;

    [Header("Detection")]
    public LayerMask obstructionMask;
    public bool ignoreTrigger = true;

    [Header("Debug")]
    public bool showDebug = false;

    private readonly Dictionary<Renderer, FadeData> _faders = new Dictionary<Renderer, FadeData>();
    private readonly HashSet<Renderer> _hitThisFrame = new HashSet<Renderer>();
    private readonly List<Renderer> _cleanupList = new List<Renderer>();

    private class FadeData
    {
        public Material[] materials;
        public Color[] originalColors;
        public float[] currentAlpha;
        public float[] targetAlpha;
    }

    void LateUpdate()
    {
        if (playerTarget == null) return;

        _hitThisFrame.Clear();

        Vector3 targetPoint = playerTarget.position + targetOffset;

        Vector3 camRight = transform.right * horizontalOriginOffset;
        Vector3 camUp = transform.up * verticalOriginOffset;

        Vector3[] origins = new Vector3[]
        {
            transform.position,
            transform.position + camRight,
            transform.position - camRight,
            transform.position + camUp,
            transform.position - camUp,
            transform.position + camRight + camUp,
            transform.position + camRight - camUp,
            transform.position - camRight + camUp,
            transform.position - camRight - camUp
        };

        QueryTriggerInteraction queryTrigger = ignoreTrigger
            ? QueryTriggerInteraction.Ignore
            : QueryTriggerInteraction.Collide;

        for (int i = 0; i < origins.Length; i++)
        {
            CastFromOriginToTarget(origins[i], targetPoint, queryTrigger);
        }

        foreach (var rend in _hitThisFrame)
        {
            if (!_faders.ContainsKey(rend))
            {
                CacheRenderer(rend);
            }

            SetTargetAlpha(rend, fadedAlpha);
        }

        foreach (var kv in _faders)
        {
            if (!_hitThisFrame.Contains(kv.Key))
            {
                SetTargetAlpha(kv.Key, 1f);
            }
        }

        _cleanupList.Clear();

        foreach (var kv in _faders)
        {
            bool finishedRestore = UpdateFade(kv.Value);

            if (finishedRestore && !_hitThisFrame.Contains(kv.Key))
            {
                _cleanupList.Add(kv.Key);
            }
        }

        for (int i = 0; i < _cleanupList.Count; i++)
        {
            RemoveRenderer(_cleanupList[i]);
        }
    }

    void CastFromOriginToTarget(Vector3 origin, Vector3 targetPoint, QueryTriggerInteraction queryTrigger)
    {
        Vector3 dir = targetPoint - origin;
        float distance = dir.magnitude;
        if (distance <= 0.01f) return;

        dir.Normalize();

        if (showDebug)
        {
            Debug.DrawLine(origin, targetPoint, Color.cyan);
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            sphereCastRadius,
            dir,
            distance,
            obstructionMask,
            queryTrigger
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Renderer rend = GetValidRenderer(hits[i].collider);
            if (rend == null) continue;

            if (!IsRendererBeforeTarget(rend.bounds, origin, targetPoint))
                continue;

            _hitThisFrame.Add(rend);
        }
    }

    bool IsRendererBeforeTarget(Bounds bounds, Vector3 origin, Vector3 targetPoint)
    {
        Vector3 line = targetPoint - origin;
        float lineLength = line.magnitude;
        if (lineLength <= 0.0001f) return false;

        Vector3 lineDir = line / lineLength;
        Vector3 toCenter = bounds.center - origin;
        float projectedDistance = Vector3.Dot(toCenter, lineDir);

        float objectRadius = bounds.extents.magnitude;

        if (projectedDistance + objectRadius < 0f)
            return false;

        if (projectedDistance - objectRadius > lineLength)
            return false;

        return true;
    }

    Renderer GetValidRenderer(Collider col)
    {
        if (col == null) return null;

        Renderer rend = col.GetComponent<Renderer>();
        if (rend == null)
            rend = col.GetComponentInParent<Renderer>();

        return rend;
    }

    void CacheRenderer(Renderer rend)
    {
        Material[] mats = rend.materials;

        FadeData data = new FadeData
        {
            materials = mats,
            originalColors = new Color[mats.Length],
            currentAlpha = new float[mats.Length],
            targetAlpha = new float[mats.Length]
        };

        for (int i = 0; i < mats.Length; i++)
        {
            Material mat = mats[i];
            if (mat == null) continue;

            SetupMaterialForFade(mat);

            Color color = GetMaterialColor(mat);
            data.originalColors[i] = color;
            data.currentAlpha[i] = color.a;
            data.targetAlpha[i] = color.a;
        }

        _faders.Add(rend, data);
    }

    void SetTargetAlpha(Renderer rend, float alpha)
    {
        if (!_faders.TryGetValue(rend, out FadeData data)) return;

        for (int i = 0; i < data.targetAlpha.Length; i++)
        {
            float originalA = data.originalColors[i].a;
            data.targetAlpha[i] = Mathf.Min(originalA, alpha);
        }
    }

    bool UpdateFade(FadeData data)
    {
        bool allRestored = true;

        for (int i = 0; i < data.materials.Length; i++)
        {
            Material mat = data.materials[i];
            if (mat == null) continue;

            float next = Mathf.MoveTowards(
                data.currentAlpha[i],
                data.targetAlpha[i],
                fadeSpeed * Time.deltaTime
            );

            data.currentAlpha[i] = next;

            Color c = data.originalColors[i];
            c.a = next;
            SetMaterialColor(mat, c);

            bool restoring = Mathf.Approximately(data.targetAlpha[i], data.originalColors[i].a);
            bool restored = Mathf.Abs(next - data.originalColors[i].a) < 0.01f;

            if (!(restoring && restored))
                allRestored = false;
        }

        return allRestored;
    }

    void RemoveRenderer(Renderer rend)
    {
        if (!_faders.TryGetValue(rend, out FadeData data)) return;

        for (int i = 0; i < data.materials.Length; i++)
        {
            Material mat = data.materials[i];
            if (mat == null) continue;

            SetMaterialColor(mat, data.originalColors[i]);

            if (data.originalColors[i].a >= 0.99f)
            {
                SetupMaterialOpaque(mat);
            }
        }

        _faders.Remove(rend);
    }

    Color GetMaterialColor(Material mat)
    {
        return mat.HasProperty("_Color") ? mat.color : Color.white;
    }

    void SetMaterialColor(Material mat, Color color)
    {
        if (mat.HasProperty("_Color"))
            mat.color = color;
    }

    void SetupMaterialForFade(Material mat)
    {
        if (mat == null) return;
        if (mat.shader.name != "Standard") return;

        mat.SetFloat("_Mode", 2f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    void SetupMaterialOpaque(Material mat)
    {
        if (mat == null) return;
        if (mat.shader.name != "Standard") return;

        mat.SetFloat("_Mode", 0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = -1;
    }

    void OnDisable()
    {
        ForceRestoreAll();
    }

    void OnDestroy()
    {
        ForceRestoreAll();
    }

    void ForceRestoreAll()
    {
        foreach (var kv in _faders)
        {
            FadeData data = kv.Value;

            for (int i = 0; i < data.materials.Length; i++)
            {
                Material mat = data.materials[i];
                if (mat == null) continue;

                SetMaterialColor(mat, data.originalColors[i]);

                if (data.originalColors[i].a >= 0.99f)
                {
                    SetupMaterialOpaque(mat);
                }
            }
        }

        _faders.Clear();
        _hitThisFrame.Clear();
        _cleanupList.Clear();
    }
}