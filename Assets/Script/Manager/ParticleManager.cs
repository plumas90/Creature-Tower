using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ParticleType
{
    NONE,
    NORMAL,
    SPECIAL
}

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [SerializeField] private List<GameObject> prefabs;
    [SerializeField] private Dictionary<string, GameObject> prefabDict;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Initialize();
    }

    private void Initialize()
    {
        prefabDict = new Dictionary<string, GameObject>();

        if (prefabs == null)
            return;

        foreach (var prefab in prefabs)
        {
            if (prefab == null)
                continue;

            if (!prefabDict.ContainsKey(prefab.name))
                prefabDict.Add(prefab.name, prefab);
        }
    }

    private static bool EnsureReady()
    {
        if (Instance == null)
            return false;

        if (Instance.prefabDict == null)
            Instance.Initialize();

        return Instance.prefabDict != null;
    }

    public static void PlayEffectLocal(string name, Vector3 pos, Transform parents)
    {
        if (!EnsureReady())
            return;

        if (!CheckContainKey(name))
            return;

        var tempDict = Instance.prefabDict;
        GameObject prefab = Instantiate(tempDict[name], parents);
        prefab.transform.position = pos;

        ParticleSystem ps = prefab.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Play();
    }

    public void PlayEffect(string name, Vector3 pos, GameObject parent = null)
    {
        if (!EnsureReady())
            return;

        if (!CheckContainKey(name))
            return;

        Transform parentTransform = parent != null ? parent.transform : null;
        SpawnEffect(name, pos, parentTransform);
    }

    public void PlayEffect(string name, Vector3 pos)
    {
        if (!EnsureReady())
            return;

        if (!CheckContainKey(name))
            return;

        SpawnEffect(name, pos, null);
    }

    private void SpawnEffect(string name, Vector3 pos, Transform parent)
    {
        if (!EnsureReady())
            return;

        var tempDict = Instance.prefabDict;
        if (!tempDict.ContainsKey(name))
            return;

        GameObject prefab = parent != null
            ? Instantiate(tempDict[name], pos, Quaternion.identity, parent)
            : Instantiate(tempDict[name], pos, Quaternion.identity);

        ParticleSystem ps = prefab.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Play();
    }

    private static bool CheckContainKey(string name)
    {
        if (!EnsureReady())
            return false;

        var tempDict = Instance.prefabDict;
        if (!tempDict.ContainsKey(name))
        {
            bool result = TryCachingClip(name);
            if (!result)
                return false;
        }

        return true;
    }

    private static bool TryCachingClip(string name)
    {
        if (!EnsureReady())
            return false;

        var clipDict = Instance.prefabDict;
        if (!clipDict.ContainsKey(name))
        {
            var clip = Resources.Load<GameObject>("Prefabs/Particle/" + name);
            if (clip == null)
            {
                Debug.LogError("Can't find " + name);
                return false;
            }

            clipDict.Add(clip.name, clip);
        }

        return true;
    }
}
