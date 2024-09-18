using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelConnection
{
   private int4 _bounds;
   private List<int4> _pieces;

   public List<int4> Pieces => _pieces;

   public int4 Bounds => _bounds;

   public Color DebugColor;
   
   public float Length => GetLength();
   

   public LevelConnection(int4 startingPiece)
   {
      _pieces = new List<int4>();
      _pieces.Add( startingPiece );
      _bounds = startingPiece;
      
      DebugColor = new Color(Random.Range( 0,1f ),Random.Range( 0,1f ),Random.Range( 0,1f ), 1);
   }

   private float GetLength()
   {
      float length = math.distance( _bounds.zw, _bounds.xy );
      return length;
   }

   public int GetLargestDimension()
   {
      int2 size = _bounds.Size();
      return math.max( size.x, size.y );
   }

   public bool TryMerge( LevelConnection otherConnection )
   {
      bool broadphase = _bounds.Borders( otherConnection.Bounds ) || _bounds.Overlaps( otherConnection.Bounds );
      
      if ( !broadphase )
         return false;
      
      List<int4> otherPieces = otherConnection.Pieces;
      foreach ( int4 piece in _pieces )
      {
         foreach ( int4 otherPiece in otherPieces )
         {
            if ( piece.Borders( otherPiece ) || piece.Overlaps( otherPiece ) )
            {
               Merge( otherConnection );
               return true;
            }
         }
      }

      return false;
   }

   private void Merge( LevelConnection otherConnection )
   {
      _pieces.AddRange( otherConnection.Pieces );
      

      int4 otherBounds = otherConnection.Bounds;
      
      _bounds.xy = math.min( _bounds.xy, otherBounds.xy );
      _bounds.zw = math.max( _bounds.zw, otherBounds.zw );
   }
   
}

public struct LevelConnectionInfo
{
   public int4 Bounds;
   public int2 Connections;

   public LevelConnectionInfo( int room1, int room2, int4 bounds )
   {
      Bounds = bounds;
      Connections = new int2(math.min( room1, room2 ), math.max( room1, room2 ));
   }
}
