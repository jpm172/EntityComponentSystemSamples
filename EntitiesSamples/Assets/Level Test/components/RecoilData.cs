using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct RecoilData : IComponentData
{
    public RecoilProfile Profile;
    public float TimeSinceShot;
    public float RecoilTimer;
    public float RecoveryTime;

    public float TargetRecoilAngle;
    public float RecoilAngle;

    public float3 RecoilOffset;

    public bool Debug;
}
