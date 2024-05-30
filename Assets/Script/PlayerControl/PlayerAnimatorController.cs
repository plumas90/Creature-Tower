using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using static UnityEditor.PlayerSettings;

public class PlayerAnimatorController : MonoBehaviour
{
    [SerializeField] private GameObject playerSprite;
    [SerializeField] private GameObject weaponSprite;

    private TopDownCharacterController characterController;
    private PlayerStatControl playerStatControl;

    private SpriteRenderer playerRenderer;
    private SpriteRenderer weaponRenderer;
    [HideInInspector] public int isBack;
    public Animator _animation;
    private Animator weaponAnimator;
    private SpriteLibrary PlayerSpritelibrary;
    private SpriteLibrary WeaponSpritelibrary;

    //private PhotonView pv;

    private void Awake()
    {
        //pv = GetComponent<PhotonView>();
        _animation = playerSprite.GetComponent<Animator>();
        weaponAnimator = weaponSprite.GetComponent<Animator>();
        PlayerSpritelibrary = playerSprite.GetComponent<SpriteLibrary>();
        WeaponSpritelibrary = weaponSprite.GetComponent<SpriteLibrary>();
        playerStatControl = GetComponent<PlayerStatControl>();

        playerRenderer = playerSprite.GetComponentInChildren<SpriteRenderer>();
        weaponRenderer = weaponSprite.GetComponentInChildren<SpriteRenderer>();
    }
    private void Start()
    {
        characterController = GetComponent<TopDownCharacterController>();
        PlayerSpritelibrary.spriteLibraryAsset = playerStatControl.PlayerSprite;
        WeaponSpritelibrary.spriteLibraryAsset = playerStatControl.WeaponSprite;
        characterController.OnMoveEvent += MoveAnimator;
        characterController.OnRollEvent += RPCRollAnimator;
        characterController.OnLookEvent += LookBack;
        characterController.OnAttackEvent += Fire;
        //playerStatControl.OnDieEvent += Die;
        //playerStatControl.OnRegenEvent += Regen;
        //playerStatControl.HitEvent += ColorTeen;
    }


    private void Fire()
    {
        weaponAnimator.SetTrigger("IsFire");
    }


    private void LookBack(Vector2 direction)
    {
        float rotY = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        if (Mathf.Abs(rotY) > 90f)
        {
            _animation.SetFloat("IsLookBack", 0);
            WSO(rotY);
            //pv.RPC("WSO", RpcTarget.AllBuffered, rotY); 동기화 처리
        }
        else
        {
            _animation.SetFloat("IsLookBack", 1);
            WSO(rotY);
            //pv.RPC("WSO", RpcTarget.AllBuffered, rotY);

        }
    }
    void WSO(float rotY)//WeaponSortingOrder 기존 플레이어 레이어 5 즉 총을 위로 향한다면 플레이어 레이어가 위에 있어서 총을 일부 가림 아래쪽은 다보임 처리
    {
        if (Mathf.Abs(rotY) > 90f)
        {
            weaponRenderer.sortingOrder = 11;
        }
        else
        {
            weaponRenderer.sortingOrder = 9;
        }
    }

    private void MoveAnimator(Vector2 direction)
    {
        if (direction != Vector2.zero)
        {
            _animation.SetBool("IsRun", true);
        }
        else
        {
            _animation.SetBool("IsRun", false);
        }
    }


    private void RPCRollAnimator()
    {
        RollAnimator();
        //pv.RPC("RollAnimator", RpcTarget.AllBuffered);
    }

    private void RollAnimator()
    {
        _animation.SetTrigger("IsRoll");
        weaponRenderer.color = new Vector4(255, 255, 255, 0); // 구를때 무기 안보이기 처리
        Invoke("EndRollAnimator", 0.7f);
    }

    private void EndRollAnimator()
    {
        weaponRenderer.color = new Vector4(255, 255, 255, 255);
    }
    private void ColorTeen()
    {

    }

    /* private void Die() 죽음 처리인데 싱글겜이라 어캐 될지 모르겟음
    {
        _animation.SetTrigger("IsDie");
        int viewID = pv.ViewID;
        pv.RPC("PunDie", RpcTarget.OthersBuffered, viewID);
    }

    [PunRPC]
    private void PunDie(int viewID)
    {
        PhotonView pv = PhotonView.Find(viewID);
        pv.GetComponent<PlayerAnimatorController>()._animation.SetTrigger("IsDie");
    }


    public void Regen()
    {
        Debug.Log("부활");
        _animation.SetTrigger("IsRegen");
        int viewID = pv.ViewID;
        pv.RPC("PunRegen", RpcTarget.OthersBuffered, viewID);
    }

    [PunRPC]
    private void PunRegen(int viewID)
    {
        PhotonView pv = PhotonView.Find(viewID);
        pv.GetComponent<PlayerAnimatorController>()._animation.SetTrigger("IsRegen");
    }
     */
}