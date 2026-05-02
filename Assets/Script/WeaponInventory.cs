using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class WeaponInventory : MonoBehaviour
{

    public AudioClip failSound;
    public AudioClip purchaseSound;
    public AudioClip equipSound;
    public int[] price;

    bool[] weapon;
    Button[] weaponbtn;
    Text[] weapontext;
    Image[] weaponimg;
    int currentWeapon;
     
    AudioSource AS;
    SwitchWeapon sw;
 
    void Awake()
    {
        AS = GetComponent<AudioSource>();
        weaponbtn = GetComponentsInChildren<Button>();
        weapontext = new Text[weaponbtn.Length];
        
        for(int i=0; i <weaponbtn.Length; i++)
        {
            weapontext[i] = weaponbtn[i].GetComponentInChildren<Text>();
            weapontext[i].text = price[i].ToString("$####");
        }

        weaponimg = GetComponentsInChildren<Image>();
        weapon = new bool[weaponbtn.Length];
        sw = GameObject.FindObjectOfType<SwitchWeapon>();
        currentWeapon = 999;
    }

    private void OnEnable()
    {
        UpdateWeaponButton();
    }

    void UpdateWeaponButton()
    {
        for (int i = 0; i < weapon.Length; i++)
        {
            int bought = PlayerPrefs.GetInt("DataWeapon" + i, 0);
            if (bought == 1)
            {
                weaponimg[i].color = Color.white;
                weapontext[i].text = "";
                if (i == currentWeapon)
                    weapontext[i].text = "<Color=orange>Equiped</Color>";
            }
            else
            {
                weaponimg[i].color = Color.gray;
            }
        }
    }

    public void BuyWeapon(int id)
    {
        if (PlayerPrefs.GetInt("DataWeapon" + id, 0) == 0)
        {
            if (CrystalCount.crystal >= price[id])   
            {
                CrystalCount.CrystalUpdate(-price[id]);
                PlayerPrefs.SetInt("DataWeapon" + id, 1);
                PlayerPrefs.Save();
                AS.PlayOneShot(purchaseSound);
            } else
            {
                AS.PlayOneShot(failSound);
            }
        } else
        {
            sw.StartSwitch(id);  
            currentWeapon = id;
            AS.PlayOneShot(equipSound);
        }
        UpdateWeaponButton();
    }
}
