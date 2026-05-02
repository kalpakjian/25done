using UnityEngine;
using UnityEngine.UI;

public class ShowHP : MonoBehaviour {

	public Text HPText;
	public Image HPBar;
	PlayerLife PL;

	void Start () {
		PL = GameObject.FindObjectOfType<PlayerLife>();
	}
	
	void Update () {
		HPBar.fillAmount = PL.HP / PL.maxHP;
		HPText.text = PL.HP.ToString() + " / " + PL.maxHP.ToString();
	}
}

