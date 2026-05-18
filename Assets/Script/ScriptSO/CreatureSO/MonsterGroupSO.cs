using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterSpawnData
{
    public GameObject monsterPrefab;
    public Vector2 spawnOffset; // 소환될 상대 위치 오프셋
}

[System.Serializable]
public class MonsterWaveInfo
{
    public string waveName = "Wave";
    public List<MonsterSpawnData> spawnList = new List<MonsterSpawnData>();
    public float delayBeforeWave = 1f; // 웨이브 시작 전 대기 시간
}

[CreateAssetMenu(fileName = "MonsterGroup", menuName = "ScriptableObject/MonsterGroup", order = int.MinValue)]
public class MonsterGroupSO : ScriptableObject
{
    public string groupName;
    public int targetFloorMin = 1;  // 등장 가능한 최소 층
    public int targetFloorMax = 15; // 등장 가능한 최대 층
    public List<MonsterWaveInfo> waves = new List<MonsterWaveInfo>();
}
