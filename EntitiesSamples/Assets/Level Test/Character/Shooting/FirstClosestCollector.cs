using System.Collections;
using System.Collections.Generic;
using Unity.Physics;


public struct FirstClosestCollector : ICollector<RaycastHit>
{
    public bool AddHit( RaycastHit hit )
    {
        throw new System.NotImplementedException();
    }

    public bool EarlyOutOnFirstHit { get; }
    public float MaxFraction { get; }
    public int NumHits { get; }
}
