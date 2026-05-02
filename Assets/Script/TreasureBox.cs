using UnityEngine;

public class TreasureBox : MonoBehaviour {

	public GameObject rewardItem;
	public AudioClip openSound;
	Animator anim;
	AudioSource AS;
	Collider col;

	void Start () {
		anim = GetComponent <Animator>();
		AS = GetComponent <AudioSource>();
		col = GetComponent <Collider>();
	}
	
	void Hit () {
		col.enabled = false;
		AS.PlayOneShot(openSound);
		anim.Play("OpenChest");
		if (rewardItem)
			Instantiate (rewardItem, transform.position, Quaternion.identity);
		Destroy (gameObject, 3);
	}
}
