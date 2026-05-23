using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossController : Enemy {

	AudioSource AS;
	public AudioClip hurtSound;

	[Header("Ending Delay (seconds after death)")]
	public float endingDelay = 3f;

	void Start () {
		start();
		AS = GetComponent<AudioSource>();
		keepBodyAfterDeath = true;   // Boss 死後保留屍體
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

			Vector3 pushBack = (transform.position - attack.position).normalized;
			pushBack *= attack.strength;
			GetComponent<Rigidbody>().AddForce(pushBack * 10, ForceMode.Impulse);

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

	protected override void Die()
	{
		base.Die();
		StartCoroutine(ShowEndingAfterDelay());
	}

	private IEnumerator ShowEndingAfterDelay()
	{
		// 等待 endingDelay 秒（使用真實時間，避免慢動作影響）
		yield return new WaitForSecondsRealtime(endingDelay);

		EndingUI endingUI = FindObjectOfType<EndingUI>();
		if (endingUI == null)
		{
			Debug.LogWarning("[BossController] 找不到場景中的 EndingUI，請確認場景中有掛 EndingUI 腳本的物件。");
			yield break;
		}

		PlayerLife playerLife = FindObjectOfType<PlayerLife>();
		int remainHP = playerLife != null ? Mathf.RoundToInt(playerLife.HP) : 0;
		int gemCount = CrystalCount.crystal;

		endingUI.ShowEnding(remainHP, gemCount);
	}

	void Update () {
		update();
	}

	void LateUpdate() {
		lateUpdate();
	}
}
