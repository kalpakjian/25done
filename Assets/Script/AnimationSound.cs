using System.Linq;
using UnityEngine;

public class AnimationSound : StateMachineBehaviour {

	public AudioClip[] sounds;
	private AudioSource audioSource;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

		if (audioSource == null) {
			audioSource = animator.GetComponent<AudioSource>();
		}

		if (audioSource == null) {
			Debug.LogWarning($"AnimationSound: No AudioSource found on '{animator.name}'.");
			return;
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
