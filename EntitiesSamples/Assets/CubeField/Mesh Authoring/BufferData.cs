using System;
using Unity.Entities;
using UnityEngine;

public class BufferData : IComponentData, IDisposable
{
    private ComputeBuffer Buffer;

    public void Dispose()
    {
        Buffer?.Dispose();
    }
}
