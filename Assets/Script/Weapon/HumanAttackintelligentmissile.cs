using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanAttackintelligentmissile : MonoBehaviour
{
    bool targeting;
    GameObject target;
    Bullet _bullet;
    private float turningForce;
    public bool ready;
    int targetName;

    // ## 중요 ## 스타트에서 기본값을 설정하면 중간에 생성된 물체의 스타트를 컨트롤 하기 어려움 아래처럼 이니시에이팅 줄여서 init을 만들고 그걸 실행시켜주는게
    // 컨트롤이 쉬움
    //public void Start()
    //{
    //targeting = false;
    //_bullet = GetComponentInParent<Bullet>();
    //turningForce = 15f;
    //targetName = "Enemy";
    //}
    public void init(int i) // 2개인 이유 거북이 유도 공격도 같은 원리였음 속도 다르게 할려고 2개로함
    {
        if (i == 1)
        {
            targeting = false;
            _bullet = GetComponentInParent<Bullet>();
            turningForce = 15f;
            targetName = 7;
            ready = true;
        }
        else
        {
            transform.localScale = new Vector3(5, 5, 0);
            targeting = false;
            _bullet = GetComponentInParent<Bullet>();
            turningForce = 7f;
            targetName = 8;
            ready = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (targeting)
        {
            if (target == null) { return; }
            Vector2 dir = (target.transform.position - transform.position).normalized;
            _bullet.gameObject.transform.right = Vector3.Slerp(_bullet.gameObject.transform.right.normalized, dir, turningForce * Time.deltaTime);
            //slerp == 보간 처리
        }
    }
    private void OnTriggerEnter2D(Collider2D collision) // 총알에 일정 거리 이내로 들어올시 타겟팅 트루로 바꿔 유도 시작
    {
        if (ready && !targeting && collision.gameObject.layer == targetName)
        {
            targeting = true;
            target = collision.gameObject;
        }
    }
}
