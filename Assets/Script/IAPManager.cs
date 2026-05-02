using UnityEngine;

public class IAPManager : MonoBehaviour
{

    public void Purchase100Crystal()
    {
        CrystalCount.CrystalUpdate(100);
    }

    public void Purchase500Crystal()
    {
        CrystalCount.CrystalUpdate(500);
    }

    public void PurchaseAdsRemove()
    {
        PlayerPrefs.SetInt("adsRemoved", 1);
        PlayerPrefs.Save();
    }
}


