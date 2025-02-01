using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [SerializeField]
    private TMP_Dropdown _healingDropdown;
    
    public Sprite[] WoundSprites;

    public GameObject WoundPrefab;

    private int _woundCount;

    private Image[] bodyParts;
    // Start is called before the first frame update
    void Start()
    {
        bodyParts = new[] {_head, _chest, _leftArm, _rightArm, _leftLeg, _rightLeg};
    }

    public void AddWound()
    {
        int bodyPart = Random.Range( 0, bodyParts.Length );
        AddRandomWound( bodyParts[bodyPart] );
        
        _woundCount++;


    }

    public void HealWounds( )
    {
        Debug.Log( _healingDropdown.options[_healingDropdown.value].text );
    }


    private void AddRandomWound( Image bodyPart )
    {
        
        ushort[] tris = bodyPart.sprite.triangles;
        Vector2[] verts = bodyPart.sprite.vertices;
        float ppu = bodyPart.sprite.pixelsPerUnit; 
        
        int randomIndex = Random.Range( 0, tris.Length / 3 );//select a random tri from the body part's mesh

        for ( int i = 0; i <  tris.Length / 3; i++ )
        {
            int index = i * 3;
            Vector3 a = verts[tris[index]]*ppu;
            Vector3 b = verts[tris[index+1]]*ppu;
            Vector3 c = verts[tris[index+2]]*ppu;

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

        Vector2 randomPos = RandomWithinTriangle( verts[tris[randomIndex]], verts[tris[randomIndex + 1]], verts[tris[randomIndex + 2]] );
        
        randomPos *= ppu;
        
        Debug.DrawLine( randomPos, randomPos + (Vector2.up*30), Color.red, 1  );
        Debug.DrawLine( randomPos, randomPos + (Vector2.down*30), Color.red, 1  );
        Debug.DrawLine( randomPos, randomPos + (Vector2.left*30), Color.red, 1  );
        Debug.DrawLine( randomPos, randomPos + (Vector2.right*30), Color.red, 1  );
        
        GameObject newWound = Instantiate( WoundPrefab, bodyPart.transform );
        
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
