using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour {

	public Transform target;           
	public float smoothing = 5f;

	[Header("Camera Angle")]
	[Tooltip("設定相機的旋轉角度。X 軸越大越接近俯視（建議 50-75），Y 軸控制左右方向。")]
	public Vector3 cameraRotation = new Vector3(60f, 45f, 0f);

	[Header("Camera Position Offset")]
	[Tooltip("相機相對於玩家的額外位移。調整 Y 可以讓鏡頭更高或更低。")]
	public Vector3 positionOffset = new Vector3(0f, 0f, 0f);

    Rigidbody rb;
    Vector3 offset;             
	
	
	void Start () {
		// 套用自訂旋轉角度
		transform.rotation = Quaternion.Euler(cameraRotation);

		offset = transform.position - target.position;
		offset *= 2; // Zoom out to make player appear half size
        rb = target.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 targetCamPos = rb.position + offset + positionOffset;
        transform.position = Vector3.Lerp(transform.position,
                                            targetCamPos,
                                            smoothing * Time.deltaTime);
    }

    /// <summary>
    /// 瞬間跳到目標位置（用於傳送後避免相機緩移途中全黑）
    /// </summary>
    public void SnapToTarget()
    {
        if (rb != null)
        {
            transform.position = rb.position + offset + positionOffset;
        }
    }

    /// <summary>
    /// 傳入目標世界座標，相機直接跳到對應位置（傳送瞬間用，不依賴 rb.position）
    /// </summary>
    public void SnapToPosition(Vector3 targetWorldPos)
    {
        transform.position = targetWorldPos + offset + positionOffset;
    }
}
