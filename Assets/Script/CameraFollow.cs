using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour {

	public Transform target;           
	public float smoothing = 5f;
    Rigidbody rb;

    Vector3 offset;             
	
	
	void Start () {
		offset = transform.position - target.position;
        rb = target.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 targetCamPos = rb.position + offset;
        transform.position = Vector3.Lerp(transform.position,
                                            targetCamPos,
                                            smoothing * Time.deltaTime);
    }
}