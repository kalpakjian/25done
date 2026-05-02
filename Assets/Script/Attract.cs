using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Attract : MonoBehaviour {

	public float offset = 0.5f;
	public AudioClip getSound;
	AudioSource AS;
	Collider mycol;
	Transform player;
	Vector3 targetPos;
	float speed=5;
	float rotSpeed=360;
	float startDelay = 1f;
	bool startMove;

	void Start () {
		startMove = false;
		Invoke("TriggerStart", startDelay);
		AS = GetComponent<AudioSource>();
		mycol = GetComponent<Collider>();
		player = GameObject.FindWithTag("Player").transform;
	}

	void Update () {
		if (startMove)
		{
			targetPos = player.position;
			targetPos.y += offset;
			transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
		}
	}

	void LateUpdate () {
		if (startMove)
		{
			transform.RotateAround(player.position, Vector3.up, rotSpeed * Time.deltaTime);
			transform.Rotate(Vector3.up * 720 * Time.deltaTime, Space.World);
		}
	}

	void OnTriggerStay (Collider col) {
		if (startMove)
		{
			if (col.CompareTag("Player"))
			{
				CrystalCount.CrystalUpdate(1);
				mycol.enabled = false;
				transform.localScale = Vector3.zero;
				AS.PlayOneShot(getSound);
				Destroy(gameObject, 3);
			}
		}
	}

	void TriggerStart()
	{
		startMove = true;
	}
}

