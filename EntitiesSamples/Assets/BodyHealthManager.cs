using System.Collections;
using System.Collections.Generic;
using Unity.Physics;
using UnityEngine;
using UnityEngine.UI;

public class BodyHealthManager : MonoBehaviour
{
    [SerializeField]
    private Image _head,
        _chest,
        _leftArm,
        _rightArm,
        _leftLeg,
        _rightLeg;

    public Sprite[] WoundSprites;

    public GameObject WoundPrefab;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void AddWound()
    {
        ushort[] tris = _leftArm.sprite.triangles;
        int randomIndex = Random.Range( 0, tris.Length / 3 );
        /*//
        Vector2 pos = _leftArm.sprite.vertices[randomIndex];
        pos += _leftArm.sprite.vertices[randomIndex+1];
        pos += _leftArm.sprite.vertices[randomIndex+2];
        pos /= 3;
        pos *= 100;
        */
        
        for ( int i = 0; i <  tris.Length / 3; i++ )
        {
            int index = i * 3;
            Vector3 a = _leftArm.sprite.vertices[tris[index]]*100;
            Vector3 b = _leftArm.sprite.vertices[tris[index+1]]*100;
            Vector3 c = _leftArm.sprite.vertices[tris[index+2]]*100;

            if ( randomIndex == i )
            {
                Debug.DrawLine( a,b,Color.blue, 1 );
                Debug.DrawLine( b,c,Color.blue, 1 );
                Debug.DrawLine( c,a,Color.blue, 1 );
            }
            else
            {
                Debug.DrawLine( a,b,Color.black, 1 );
                Debug.DrawLine( b,c,Color.black, 1 );
                Debug.DrawLine( c,a,Color.black, 1 );
            }
            
        }

        randomIndex *= 3;
        
        
        //Vector2 randomPos = RandomWithinTriangle( _leftArm.sprite.vertices[randomIndex], _leftArm.sprite.vertices[randomIndex + 1], _leftArm.sprite.vertices[randomIndex + 2] );
        Vector2 randomPos = RandomWithinTriangle( _leftArm.sprite.vertices[tris[randomIndex]], 
            _leftArm.sprite.vertices[tris[randomIndex + 1]], 
            _leftArm.sprite.vertices[tris[randomIndex + 2]] );
        
        //Debug.Log( $"A: {_leftArm.sprite.vertices[randomIndex]}, B: {_leftArm.sprite.vertices[randomIndex+1]}, C: {_leftArm.sprite.vertices[randomIndex+2]} = {randomPos}" );

        randomPos *= 100;
        
        Debug.DrawLine( randomPos, randomPos + (Vector2.up*30), Color.red, 1  );
        Debug.DrawLine( randomPos, randomPos + (Vector2.down*30), Color.red, 1  );
        Debug.DrawLine( randomPos, randomPos + (Vector2.left*30), Color.red, 1  );
        Debug.DrawLine( randomPos, randomPos + (Vector2.right*30), Color.red, 1  );
        
        GameObject newWound = Instantiate( WoundPrefab, _leftArm.transform );
        //newWound.GetComponent<RectTransform>().localPosition = pos;
        newWound.GetComponent<RectTransform>().localPosition = randomPos;
        newWound.GetComponent<RectTransform>().rotation = Quaternion.Euler( 0,0,Random.Range( -360, 360 ) );
        newWound.GetComponent<Image>().sprite = WoundSprites[Random.Range( 0, WoundSprites.Length )];
    }

    
    private Vector2 RandomWithinTriangle(Vector2 pointA, Vector2 pointB, Vector2 pointC)
    {
        var r1 = Mathf.Sqrt(Random.Range(0f, 1f));
        var r2 = Random.Range(0f, 1f);
        var m1 = 1 - r1;
        var m2 = r1 * (1 - r2);
        var m3 = r2 * r1;

        /*
        var p1 = t.GetVertex(0).ToVector2();
        var p2 = t.GetVertex(1).ToVector2();
        var p3 = t.GetVertex(2).ToVector2();
        */
        
        //return (m1 * p1) + (m2 * p2) + (m3 * p3);
        return (m1 * pointA) + (m2 * pointB) + (m3 * pointC);
    }
    
    private Vector2 GetRandomPointFromTri(Vector2 pointA, Vector2 pointB, Vector2 pointC)
    {
        Vector2 result = new Vector2();

        float r1 = Random.Range( 0, 1 );
        float r2 = Random.Range( 0, 1 );
        if ( r1 + r2 > 1 )
        {
            r1 = ( 1 - r1 );
            r2 = ( 1 - r2 );
        }
        
        //float x = width * r2 + C.x * r1
        //let y = C.y * r1
        
        return result;
    }
    
}
