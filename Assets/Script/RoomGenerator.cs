using UnityEngine;
using System.Collections.Generic;

public class RoomGenerator : MonoBehaviour
{
    [Header("Floor Grid")]
    public GameObject floorPrefab;
    public int floorCountX = 8;
    public int floorCountZ = 6;
    public float tileSpacingX = 1f;
    public float tileSpacingZ = 1f;

    [Header("Walls")]
    public float wallSpacing = 1f;
    [Tooltip("如果你的牆模型本身不是以本地 +Z 朝向正面，可在這裡補償 Y 旋轉。0 = 模型正面就是 +Z")]
    public float wallModelYawOffset = 0f;

    [Header("Seed")]
    public bool useSeed = false;
    public int seed = 12345;

    [Header("Prefabs")]
    public List<GameObject> wallPrefabs = new List<GameObject>();
    public List<GameObject> cornerPrefabs = new List<GameObject>();

    [Header("Roots")]
    public Transform floorRoot;
    public Transform wallRoot;

    [Header("Debug")]
    public bool drawGizmos = true;
    public bool drawCornerLabels = true;
    public bool drawWallNormals = true;
    public bool drawFloorBounds = true;

    private Vector3[] debugCorners = new Vector3[0];

    private List<int> wallBag = new List<int>();
    private int lastWallPrefabIndex = -1;

    [ContextMenu("Generate Room")]
    public void GenerateRoom()
    {
        Random.State previousRandomState = Random.state;

        try
        {
            if (useSeed)
                Random.InitState(seed);

            EnsureRoots();
            ClearRoomInternal();
            ResetWallBag();

            if (floorPrefab == null && wallPrefabs.Count == 0 && cornerPrefabs.Count == 0)
            {
                Debug.LogWarning("RoomGenerator: No floorPrefab, wallPrefabs, or cornerPrefabs assigned!");
                return;
            }

            debugCorners = GetRoomCornersLocal();

            GenerateFloor();
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
        EnsureRoots();
        ClearRoomInternal();
        ResetWallBag();
        debugCorners = new Vector3[0];
    }

    [ContextMenu("Bake Room")]
    public void BakeRoom()
    {
        EnsureRoots();

        if ((floorRoot == null || floorRoot.childCount == 0) &&
            (wallRoot == null || wallRoot.childCount == 0))
        {
            GenerateRoom();
        }

        debugCorners = new Vector3[0];

        #if UNITY_EDITOR
        DestroyImmediate(this);
        #else
        Destroy(this);
        #endif
    }

    private void EnsureRoots()
    {
        if (floorRoot == null)
        {
            Transform existing = transform.Find("FloorRoot");
            if (existing != null)
                floorRoot = existing;
            else
            {
                GameObject go = new GameObject("FloorRoot");
                go.transform.SetParent(transform, false);
                floorRoot = go.transform;
            }
        }

        if (wallRoot == null)
        {
            Transform existing = transform.Find("WallRoot");
            if (existing != null)
                wallRoot = existing;
            else
            {
                GameObject go = new GameObject("WallRoot");
                go.transform.SetParent(transform, false);
                wallRoot = go.transform;
            }
        }
    }

    private void ClearRoomInternal()
    {
        ClearChildrenOf(floorRoot);
        ClearChildrenOf(wallRoot);
    }

    private void ClearChildrenOf(Transform root)
    {
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;

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

    private void GenerateFloor()
    {
        if (floorPrefab == null) return;

        for (int x = 0; x < floorCountX; x++)
        {
            for (int z = 0; z < floorCountZ; z++)
            {
                Vector3 localPos = new Vector3(x * tileSpacingX, 0f, z * tileSpacingZ);
                GameObject tile = Instantiate(floorPrefab, floorRoot);
                tile.transform.localPosition = localPos;
                tile.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private Vector3[] GetRoomCornersLocal()
    {
        float minX = -tileSpacingX * 0.5f;
        float minZ = -tileSpacingZ * 0.5f;
        float maxX = (floorCountX - 0.5f) * tileSpacingX;
        float maxZ = (floorCountZ - 0.5f) * tileSpacingZ;

        return new Vector3[]
        {
            new Vector3(minX, 0f, minZ),
            new Vector3(maxX, 0f, minZ),
            new Vector3(maxX, 0f, maxZ),
            new Vector3(minX, 0f, maxZ)
        };
    }

    private Vector3 GetRoomCenterLocal()
    {
        float centerX = ((floorCountX - 1) * tileSpacingX) * 0.5f;
        float centerZ = ((floorCountZ - 1) * tileSpacingZ) * 0.5f;
        return new Vector3(centerX, 0f, centerZ);
    }

    private void SpawnCorners()
    {
        if (cornerPrefabs == null || cornerPrefabs.Count == 0) return;

        float[] cornerRotations = { 0f, -90f, 180f, 90f };

        for (int i = 0; i < debugCorners.Length; i++)
        {
            GameObject prefab = GetRandomCornerPrefab();
            if (prefab == null) continue;

            GameObject corner = Instantiate(prefab, wallRoot);
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

            GameObject wall = Instantiate(prefab, wallRoot);
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
            wallBag.Add(validIndices[i]);

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
            candidateNormal = -candidateNormal;

        return candidateNormal;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Vector3[] corners = GetRoomCornersLocal();

        if (drawFloorBounds)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 a = transform.TransformPoint(corners[i]);
                Vector3 b = transform.TransformPoint(corners[(i + 1) % corners.Length]);
                Gizmos.DrawLine(a, b);
            }
        }

        Gizmos.color = Color.red;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(corners[i]);
            Gizmos.DrawSphere(worldPos, 0.12f);
        }

        Vector3 roomCenterWorld = transform.TransformPoint(GetRoomCenterLocal());
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(roomCenterWorld, 0.1f);

        if (drawWallNormals)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 start = corners[i];
                Vector3 end = corners[(i + 1) % corners.Length];

                Vector3 mid = (start + end) * 0.5f;
                Vector3 normal = GetInwardNormal(start, end);

                Vector3 worldMid = transform.TransformPoint(mid);
                Vector3 worldNormalEnd = transform.TransformPoint(mid + normal * 1.0f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(worldMid, worldNormalEnd);
            }
        }

        if (drawCornerLabels)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(corners[i]);
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.2f, $"Corner {i + 1}");
            }

            UnityEditor.Handles.Label(roomCenterWorld + Vector3.up * 0.2f, "Center");
        }
    }
#endif
}