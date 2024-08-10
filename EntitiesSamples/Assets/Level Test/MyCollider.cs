using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;

public struct MyCollider : ICollider
{
    public Aabb CalculateAabb()
    {
        throw new System.NotImplementedException();
    }

    public bool CastRay( RaycastInput input )
    {
        throw new System.NotImplementedException();
    }

    public bool CastRay( RaycastInput input, out RaycastHit closestHit )
    {
        throw new System.NotImplementedException();
    }

    public bool CastRay( RaycastInput input, ref NativeList<RaycastHit> allHits )
    {
        throw new System.NotImplementedException();
    }

    public bool CastRay<T>( RaycastInput input, ref T collector ) where T : struct, ICollector<RaycastHit>
    {
        throw new System.NotImplementedException();
    }

    public bool CastCollider( ColliderCastInput input )
    {
        throw new System.NotImplementedException();
    }

    public bool CastCollider( ColliderCastInput input, out ColliderCastHit closestHit )
    {
        throw new System.NotImplementedException();
    }

    public bool CastCollider( ColliderCastInput input, ref NativeList<ColliderCastHit> allHits )
    {
        throw new System.NotImplementedException();
    }

    public bool CastCollider<T>( ColliderCastInput input, ref T collector ) where T : struct, ICollector<ColliderCastHit>
    {
        throw new System.NotImplementedException();
    }

    public bool CalculateDistance( PointDistanceInput input )
    {
        throw new System.NotImplementedException();
    }

    public bool CalculateDistance( PointDistanceInput input, out DistanceHit closestHit )
    {
        throw new System.NotImplementedException();
    }

    public bool CalculateDistance( PointDistanceInput input, ref NativeList<DistanceHit> allHits )
    {
        throw new System.NotImplementedException();
    }

    public bool CalculateDistance<T>( PointDistanceInput input, ref T collector ) where T : struct, ICollector<DistanceHit>
    {
        throw new System.NotImplementedException();
    }

    public bool CalculateDistance( ColliderDistanceInput input )
    {
        throw new System.NotImplementedException();
    }

    public bool CalculateDistance( ColliderDistanceInput input, out DistanceHit closestHit )
    {
        throw new System.NotImplementedException();
    }

    public bool CalculateDistance( ColliderDistanceInput input, ref NativeList<DistanceHit> allHits )
    {
        throw new System.NotImplementedException();
    }

    public bool CalculateDistance<T>( ColliderDistanceInput input, ref T collector ) where T : struct, ICollector<DistanceHit>
    {
        throw new System.NotImplementedException();
    }

    public bool CheckSphere( float3 position, float radius, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool OverlapSphere( float3 position, float radius, ref NativeList<DistanceHit> outHits, CollisionFilter filter,
        QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool OverlapSphereCustom<T>( float3 position, float radius, ref T collector, CollisionFilter filter,
        QueryInteraction queryInteraction = QueryInteraction.Default ) where T : struct, ICollector<DistanceHit>
    {
        throw new System.NotImplementedException();
    }

    public bool CheckCapsule( float3 point1, float3 point2, float radius, CollisionFilter filter,
        QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool OverlapCapsule( float3 point1, float3 point2, float radius, ref NativeList<DistanceHit> outHits, CollisionFilter filter,
        QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool OverlapCapsuleCustom<T>( float3 point1, float3 point2, float radius, ref T collector, CollisionFilter filter,
        QueryInteraction queryInteraction = QueryInteraction.Default ) where T : struct, ICollector<DistanceHit>
    {
        throw new System.NotImplementedException();
    }

    public bool CheckBox( float3 center, quaternion orientation, float3 halfExtents, CollisionFilter filter,
        QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool OverlapBox( float3 center, quaternion orientation, float3 halfExtents, ref NativeList<DistanceHit> outHits,
        CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool OverlapBoxCustom<T>( float3 center, quaternion orientation, float3 halfExtents, ref T collector,
        CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default ) where T : struct, ICollector<DistanceHit>
    {
        throw new System.NotImplementedException();
    }

    public bool SphereCast( float3 origin, float radius, float3 direction, float maxDistance, CollisionFilter filter,
        QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool SphereCast( float3 origin, float radius, float3 direction, float maxDistance, out ColliderCastHit hitInfo,
        CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool SphereCastAll( float3 origin, float radius, float3 direction, float maxDistance, ref NativeList<ColliderCastHit> outHits,
        CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool SphereCastCustom<T>( float3 origin, float radius, float3 direction, float maxDistance, ref T collector,
        CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default ) where T : struct, ICollector<ColliderCastHit>
    {
        throw new System.NotImplementedException();
    }

    public bool BoxCast( float3 center, quaternion orientation, float3 halfExtents, float3 direction, float maxDistance,
        CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool BoxCast( float3 center, quaternion orientation, float3 halfExtents, float3 direction, float maxDistance,
        out ColliderCastHit hitInfo, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool BoxCastAll( float3 center, quaternion orientation, float3 halfExtents, float3 direction, float maxDistance,
        ref NativeList<ColliderCastHit> outHits, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool BoxCastCustom<T>( float3 center, quaternion orientation, float3 halfExtents, float3 direction, float maxDistance,
        ref T collector, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default ) where T : struct, ICollector<ColliderCastHit>
    {
        throw new System.NotImplementedException();
    }

    public bool CapsuleCast( float3 point1, float3 point2, float radius, float3 direction, float maxDistance,
        CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool CapsuleCast( float3 point1, float3 point2, float radius, float3 direction, float maxDistance,
        out ColliderCastHit hitInfo, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool CapsuleCastAll( float3 point1, float3 point2, float radius, float3 direction, float maxDistance,
        ref NativeList<ColliderCastHit> outHits, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default )
    {
        throw new System.NotImplementedException();
    }

    public bool CapsuleCastCustom<T>( float3 point1, float3 point2, float radius, float3 direction, float maxDistance,
        ref T collector, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default ) where T : struct, ICollector<ColliderCastHit>
    {
        throw new System.NotImplementedException();
    }

    public CollisionFilter GetCollisionFilter()
    {
        throw new System.NotImplementedException();
    }

    public void SetCollisionFilter( CollisionFilter filter )
    {
        throw new System.NotImplementedException();
    }

    public ColliderType Type { get; }
    public CollisionType CollisionType { get; }
    public MassProperties MassProperties { get; }
    public int MemorySize { get; }
}
