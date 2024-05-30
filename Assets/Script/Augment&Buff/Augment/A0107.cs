using UnityEngine;

public class A0107 : MonoBehaviour
{
    private PlayerStatControl playerStat;
    public float power;
    public float oldpower;
    bool Ismove;
    float powerTime = 0f;
    private void Awake()
    {

            playerStat = GetComponent<PlayerStatControl>();
            playerStat.MoveStartEvent += MoveStartEvent;
            playerStat.MoveEndEvent += MoveEndEvent;
            power = 0;
            oldpower = 0;
            powerTime = 0f;
            Ismove =false;

    }   
    private void Update()
    {
        if (!Ismove) 
        {
            playerStat.ATK.added += (Time.deltaTime) * 1f;
            power += Time.deltaTime * 1f;
            powerTime += Time.deltaTime;
        }

    }

    // Update is called once per frame
    void MoveStartEvent()
    {
        playerStat.ATK.added -= power;
        power = 0;
        Ismove = true;
    }
    void MoveEndEvent() 
    {
        Ismove=false;
        powerTime = 0f;
    }
}
