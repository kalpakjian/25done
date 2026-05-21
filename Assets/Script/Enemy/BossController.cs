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
		// 三連擊收尾技無視 hurtInterval，確保推力一定觸發
		bool isComboFinisher = attack.attackStep >= 3;

		if (!dead && (isComboFinisher || Time.time >= nextHurtTime))
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

			// 第三擊（收尾技）無論 canPushEnemy 是否勾選，一律強制推擊
			if (attack.canPushEnemy || attack.attackStep >= 3)
			{
				Vector3 pushBack = (transform.position - attack.position).normalized;
				pushBack.y = 0f;
				if (pushBack.sqrMagnitude < 0.001f)
					pushBack = Vector3.forward;
				pushBack.Normalize();

				float pushSpeed = attack.attackStep >= 3 ? 0.4f
				                : attack.attackStep == 2 ? 0.25f
				                : 0.15f;
				pushBack *= Mathf.Max(attack.strength, 1) * pushSpeed;
				GetComponent<Rigidbody>().AddForce(pushBack, ForceMode.VelocityChange);
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
