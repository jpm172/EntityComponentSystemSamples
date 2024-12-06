using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;

[InternalBufferCapacity(100)]
public struct ColliderBufferElement : IBufferElementData
{
    public BlobAssetReference<Collider> Value;
}
