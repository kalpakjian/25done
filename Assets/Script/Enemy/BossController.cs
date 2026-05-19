using UnityEngine;
using System.Collections.Generic;

public class BossController : Enemy {

	AudioSource AS;
	public AudioClip hurtSound;

	void Start () {
		start();
		AS = GetComponent<AudioSource>();
	}

	protected override void Hurt(Attack attack)
	{
		if (!dead && Time.time >= nextHurtTime)
		{
			SlowMotion.Slow(0.1f, 5);
			HP -= attack.damage;
			if (HPBar) HPBar.fillAmount = HP / maxHP;

			for (int i = 0; i < material.Count; i++)
				material[i].SetColor("_EmissionColor", Color.white);

			Invoke("StopEmission", 0.05f);

			if (HP <= 0)
			{
				Die();
				return;
			}
			AS.PlayOneShot(hurtSound);

			if (attack.canPushEnemy)
			{
				Vector3 pushBack = (transform.position - attack.position).normalized;
				pushBack *= attack.strength;
				GetComponent<Rigidbody>().AddForce(pushBack * 10, ForceMode.Impulse);
			}

			if (attack.type == AttackType.frozen)
			{
				for (int i = 0; i < material.Count; i++)
					material[i].color = frozenColor;
				anim.speed = 0.8f;
				Invoke("Recover", 5);
			}
			nextHurtTime = Time.time + hurtInterval;
		}
	}

	void Update () {
		update();
	}

	void LateUpdate() {
		lateUpdate();
	}
}
