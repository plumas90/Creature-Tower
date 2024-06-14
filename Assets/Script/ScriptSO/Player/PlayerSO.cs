using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public enum ClassName 
{
    TV,
    Charlie,
    KimKilWhan

}

[CreateAssetMenu(fileName = "PlayerSO", menuName = "ScriptableObject/PlayerSO", order = int.MinValue)]
public class PlayerSO : RangedUnitSO
{
    [Header("PlayerSO")]
    public string CharaterName;
    public float reloadCoolTime;         // 장전 쿨타입
    public int skillCoolTime;            // 스킬 쿨타입
    public int rollCoolTime;             // 구르기 쿨타입
    public int ammoMax;                  // 장탄수
    public int CharacterClass;           // 직업
    public int critical;                    // 크리티컬
    public SpriteLibraryAsset playerSprite;// 플레이어 스프라이트
    public SpriteLibraryAsset weaponSprite;// 무기 스프라이트
    //public Sprite indicatorSprite; //ui 총알 이미지
    public AudioClip atkClip;
    public AudioClip[] reloadClip;
}

enum characterClass
{
    rifle,       // 라이플
    shotgun,     // 샷건
    sniper       // 스나이퍼
}