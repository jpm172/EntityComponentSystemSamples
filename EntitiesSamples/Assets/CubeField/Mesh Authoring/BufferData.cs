using System;
using Unity.Entities;
using UnityEngine;

public class BufferData : IComponentData, IDisposable
{
    //private Matrix4x4 matrix; //use material.SetMatrix and use a list of Matrices instead of a managed buffer?

    
    private int[] PointField;
    private ComputeBuffer Buffer;

    public void Dispose()
    {
        Buffer?.Dispose();
    }
    
}
