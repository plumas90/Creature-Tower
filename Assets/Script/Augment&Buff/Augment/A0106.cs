
using UnityEngine;

public class A0106 : MonoBehaviour
{
    private PlayerStatControl playerStat;
    private void Awake()
    {
            playerStat = GetComponent<PlayerStatControl>();
            playerStat.KillCatchEvent += DrainPower;
    }
    // Update is called once per frame
    void DrainPower()
    {
        playerStat.ATK.added += 1f;
    }
}
