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
}
