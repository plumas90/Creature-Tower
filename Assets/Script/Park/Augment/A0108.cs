using UnityEngine;

public class A0108 : MonoBehaviour
{
    private PlayerStatControl playerStat;
    private void Awake()
    {

            playerStat = GetComponent<PlayerStatControl>();
            playerStat.KillCatchEvent += DrainPower; // 중요한부분

    }
    // Update is called once per frame
    void DrainPower()
    {
        if (playerStat.CanSpeedBuff) 
        {
            //Debuff.Instance.GiveTouchSpeed(this.gameObject);
        }
    }
}
