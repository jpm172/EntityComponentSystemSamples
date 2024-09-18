using Unity.Mathematics;
using static MyExtensionMethods;

public struct LevelWall
{
    public int4 Bounds;
    public int Thickness;
    public int WallId;

    public int2 Origin => Bounds.Origin();
    public int2 Size => Bounds.Size();

    public LevelWall( int id, int2 origin, int2 size, int thickness  )
    {
        Bounds = new int4(origin, origin + size - Int2One);
        Thickness = thickness;
        WallId = id;
    }
    
    public override bool Equals( object obj )
    {
        if ( obj == null )
            return false;

        LevelWall wall = (LevelWall) obj;
        return wall.WallId == WallId;
    }
}
