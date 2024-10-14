using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DebugClass : MonoBehaviour
{
    public static DebugClass instance;

    public float3 Forward;
    public float3 Up;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }
    }
}
