using System;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;

public class BufferData : IComponentData, IDisposable
{
    //private Matrix4x4 matrix; //use material.SetMatrix and use a list of Matrices instead of a managed buffer?

    
    public int[] PointField;
    public ComputeBuffer Buffer;
    

    public void SetBuffer()
    {
        Buffer.SetData( PointField );
    }
    
    public void Dispose()
    {
        Buffer?.Dispose();
    }

}
