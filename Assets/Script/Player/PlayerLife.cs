using UnityEngine;

public class PlayerLife : MonoBehaviour {

	public float maxHP = 100;
	public static bool dead;
	
	[HideInInspector]
	public float HP;

	Animator anim;
	PlayerController controller;

	float nextHurtTime=0;
	public float hurtInterval=0.5f;

	[HideInInspector]
	public bool invincible;

	void Start () {
		anim = GetComponent<Animator>();
		controller = GetComponent<PlayerController>();
		HP = maxHP;
		dead = false;
	}

	void Hurt (float damage) {
		if (!dead && Time.time >= nextHurtTime && !invincible)
		{
			CameraShake.Shake(0.25f, 0.2f);
			HP -= damage;
			if (HP <= 0) {
				Die();
				return;
			}
			anim.SetTrigger("hurt");
			nextHurtTime = Time.time + hurtInterval;
		}
	}

	void Die () {
		dead = true;
		anim.SetTrigger("die");
		controller.enabled = false;
		SlowMotion.Slow(5, 10);
	}
}
