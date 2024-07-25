using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

public readonly partial struct CubeAspect :IAspect
{
    public readonly Entity Entity;
    
    private readonly RefRW<LocalTransform> _transform;
    private readonly RefRO<CubeData> _cubeData;
}
