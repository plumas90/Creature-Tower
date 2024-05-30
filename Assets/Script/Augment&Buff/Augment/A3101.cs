using UnityEngine;

public class A3101 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private CoolTimeController coolTimeController;

    public float heal=12f;
    public float healTime = 5f;
    float time = 0f;
    private void Awake()
    {

            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            coolTimeController = GetComponent<CoolTimeController>();

            playerStat.HitEvent += restartTime; // 중요한부분
    }
    private void Update()
    {
            time += Time.deltaTime;
            if (time >= healTime)
            {
                StayHeal();
                time = 0f;
            }
    }
    // Update is called once per frame
    void StayHeal()
    {
            playerStat.HPadd(heal);
    }
    void restartTime() 
    {
        time = 0;
    }
}
