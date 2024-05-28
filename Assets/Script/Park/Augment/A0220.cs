using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A0220 : MonoBehaviour
{
    public int percent;
    private PlayerStatControl stats;
    
    private void Awake()
    {
        percent = 0;
        stats = GetComponent<PlayerStatControl>();
        stats.EnemyHitEvent += Drain;
    }
    // Update is called once per frame
    void Drain()
    {
        int random = Random.Range(0, 100);
        if (percent > random)
        {
            int healAmount = (int)(stats.ATK.total * 0.1f);
            stats.HPadd(healAmount);
        }
    }

    public void PercentUp(int PerUp) //PercentUp
    {
        percent += PerUp;
    }
}
