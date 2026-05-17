using UnityEngine;
using System.Collections.Generic;

public class WallGenerator : MonoBehaviour
{
    [Header("Room Dimensions")]
    public float roomWidth = 10f;
    public float roomLength = 10f;
    public float wallSpacing = 2f;

    [Header("Model Orientation")]
    [Tooltip("如果你的牆模型本身不是以本地 +Z 朝向正面，可在這裡補償 Y 旋轉。0 = 模型正面就是 +Z")]
    public float wallModelYawOffset = 0f;

    [Header("Seed")]
    public bool useSeed = false;
    public int seed = 12345;

    [Header("Prefabs")]
    public List<GameObject> wallPrefabs = new List<GameObject>();
    public List<GameObject> cornerPrefabs = new List<GameObject>();

    [Header("Debug")]
    public bool drawGizmos = true;
    public bool drawCornerLabels = true;
    public bool drawWallNormals = true;

    private Vector3[] debugCorners = new Vector3[0];

    // Bag system for walls
    private List<int> wallBag = new List<int>();
    private int lastWallPrefabIndex = -1;

    [ContextMenu("Generate Room")]
    public void GenerateRoom()
    {
        Random.State previousRandomState = Random.state;

        try
        {
            if (useSeed)
            {
                Random.InitState(seed);
            }

            ClearRoomInternal();
            ResetWallBag();

            if (cornerPrefabs.Count == 0 && wallPrefabs.Count == 0)
            {
                Debug.LogWarning("WallGenerator: No prefabs assigned!");
                return;
            }

            debugCorners = GetRoomCornersLocal();

            SpawnCorners();

            SpawnWallEdge(debugCorners[0], debugCorners[1]);
            SpawnWallEdge(debugCorners[1], debugCorners[2]);
            SpawnWallEdge(debugCorners[2], debugCorners[3]);
            SpawnWallEdge(debugCorners[3], debugCorners[0]);
        }
        finally
        {
            Random.state = previousRandomState;
        }
    }

    [ContextMenu("Clear Room")]
    public void ClearRoom()
    {
        ClearRoomInternal();
        ResetWallBag();
        debugCorners = new Vector3[0];
    }

    private void ClearRoomInternal()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void ResetWallBag()
    {
        wallBag.Clear();
        lastWallPrefabIndex = -1;
    }

    private Vector3[] GetRoomCornersLocal()
    {
        return new Vector3[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(roomWidth, 0f, 0f),
            new Vector3(roomWidth, 0f, roomLength),
            new Vector3(0f, 0f, roomLength)
        };
    }

    private Vector3 GetRoomCenterLocal()
    {
        return new Vector3(roomWidth * 0.5f, 0f, roomLength * 0.5f);
    }

    private void SpawnCorners()
    {
        if (cornerPrefabs == null || cornerPrefabs.Count == 0) return;

        float[] cornerRotations = { 0f, -90f, 180f, 90f };

        for (int i = 0; i < debugCorners.Length; i++)
        {
            GameObject prefab = GetRandomCornerPrefab();
            if (prefab == null) continue;

            GameObject corner = Instantiate(prefab, transform);
            corner.transform.localPosition = debugCorners[i];
            corner.transform.localRotation = Quaternion.Euler(0f, cornerRotations[i], 0f);
        }
    }

    private GameObject GetRandomCornerPrefab()
    {
        List<GameObject> validCorners = new List<GameObject>();

        for (int i = 0; i < cornerPrefabs.Count; i++)
        {
            if (cornerPrefabs[i] != null)
                validCorners.Add(cornerPrefabs[i]);
        }

        if (validCorners.Count == 0) return null;

        int index = Random.Range(0, validCorners.Count);
        return validCorners[index];
    }

    private void SpawnWallEdge(Vector3 start, Vector3 end)
    {
        if (wallPrefabs == null || wallPrefabs.Count == 0) return;

        float distance = Vector3.Distance(start, end);
        int segmentCount = Mathf.Max(1, Mathf.FloorToInt(distance / wallSpacing));

        Vector3 inwardNormal = GetInwardNormal(start, end);

        Quaternion baseRotation = Quaternion.LookRotation(inwardNormal, Vector3.up);
        Quaternion modelOffsetRotation = Quaternion.Euler(0f, wallModelYawOffset, 0f);
        Quaternion finalRotation = baseRotation * modelOffsetRotation;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (i + 0.5f) / segmentCount;
            Vector3 pos = Vector3.Lerp(start, end, t);

            GameObject prefab = GetWallPrefabFromBag();
            if (prefab == null) continue;

            GameObject wall = Instantiate(prefab, transform);
            wall.transform.localPosition = pos;
            wall.transform.localRotation = finalRotation;
        }
    }

    private GameObject GetWallPrefabFromBag()
    {
        if (wallPrefabs == null || wallPrefabs.Count == 0)
            return null;

        List<int> validIndices = new List<int>();
        for (int i = 0; i < wallPrefabs.Count; i++)
        {
            if (wallPrefabs[i] != null)
                validIndices.Add(i);
        }

        if (validIndices.Count == 0)
            return null;

        if (validIndices.Count == 1)
        {
            lastWallPrefabIndex = validIndices[0];
            return wallPrefabs[validIndices[0]];
        }

        if (wallBag.Count == 0)
        {
            RefillAndShuffleWallBag(validIndices);
        }

        int pickedIndex = wallBag[0];
        wallBag.RemoveAt(0);
        lastWallPrefabIndex = pickedIndex;

        return wallPrefabs[pickedIndex];
    }

    private void RefillAndShuffleWallBag(List<int> validIndices)
    {
        wallBag.Clear();

        for (int i = 0; i < validIndices.Count; i++)
        {
            wallBag.Add(validIndices[i]);
        }

        for (int i = wallBag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = wallBag[i];
            wallBag[i] = wallBag[j];
            wallBag[j] = temp;
        }

        if (wallBag.Count > 1 && wallBag[0] == lastWallPrefabIndex)
        {
            int swapIndex = Random.Range(1, wallBag.Count);
            int temp = wallBag[0];
            wallBag[0] = wallBag[swapIndex];
            wallBag[swapIndex] = temp;
        }
    }

    private Vector3 GetInwardNormal(Vector3 start, Vector3 end)
    {
        Vector3 wallDirection = (end - start).normalized;
        Vector3 candidateNormal = Vector3.Cross(Vector3.up, wallDirection).normalized;

        Vector3 edgeMid = (start + end) * 0.5f;
        Vector3 toCenter = (GetRoomCenterLocal() - edgeMid).normalized;

        if (Vector3.Dot(candidateNormal, toCenter) < 0f)
        {
            candidateNormal = -candidateNormal;
        }

        return candidateNormal;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Vector3[] corners = GetRoomCornersLocal();

        Gizmos.color = Color.red;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(corners[i]);
            Gizmos.DrawSphere(worldPos, 0.15f);
        }

        Gizmos.color = Color.white;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 a = transform.TransformPoint(corners[i]);
            Vector3 b = transform.TransformPoint(corners[(i + 1) % corners.Length]);
            Gizmos.DrawLine(a, b);
        }

        Vector3 roomCenterWorld = transform.TransformPoint(GetRoomCenterLocal());
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(roomCenterWorld, 0.12f);

        if (drawWallNormals)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 start = corners[i];
                Vector3 end = corners[(i + 1) % corners.Length];

                Vector3 mid = (start + end) * 0.5f;
                Vector3 normal = GetInwardNormal(start, end);

                Vector3 worldMid = transform.TransformPoint(mid);
                Vector3 worldNormalEnd = transform.TransformPoint(mid + normal * 1.2f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(worldMid, worldNormalEnd);
            }
        }

        if (drawCornerLabels)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(corners[i]);
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.25f, $"Corner {i + 1}");
            }

            UnityEditor.Handles.Label(roomCenterWorld + Vector3.up * 0.25f, "Center");
        }
    }
#endif
}