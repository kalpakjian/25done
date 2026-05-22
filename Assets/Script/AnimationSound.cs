using System.Linq;
using UnityEngine;

public class AnimationSound : StateMachineBehaviour {

	public AudioClip[] sounds;
	private AudioSource audioSource;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

	if (audioSource == null) {
			// 先找同物件，再找父物件
			audioSource = animator.GetComponent<AudioSource>();
			if (audioSource == null)
				audioSource = animator.GetComponentInParent<AudioSource>();
		}

		// 還是找不到 → 自動加一個（靜音防錯；怪物用 3D 音效）
		if (audioSource == null) {
			audioSource = animator.gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake  = false;
			audioSource.spatialBlend = 1f;   // 3D
			audioSource.rolloffMode  = AudioRolloffMode.Linear;
			audioSource.maxDistance  = 30f;
			Debug.Log($"[AnimationSound] 自動為 '{animator.name}' 加入 AudioSource。");
		}

		if (sounds == null || sounds.Length == 0) {
			Debug.LogWarning($"AnimationSound: No sounds assigned on '{animator.name}'.");
			return;
		}

		AudioClip[] validSounds = sounds.Where(s => s != null).ToArray();

		if (validSounds.Length == 0) {
			Debug.LogWarning($"AnimationSound: All assigned sounds are null on '{animator.name}'.");
			return;
		}

		int index = Random.Range(0, validSounds.Length);
		audioSource.PlayOneShot(validSounds[index]);
	}
}
