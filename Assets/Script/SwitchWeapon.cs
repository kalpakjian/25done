using UnityEngine;

public class SwitchWeapon : MonoBehaviour
{
    public GameObject currentWeapon;
    public GameObject[] weapon;

    void Start()
    {
        int weaponNow = PlayerPrefs.GetInt("myweapon", -1);
        if (weaponNow != -1)
            StartSwitch(weaponNow);
    }

    public void StartSwitch(int id)
    {
        Destroy(currentWeapon);
        currentWeapon = Instantiate(weapon[id], transform);
        PlayerPrefs.SetInt("myweapon", id);
        PlayerPrefs.Save();
    }
}


