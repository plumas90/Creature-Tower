using UnityEngine;

public class A3207 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private CoolTimeController coolTimeController;
    private void Awake()
    {

            controller = GetComponent<TopDownCharacterController>();
            coolTimeController = GetComponent<CoolTimeController>();

            controller.OnAttackEvent += atkCoolTime;
    }
    // Update is called once per frame
    void atkCoolTime()
    {
        coolTimeController.curSkillCool -= 0.3f;
    }
}
