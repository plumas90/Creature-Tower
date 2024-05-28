using UnityEngine;

public class A1302 : MonoBehaviour
{

    private WeaponSystem weaponSystem;
    private void Awake()
    {

            weaponSystem = GetComponent<WeaponSystem>();
            /*if (GameManager.Instance != null) 
            {
                GameManager.Instance.OnStageStartEvent += reloaing;
                GameManager.Instance.OnBossStageStartEvent += reloaing;
            }*/
    }
    // Update is called once per frame
    void reloaing()
    {
        weaponSystem.canresurrection = true;
    }
}
