using System.Collections;
using UnityEngine;

public class A0127 : MonoBehaviour
{
    private PlayerStatControl playerStat;
    int time = 5;
    float regenhp;
    WaitForSeconds autoTime;
    private void Awake()
    {

        playerStat = GetComponent<PlayerStatControl>();
        regenhp = 5f;
        //photonView.RPC("AutoHealingStart", RpcTarget.All);
        autoTime = new WaitForSeconds(time);
        StartCoroutine("AutoHealing");

    }
    private void OnEnable()
    {
        AutoHealingStart();
        //photonView.RPC("AutoHealingStart", RpcTarget.All);
    }

    void AutoHealingStart()
    {
            StopCoroutine("AutoHealing");
            StartCoroutine("AutoHealing");

    }
    IEnumerator AutoHealing()
    {
        while (true)
        {
            if (playerStat.CurHP <= 0) 
            {
                yield return null;
            }
            playerStat.HPadd(regenhp);
            yield return autoTime;
        }
    }
}
