using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BT 노드 간 데이터를 공유하기 위한 Blackboard 시스템.
/// Key-Value 방식으로 데이터를 저장하고, 여러 Task가 읽고 쓸 수 있다.
/// </summary>
public class BossBTBlackboard
{
    private readonly Dictionary<string, object> data = new Dictionary<string, object>();

    /// <summary>
    /// 값을 저장한다. 이미 존재하는 키면 덮어쓴다.
    /// </summary>
    public void Set<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[BossBTBlackboard] Set called with null or empty key");
            return;
        }

        data[key] = value;
    }

    /// <summary>
    /// 값을 가져온다. 키가 없으면 default(T)를 반환한다.
    /// </summary>
    public T Get<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[BossBTBlackboard] Get called with null or empty key");
            return default(T);
        }

        if (data.TryGetValue(key, out object value))
        {
            if (value is T typedValue)
                return typedValue;

            // 타입이 맞지 않으면 경고
            Debug.LogWarning($"[BossBTBlackboard] Type mismatch for key '{key}': expected {typeof(T).Name}, got {value.GetType().Name}");
            return default(T);
        }

        return default(T);
    }

    /// <summary>
    /// 값을 안전하게 가져온다. 성공하면 true, 실패하면 false를 반환한다.
    /// </summary>
    public bool TryGet<T>(string key, out T value)
    {
        if (string.IsNullOrEmpty(key))
        {
            value = default(T);
            return false;
        }

        if (data.TryGetValue(key, out object obj))
        {
            if (obj is T typedValue)
            {
                value = typedValue;
                return true;
            }

            // 타입이 맞지 않으면 실패
            Debug.LogWarning($"[BossBTBlackboard] Type mismatch for key '{key}': expected {typeof(T).Name}, got {obj.GetType().Name}");
        }

        value = default(T);
        return false;
    }

    /// <summary>
    /// 키가 존재하는지 확인한다.
    /// </summary>
    public bool Has(string key)
    {
        return !string.IsNullOrEmpty(key) && data.ContainsKey(key);
    }

    /// <summary>
    /// 특정 키를 제거한다.
    /// </summary>
    public bool Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return data.Remove(key);
    }

    /// <summary>
    /// 모든 데이터를 제거한다.
    /// </summary>
    public void Clear()
    {
        data.Clear();
    }

    /// <summary>
    /// 저장된 키의 개수를 반환한다.
    /// </summary>
    public int Count => data.Count;

    /// <summary>
    /// 모든 키를 반환한다.
    /// </summary>
    public IEnumerable<string> GetKeys()
    {
        return data.Keys;
    }

    /// <summary>
    /// 디버그용: 현재 저장된 모든 데이터를 문자열로 반환한다.
    /// </summary>
    public string ToDebugString()
    {
        if (data.Count == 0)
            return "[BossBTBlackboard] Empty";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[BossBTBlackboard] Contents:");
        foreach (var kvp in data)
        {
            string valueStr = kvp.Value != null ? kvp.Value.ToString() : "null";
            string typeStr = kvp.Value != null ? kvp.Value.GetType().Name : "null";
            sb.AppendLine($"  {kvp.Key} = {valueStr} ({typeStr})");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 값을 가져오되, 없으면 기본값을 저장하고 반환한다.
    /// </summary>
    public T GetOrCreate<T>(string key, T defaultValue)
    {
        if (TryGet<T>(key, out T value))
            return value;

        Set(key, defaultValue);
        return defaultValue;
    }

    /// <summary>
    /// 값을 가져오되, 없으면 factory 함수를 실행해서 값을 생성하고 저장한 뒤 반환한다.
    /// </summary>
    public T GetOrCreate<T>(string key, Func<T> factory)
    {
        if (TryGet<T>(key, out T value))
            return value;

        T newValue = factory != null ? factory() : default(T);
        Set(key, newValue);
        return newValue;
    }
}
