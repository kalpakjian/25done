using UnityEngine;
using UnityEngine.UI;

public class CrystalCount : MonoBehaviour {

	public static Text crystalText;
	public static int crystal;

	void Start () {
		crystalText = GetComponent<Text>();
		crystal = 0;
		CrystalUpdate(0);
	}
	
	public static void CrystalUpdate (int amount) {
		crystal += amount;
		crystalText.text = "x" + crystal.ToString();
	}
}
