using UnityEngine;

public class A0113 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;

    private int nowgold;
    private float nowpower;
    private float oldpower;
    private void Awake()
    {

            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            //nowgold = GameManager.Instance.TeamGold;
            oldpower = 0;
            //GameManager.Instance.ChangeGoldEvent += setgold; 
    }
    // Update is called once per frame
    void setgold()
    {
        nowpower = nowgold * 0.05f;
        playerStat.ATK.added += nowpower; 
        playerStat.ATK.added -= oldpower; 
        oldpower = nowpower;
    }
}
