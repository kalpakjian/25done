using UnityEngine;

public class FloorArraySpawner : MonoBehaviour
{
    public GameObject floorPrefab;
    public int width = 8;
    public int height = 6;
    public float spacingX = 1f;
    public float spacingZ = 1f;

    void Start()
    {
        GenerateFloor();
    }

    public void GenerateFloor()
    {
        if (floorPrefab == null)
        {
            Debug.LogError("FloorArraySpawner: floorPrefab is not assigned.");
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 localPos = new Vector3(x * spacingX, 0f, z * spacingZ);
                GameObject tile = Instantiate(floorPrefab, transform);
                tile.transform.localPosition = localPos;
                tile.transform.localRotation = Quaternion.identity;
            }
        }
    }

    // 例如：在 Editor 按一個按鈕重新生成
    [ContextMenu("Rebuild Floor")]
    public void RebuildFloor()
    {
        GenerateFloor();
    }
}