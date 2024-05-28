using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A1101 : MonoBehaviour
{
    private PlayerStatControl playerStat;
    private void Awake()
    {

            playerStat = GetComponent<PlayerStatControl>();
            playerStat.EnemyHitEvent += HitPlusDamege; // 중요한부분
     }
    // Update is called once per frame
    void HitPlusDamege()
    {
        playerStat.ATK.added += 0.5f; // 중요한 부분2
    }
}
