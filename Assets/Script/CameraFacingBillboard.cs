using UnityEngine;
using System.Collections;

public class CameraFacingBillboard : MonoBehaviour {

	public Camera myCamera;
	public bool flip=true;

	void Start () {
		myCamera = Camera.main;
	}

	void Update() {

		transform.LookAt(transform.position + myCamera.transform.rotation * Vector3.back,
						myCamera.transform.rotation * Vector3.up);
		if (flip)
			transform.Rotate(0,180,0);
	}
}