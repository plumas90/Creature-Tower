using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats
{
    private float _added = 0f;

    public float added
    {
        get => _added;
        set
        {
            float diff = value - _added;
            _added += diff * scale;
        }
    }

    public virtual float total
    {
        get
        {
            if ((basic + _added) * coefficient <= 0) { return 0.1f; }
            else { return (basic + _added) * coefficient; }
        }
    }

    public float basic { get; private set; }
    public float coefficient { get; set; } = 1;
    public float scale { get; set; } = 1.0f; // 캐릭터별 스탯 계수

    public Stats(float basic, float scale = 1.0f)
    {
        this.basic = basic;
        this.scale = scale;
    }
}

public class AtkSpeedStats : Stats
{
    public AtkSpeedStats(float basic, float scale = 1.0f) : base(basic, scale) { }

    public override float total
    {
        get
        {
            float calculated = basic * (1f + added) * coefficient;
            return calculated <= 0f ? 0.1f : calculated;
        }
    }
}
