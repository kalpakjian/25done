using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraObstructionFader_BuiltIn : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;
    public Vector3 targetOffset = Vector3.up * 1.2f;

    [Header("Detection")]
    public LayerMask obstructionMask;
    public bool ignoreTrigger = true;

    [Header("Main Fade Corridor")]
    public bool useSphereCast = true;
    public float mainCastRadius = 0.6f;

    [Header("Outer Soft Fade Area")]
    public float outerCheckRadius = 1.4f;
    [Range(0.05f, 1f)] public float mainFadeAlpha = 0.28f;
    [Range(0.05f, 1f)] public float outerFadeAlpha = 0.65f;

    [Header("Fade")]
    public float fadeSpeed = 8f;

    [Header("Debug")]
    public bool showDebugRay = false;

    private readonly Dictionary<Renderer, FadeData> _faders = new Dictionary<Renderer, FadeData>();
    private readonly Dictionary<Renderer, float> _desiredAlphaThisFrame = new Dictionary<Renderer, float>();
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

        _desiredAlphaThisFrame.Clear();

        Vector3 origin = transform.position;
        Vector3 target = playerTarget.position + targetOffset;
        Vector3 dir = target - origin;
        float distance = dir.magnitude;

        if (distance <= 0.01f) return;
        dir.Normalize();

        if (showDebugRay)
        {
            Debug.DrawLine(origin, target, Color.cyan);
        }

        QueryTriggerInteraction queryTrigger = ignoreTrigger
            ? QueryTriggerInteraction.Ignore
            : QueryTriggerInteraction.Collide;

        RaycastHit[] hits = useSphereCast
            ? Physics.SphereCastAll(origin, mainCastRadius, dir, distance, obstructionMask, queryTrigger)
            : Physics.RaycastAll(origin, dir, distance, obstructionMask, queryTrigger);

        for (int i = 0; i < hits.Length; i++)
        {
            Renderer rend = GetValidRenderer(hits[i].collider);
            if (rend == null) continue;

            MarkDesiredAlpha(rend, mainFadeAlpha);
        }

        Collider[] outerCols = Physics.OverlapCapsule(
            origin,
            target,
            outerCheckRadius,
            obstructionMask,
            queryTrigger
        );

        for (int i = 0; i < outerCols.Length; i++)
        {
            Renderer rend = GetValidRenderer(outerCols[i]);
            if (rend == null) continue;

            float corridorDistance = DistanceRendererToSegment(rend.bounds, origin, target);

            if (corridorDistance <= outerCheckRadius)
            {
                float t = Mathf.InverseLerp(outerCheckRadius, mainCastRadius, corridorDistance);
                float blendedAlpha = Mathf.Lerp(outerFadeAlpha, mainFadeAlpha, t);
                MarkDesiredAlpha(rend, blendedAlpha);
            }
        }

        foreach (var kv in _desiredAlphaThisFrame)
        {
            if (!_faders.ContainsKey(kv.Key))
            {
                CacheRenderer(kv.Key);
            }

            SetTargetAlpha(kv.Key, kv.Value);
        }

        foreach (var kv in _faders)
        {
            Renderer rend = kv.Key;
            if (!_desiredAlphaThisFrame.ContainsKey(rend))
            {
                SetTargetAlpha(rend, 1f);
            }
        }

        _cleanupList.Clear();

        foreach (var kv in _faders)
        {
            Renderer rend = kv.Key;
            FadeData data = kv.Value;

            bool finishedRestore = UpdateFade(rend, data);

            if (finishedRestore && !_desiredAlphaThisFrame.ContainsKey(rend))
            {
                _cleanupList.Add(rend);
            }
        }

        for (int i = 0; i < _cleanupList.Count; i++)
        {
            RemoveRenderer(_cleanupList[i]);
        }
    }

    void MarkDesiredAlpha(Renderer rend, float alpha)
    {
        if (rend == null) return;

        if (_desiredAlphaThisFrame.TryGetValue(rend, out float existing))
        {
            if (alpha < existing)
                _desiredAlphaThisFrame[rend] = alpha;
        }
        else
        {
            _desiredAlphaThisFrame.Add(rend, alpha);
        }
    }

    Renderer GetValidRenderer(Collider col)
    {
        if (col == null) return null;

        Renderer rend = col.GetComponent<Renderer>();
        if (rend == null)
            rend = col.GetComponentInParent<Renderer>();

        return rend;
    }

    float DistanceRendererToSegment(Bounds bounds, Vector3 a, Vector3 b)
    {
        Vector3 p = bounds.center;
        Vector3 ab = b - a;
        float abSqr = ab.sqrMagnitude;

        if (abSqr <= 0.0001f)
            return Vector3.Distance(p, a);

        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / abSqr);
        Vector3 closest = a + ab * t;

        float centerDistance = Vector3.Distance(p, closest);
        float approxExtent = bounds.extents.magnitude * 0.35f;

        return Mathf.Max(0f, centerDistance - approxExtent);
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

    bool UpdateFade(Renderer rend, FadeData data)
    {
        bool allRestored = true;

        for (int i = 0; i < data.materials.Length; i++)
        {
            Material mat = data.materials[i];
            if (mat == null) continue;

            float target = data.targetAlpha[i];
            float current = data.currentAlpha[i];
            float next = Mathf.MoveTowards(current, target, fadeSpeed * Time.deltaTime);

            data.currentAlpha[i] = next;

            Color baseColor = data.originalColors[i];
            baseColor.a = next;
            SetMaterialColor(mat, baseColor);

            bool restoring = Mathf.Approximately(target, data.originalColors[i].a);
            bool restored = Mathf.Abs(next - data.originalColors[i].a) < 0.01f;

            if (!(restoring && restored))
            {
                allRestored = false;
            }
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

            Color original = data.originalColors[i];
            SetMaterialColor(mat, original);

            if (original.a >= 0.99f)
            {
                SetupMaterialOpaque(mat);
            }
        }

        _faders.Remove(rend);
    }

    Color GetMaterialColor(Material mat)
    {
        if (mat.HasProperty("_Color"))
            return mat.color;

        return Color.white;
    }

    void SetMaterialColor(Material mat, Color color)
    {
        if (mat.HasProperty("_Color"))
        {
            mat.color = color;
        }
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
        _desiredAlphaThisFrame.Clear();
        _cleanupList.Clear();
    }
}