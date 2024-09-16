using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelConnection
{
   private IntBounds _bounds;
   private List<IntBounds> _pieces;

   public List<IntBounds> Pieces => _pieces;

   public IntBounds Bounds => _bounds;

   public Color DebugColor;
   
   public float Length => GetLength();
   

   public LevelConnection(IntBounds startingPiece)
   {
      _pieces = new List<IntBounds>();
      _pieces.Add( startingPiece );
      _bounds = startingPiece;
      
      DebugColor = new Color(Random.Range( 0,1f ),Random.Range( 0,1f ),Random.Range( 0,1f ), 1);
   }

   private float GetLength()
   {
      float length = math.distance( _bounds.Bounds.zw, _bounds.Bounds.xy );
      return length;
   }

   public int GetLargestDimension()
   {
      int2 size = _bounds.Size;
      return math.max( size.x, size.y );
   }

   public bool TryMerge( LevelConnection otherConnection )
   {
      bool broadphase = _bounds.Borders( otherConnection.Bounds ) || _bounds.Overlaps( otherConnection.Bounds );
      
      if ( !broadphase )
         return false;
      
      List<IntBounds> otherPieces = otherConnection.Pieces;
      foreach ( IntBounds piece in _pieces )
      {
         foreach ( IntBounds otherPiece in otherPieces )
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
      
      int4 bounds = _bounds.Bounds;
      int4 otherBounds = otherConnection.Bounds.Bounds;
      
      _bounds.Bounds.xy = math.min( bounds.xy, otherBounds.xy );
      _bounds.Bounds.zw = math.max( bounds.zw, otherBounds.zw );
   }
   
}

public struct LevelConnectionInfo
{
   public IntBounds Bounds;
   public int2 Connections;

   public LevelConnectionInfo( int room1, int room2, IntBounds bounds )
   {
      Bounds = bounds;
      Connections = new int2(math.min( room1, room2 ), math.max( room1, room2 ));
   }
}
