
using UnityEngine;

public class A0111 : MonoBehaviour//공격을 하지 않은 시간에 비례하여 다음 공격의 공격력이 증가 합니다.

{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private float power;

    bool stop;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            power = 0f;
            controller.OnAttackEvent += StartAtk;
            controller.OnEndAttackEvent += StopAtk;
            stop = true;
    }
    private void Update()
    {
        if (stop) 
        {
            playerStat.ATK.added += (Time.deltaTime) * 0.5f;
            power += Time.deltaTime * 0.5f;
        }
    }
    void StartAtk()
    {
        stop = false;
    }
    void StopAtk()
    {
        if (!stop) 
        {
            playerStat.ATK.added -= power;
            power = 0f;
            stop = true;
        }
    }
}
