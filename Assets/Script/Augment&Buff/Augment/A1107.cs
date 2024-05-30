using System.Collections.Generic;
using UnityEngine;

public class A1107 : MonoBehaviour //ÁÖº¯Èú
{
    float time = 0;
    List<PlayerStatControl> colleagueList = new List<PlayerStatControl>();
    int healP=4;
    public GameObject Player;

 
    public void Init()
    {
        PlayerStatControl playerStat = transform.parent.gameObject.GetComponent<PlayerStatControl>();
        if (!colleagueList.Contains(playerStat)) 
        {
            colleagueList.Add(playerStat);
        }

    }
    

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerStatControl target = collision.gameObject.GetComponent<PlayerStatControl>();
        if (target != null) 
        {
            colleagueList.Add(target);
        }
        
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerStatControl target = collision.gameObject.GetComponent<PlayerStatControl>();
        if (target != null)
        {
            colleagueList.Remove(target);
        }

    }

    private void FixedUpdate()
    {
        time += Time.deltaTime;
        if (time >= 1f && (colleagueList.Count>=1)) 
        {
            if (colleagueList.Count >= 1) 
            {
                Heal();
                time = 0F;
            }
        }
    }
    private void Heal() 
    {
        for (int i = 0; i < colleagueList.Count; ++i) 
        {

            if (colleagueList[i].CurHP >=1f) 
            {
                colleagueList[i].HPadd(healP);
            }
        }
    }
}
