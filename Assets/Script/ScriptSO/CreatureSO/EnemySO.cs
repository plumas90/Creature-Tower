using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
[CreateAssetMenu(fileName = "EnemySO", menuName = "ScriptableObject/EnemySO", order = int.MinValue)]
public class EnemySO : ScriptableObject
{
    [Header("EnemySO")]
    public string enemyName;      // �̸�
    //public EnemyType type;        // �� ����(BT��� �ൿ ��� �ҰŶ� �ʿ������ �����)
    public float atk;             // ���ݷ�
    public float hp;              // ü��
    public float speed;
    public int bossCount;
    public float IntroAnimationTime;

    //public float bulletSpeed;     // źȯ �ӵ�
    //public float atkDelay;        // ���� ������
    //public float SpecialAttackDelay; // Ư�� ���� ������
    //public float patrolDelay;     // ���� ������
    //public float chaseTime;       // ���� �����ð�
    //public float groggyTiem;      // ���� �ð�
    //public float bulletLifeTime;  // �Ѿ� ���� �ð�
    //public float breathAttackDelay; // �극�� ���� ����
    //public float enemyMoveSpeed;  // �⺻ �̵��ӵ�
    //public float enemyChaseSpeed; // �⺻ �����ӵ�
    //public float viewAngle;       // Ž�� ����
    //public float viewDistance;     // Ž�� ����
    //public float bulletSpeed;     // źȯ ӵ
    //public float atkDelay;        //  
    //public float SpecialAttackDelay; // Ư  
    //public float patrolDelay;     //  
    //public float chaseTime;       //  ð
    //public float groggyTiem;      //  ð
    //public float bulletLifeTime;  // Ѿ  ð
    //public float breathAttackDelay; // 극  
    //public float enemyMoveSpeed;  // ⺻ ̵ӵ
    //public float enemyChaseSpeed; // ⺻ ӵ
    //public float viewAngle;       // Ž 
    //public float viewDistance;     // Ž 
    //public float attackRange;     //  
    //public int dropGold;          // ִ ȭ 
    //public int unitScale;         //  ũ
    //public SpriteLibraryAsset enemySpriteLibrary;
    //public Bullet enemyBulletPrefab;

    //

    public float bossPatternTime;



    //[Header("Boss BT")] 
    [HideInInspector] public float btChaseRange = 100f;
    [HideInInspector] public float btStopDistance = 0.6f;
    [HideInInspector] public float btRepathInterval = 0.15f;
    [HideInInspector] public float btMoveSpeedMultiplier = 1f;
    [HideInInspector] [Range(0f, 1f)] public float btChaseChance = 1f;
}
/*
 * public enum EnemyType
{
    Melee,                                       // �ٰŸ�
    Ranged,                                      // ���Ÿ�
    Coward,                                      // ������ : ���� ü�� ���ϸ�, ���� �ð� �޾Ƴ�
                                                 //TODO ����
}
*/