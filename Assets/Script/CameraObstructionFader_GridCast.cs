using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraObstructionFader_GridCast : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;
    public Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Grid Sampling From Camera")]
    public float gridHalfWidth = 1.8f;
    public float gridHalfHeight = 1.0f;
    [Range(1, 9)] public int gridXCount = 5;
    [Range(1, 9)] public int gridYCount = 3;
    public float sphereCastRadius = 0.28f;

    [Header("Weighted Fade")]
    [Range(0.05f, 1f)] public float centerAlpha = 0.28f;
    [Range(0.05f, 1f)] public float sideAlpha = 0.65f;
    [Range(0.1f, 4f)] public float centerFocus = 1.6f;

    [Header("Detection")]
    public LayerMask obstructionMask;
    public bool ignoreTrigger = true;

    [Header("Fade")]
    public float fadeSpeed = 8f;

    [Header("Debug")]
    public bool showDebug = false;

    private readonly Dictionary<Renderer, FadeData> _faders = new Dictionary<Renderer, FadeData>();
    private readonly Dictionary<Renderer, float> _desiredAlphaThisFrame = new Dictionary<Renderer, float>();
    private readonly List<Renderer> _cleanupList = new List<Renderer>();
    private static Shader _transparentFadeShader;

    private class FadeData
    {
        public Material[] materials;
        public Shader[] originalShaders;
        public int[] originalRenderQueues;
        public Color[] originalColors;
        public float[] currentAlpha;
        public float[] targetAlpha;
    }

    void LateUpdate()
    {
        if (playerTarget == null) return;

        _desiredAlphaThisFrame.Clear();

        Vector3 targetPoint = playerTarget.position + targetOffset;

        QueryTriggerInteraction queryTrigger = ignoreTrigger
            ? QueryTriggerInteraction.Ignore
            : QueryTriggerInteraction.Collide;

        for (int y = 0; y < gridYCount; y++)
        {
            float yNorm = gridYCount == 1 ? 0.5f : (float)y / (gridYCount - 1);
            float ySigned = Mathf.Lerp(-1f, 1f, yNorm);
            float yOffset = ySigned * gridHalfHeight;

            for (int x = 0; x < gridXCount; x++)
            {
                float xNorm = gridXCount == 1 ? 0.5f : (float)x / (gridXCount - 1);
                float xSigned = Mathf.Lerp(-1f, 1f, xNorm);
                float xOffset = xSigned * gridHalfWidth;

                Vector3 origin =
                    transform.position +
                    transform.right * xOffset +
                    transform.up * yOffset;

                float distFromCenter = Mathf.Sqrt(xSigned * xSigned + ySigned * ySigned) / Mathf.Sqrt(2f);
                float weight = Mathf.Pow(Mathf.Clamp01(distFromCenter), centerFocus);
                float alpha = Mathf.Lerp(centerAlpha, sideAlpha, weight);

                CastFromOriginToTarget(origin, targetPoint, alpha, queryTrigger);
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
            if (!_desiredAlphaThisFrame.ContainsKey(kv.Key))
            {
                SetTargetAlpha(kv.Key, 1f);
            }
        }

        _cleanupList.Clear();

        foreach (var kv in _faders)
        {
            bool finishedRestore = UpdateFade(kv.Value);

            if (finishedRestore && !_desiredAlphaThisFrame.ContainsKey(kv.Key))
            {
                _cleanupList.Add(kv.Key);
            }
        }

        for (int i = 0; i < _cleanupList.Count; i++)
        {
            RemoveRenderer(_cleanupList[i]);
        }
    }

    void CastFromOriginToTarget(Vector3 origin, Vector3 targetPoint, float alpha, QueryTriggerInteraction queryTrigger)
    {
        Vector3 dir = targetPoint - origin;
        float distance = dir.magnitude;
        if (distance <= 0.01f) return;

        dir.Normalize();

        if (showDebug)
        {
            Color debugColor = Color.Lerp(Color.red, Color.green, Mathf.InverseLerp(sideAlpha, centerAlpha, alpha));
            Debug.DrawLine(origin, targetPoint, debugColor);
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
            Collider hitCol = hits[i].collider;
            if (hitCol == null) continue;

            Renderer rend = hitCol.GetComponent<Renderer>();
            if (rend == null) continue;

            MarkDesiredAlpha(rend, alpha);
        }
    }

    void MarkDesiredAlpha(Renderer rend, float alpha)
    {
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

    void CacheRenderer(Renderer rend)
    {
        Material[] mats = rend.materials;

        FadeData data = new FadeData
        {
            materials = mats,
            originalShaders = new Shader[mats.Length],
            originalRenderQueues = new int[mats.Length],
            originalColors = new Color[mats.Length],
            currentAlpha = new float[mats.Length],
            targetAlpha = new float[mats.Length]
        };

        for (int i = 0; i < mats.Length; i++)
        {
            Material mat = mats[i];
            if (mat == null) continue;

            data.originalShaders[i] = mat.shader;
            data.originalRenderQueues[i] = mat.renderQueue;

            Color color = GetMaterialColor(mat);
            SetupMaterialForFade(mat);
            SetMaterialColor(mat, color);

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
                RestoreOriginalMaterial(mat, data.originalShaders[i], data.originalRenderQueues[i]);
            }
        }

        _faders.Remove(rend);
    }

    Color GetMaterialColor(Material mat)
    {
        if (mat.HasProperty("_BaseColor"))
            return mat.GetColor("_BaseColor");

        return mat.HasProperty("_Color") ? mat.color : Color.white;
    }

    void SetMaterialColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.color = color;
    }

    void SetupMaterialForFade(Material mat)
    {
        if (mat == null) return;

        Shader fadeShader = GetTransparentFadeShader();
        if (fadeShader != null)
        {
            Texture mainTexture = GetKnownMainTexture(mat);
            mat.shader = fadeShader;
            if (mainTexture != null && mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", mainTexture);
            if (mainTexture != null && mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", mainTexture);
            mat.renderQueue = 3000;
            return;
        }

        if (IsURPMaterial(mat))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            return;
        }

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

    void RestoreOriginalMaterial(Material mat, Shader originalShader, int originalRenderQueue)
    {
        if (mat == null) return;

        if (originalShader != null)
            mat.shader = originalShader;

        mat.renderQueue = originalRenderQueue;

        SetupMaterialOpaque(mat);
    }

    void SetupMaterialOpaque(Material mat)
    {
        if (mat == null) return;

        if (IsURPMaterial(mat))
        {
            mat.SetFloat("_Surface", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = -1;
            return;
        }

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

    bool IsURPMaterial(Material mat)
    {
        return mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline");
    }

    Shader GetTransparentFadeShader()
    {
        if (_transparentFadeShader == null)
            _transparentFadeShader = Resources.Load<Shader>("ObstructionTransparent");

        if (_transparentFadeShader == null)
            _transparentFadeShader = Shader.Find("Custom/ObstructionTransparent");

        return _transparentFadeShader;
    }

    Texture GetKnownMainTexture(Material mat)
    {
        if (mat.HasProperty("_BaseMap"))
        {
            Texture baseMap = mat.GetTexture("_BaseMap");
            if (baseMap != null)
                return baseMap;
        }

        if (mat.HasProperty("_MainTex"))
            return mat.GetTexture("_MainTex");

        return null;
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
                    RestoreOriginalMaterial(mat, data.originalShaders[i], data.originalRenderQueues[i]);
                }
            }
        }

        _faders.Clear();
        _desiredAlphaThisFrame.Clear();
        _cleanupList.Clear();
    }
}