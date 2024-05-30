using System.Collections.Generic;
using UnityEngine;

public class A2204 : MonoBehaviour
{
    List<PlayerStatControl> target = new List<PlayerStatControl>();
    PlayerStatControl me;
    public GameObject Player;
    private TopDownCharacterController controller;
        // Update is called once per frame
    void TogetherParty()
    {
        for (int i = 0; i < target.Count; ++i) 
        {
            //Debuff.Instance.GiveLowSteamPack(target[i].gameObject);
        }
    }
    
    public void Init()
    {
        me = transform.parent.gameObject.GetComponent<PlayerStatControl>();
        controller = me.gameObject.GetComponent<TopDownCharacterController>();
        controller.OnSkillEvent += TogetherParty;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerStatControl player = collision.GetComponent<PlayerStatControl>();
        if (player != null)
        {
            target.Add(player);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerStatControl player = collision.GetComponent<PlayerStatControl>();
        if (player != null)
        {
            target.Remove(player);
        }
    }
}
