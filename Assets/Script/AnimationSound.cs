using UnityEngine;

public class AnimationSound : StateMachineBehaviour {

	public AudioClip[] sounds;
	AudioSource AS;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

		AS = animator.GetComponent<AudioSource>();
		int index = Random.Range(0,sounds.Length);
	 	AS.PlayOneShot(sounds[index]);

	}
}

