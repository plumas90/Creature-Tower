using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CustomClip
{
    [SerializeField] string name;
    [SerializeField] AudioClip audio;
}

public class AudioLibrary : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private AudioClip loby;
    [SerializeField] private List<AudioClip> stage;
    [SerializeField] private List<AudioClip> boss;

    [Header("Player")]
    [SerializeField] private AudioClip player_rolling;
    [SerializeField] private AudioClip player_hit;

    private AudioClip player_attack;
    private AudioClip reloadStart;
    private AudioClip reloadFinish;

    [Header("Enemy")]
    [SerializeField] private AudioClip enemy_attack;
    [SerializeField] private AudioClip enemy_hit;

    private GameObject player;

    [HideInInspector]
    public event Action OnRoomSoundEvent;
        

    void Start()
    {
        if (GameManager.Instance != null)
        {
            player = GameManager.Instance.clientPlayer;
            SetupPlayerSE();
            AttachPlayerSE();
        }
    }


    // ADDED 
    public void CallRoomSoundEvent(GameObject newPlayer)
    {
        player = newPlayer;
        OnRoomSoundEvent += SetupPlayerSE;
        OnRoomSoundEvent += AttachPlayerSE;
        OnRoomSoundEvent?.Invoke();
    }

    // ADDED
    public void CallLobbySoundEvent()
    {
        if (player != null)
        {
            OnRoomSoundEvent -= SetupPlayerSE;
            OnRoomSoundEvent -= AttachPlayerSE;
            player = null;
        }

        AudioManager.PlayBGM(BGMList.Dragao_Inkomodo);
    }
    
    public void SetupPlayerSE()
    {
        if (player == null)
            return;

        var stats = player.GetComponent<PlayerStatControl>();
        if (stats == null)
            return;

        player_attack = stats.atkClip;
        reloadStart = stats.reloadStartClip;
        reloadFinish = stats.reloadFinishClip;
    }

    void AttachPlayerSE()
    {
        if (player == null)
            return;

        var controller = player.GetComponent<PlayerInputController>();
        var stats = player.GetComponent<PlayerStatControl>();

        if (controller == null || stats == null)
            return;

        // ���� ���� ȿ����
        controller.OnAttackEvent += PlayShotSE;
        controller.OnRollEvent += PlayRollingSE;
        controller.OnReloadEvent += PlayReloadStartSE;
        controller.OnEndReloadEvent += PlayReloadFinishSE;

        // �ǰ� ���� ȿ����
        stats.HitEvent += PlayHitSE;
    }

    void PlayClip(string name, Vector3 pos)
    {
        SpreadClip(name, pos);
    }

    public void PlayMonsterAttack(Vector3 pos)
    {
        PlayClip(enemy_attack.name, pos);
    }

    public void PlayMonsterDead(Vector3 pos)
    {
        PlayClip(enemy_hit.name, pos);
    }

    void PlayShotSE()
    {
        if (player_attack == null || player == null)
            return;

        PlayClip(player_attack.name, player.transform.position);
    }

    void PlayRollingSE()
    {
        if (player_rolling == null || player == null)
            return;

        PlayClip(player_rolling.name, player.transform.position); 
    }

    void PlayHitSE()
    {
         if (player_hit == null || player == null)
                return;

       PlayClip(player_hit.name, player.transform.position); 
    }

    void PlayReloadStartSE()
    {
        if (reloadStart == null || player == null)
            return;

        PlayClip(reloadStart.name, player.transform.position);
    }

    void PlayReloadFinishSE()
    {
        if (reloadFinish == null || player == null)
            return;

        PlayClip(reloadFinish.name, player.transform.position);
    }

    public void SpreadClip(string name, Vector3 pos)
    {
        AudioManager.PlaySE(name, pos);
    }
}
