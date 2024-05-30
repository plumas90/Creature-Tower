using UnityEngine;

public class A0211 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private CoolTimeController coolTimeController;

    int persent = 2;
    int maxpersent = 10;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            playerStat.HitEvent2 += Endure;
    }
    void Endure(float damege)
    {
        int Per = Random.Range(persent, maxpersent);
        if (persent >= Per)
        {
            playerStat.HPadd(damege * 1.2f);
        }
    }
}
