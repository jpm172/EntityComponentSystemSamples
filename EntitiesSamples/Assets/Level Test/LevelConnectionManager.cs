using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelConnectionManager
{
   private int4 _bounds;
   private int2 _thicknessOffset;
   private List<int4> _pieces;

   public List<int4> Pieces => _pieces;

   public int4 Bounds => _bounds;

   public Color DebugColor;
   
   public float Length => GetLength();
   

   public LevelConnectionManager(int4 startingPiece, int4 direction)
   {
      _pieces = new List<int4>();
      _pieces.Add( startingPiece );
      _bounds = startingPiece;
      UpdateOffset( startingPiece, direction );
      DebugColor = new Color(Random.Range( 0,1f ),Random.Range( 0,1f ),Random.Range( 0,1f ), 1);
   }

   private void UpdateOffset( int4 piece, int4 direction )
   {
      int2 size = piece.Size();
      if ( direction.x != 0 || direction.z != 0 )
      {
         _thicknessOffset.x = size.x - 1;
      }
      else
      {
         _thicknessOffset.y = size.y - 1;
      }
   }

   private float GetLength()
   {
      float length = math.distance( _bounds.zw, _bounds.xy );
      return length;
   }

   public int GetLargestDimension()
   {
      int2 size = _bounds.Size() - _thicknessOffset;
      return math.max( size.x, size.y );
   }

   public bool TryMerge( LevelConnectionManager otherConnection )
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

   private void Merge( LevelConnectionManager otherConnection )
   {
      _pieces.AddRange( otherConnection.Pieces );

      int4 otherBounds = otherConnection.Bounds;

      _thicknessOffset = math.max( _thicknessOffset, otherConnection._thicknessOffset );
      _bounds.xy = math.min( _bounds.xy, otherBounds.xy );
      _bounds.zw = math.max( _bounds.zw, otherBounds.zw );
   }
   
}

public struct LevelConnectionInfo
{
   public int4 Bounds;
   public int2 Connections;
   public int4 Direction;

   public LevelConnectionInfo( int room1, int room2, int4 bounds, int4 direction )
   {
      Bounds = bounds;
      Connections = new int2(math.min( room1, room2 ), math.max( room1, room2 ));
      Direction = direction;
   }
}
