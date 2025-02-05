using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Physics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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
    public SubMenu _healingMenu;
    
    public Sprite[] WoundSprites;

    public GameObject WoundPrefab;

    private int _woundCount;

    private Image[] bodyParts;

    [SerializeField]
    private List<Wound> _wounds;
    
    // Start is called before the first frame update
    void Start()
    {
        bodyParts = new[] {_head, _chest, _leftArm, _rightArm, _leftLeg, _rightLeg};
        _wounds = new List<Wound>();
        _healingMenu.OnSelected += HealWounds;
    }

    public void AddWound()
    {
        int bodyPart = Random.Range( 0, bodyParts.Length );
        AddRandomWound( bodyPart );
        
        _woundCount++;
        
    }

    public void HealWounds( int value )
    {
        PlayerUIManager manager = PlayerUIManager.Instance;
        if ( value == 0 )
        {

            foreach ( int key in manager.HealthItemKeys )
            {
                ItemInfo item = manager.AllItems[key];
                
            }
            
            foreach ( Wound w in _wounds )
            {
                Destroy( w.WoundObj );
            }
            _wounds.Clear();
        }
    }
    


    private void AddRandomWound( int partIndex )
    {

        Image bodyPart = bodyParts[partIndex];
        
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
        
        GameObject newWoundObj = Instantiate( WoundPrefab, bodyPart.transform );
        
        newWoundObj.GetComponent<RectTransform>().localPosition = randomPos;
        newWoundObj.GetComponent<RectTransform>().rotation = Quaternion.Euler( 0,0,Random.Range( -360, 360 ) );
        newWoundObj.GetComponent<Image>().sprite = WoundSprites[Random.Range( 0, WoundSprites.Length )];
        
        Wound newWound = new Wound( WoundType.Moderate, (BodyPart)partIndex, newWoundObj );
        _wounds.Add( newWound );
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

[Serializable]
public class Wound
{
    [SerializeField]
    private WoundType _type;
    [SerializeField]
    private BodyPart _affectedPart;

    private int _healingNeeded;
    
    
    private GameObject _woundObj;

    public GameObject WoundObj => _woundObj;

    public Wound( WoundType type, BodyPart part, GameObject woundObj )
    {
        _type = type;
        _affectedPart = part;
        _woundObj = woundObj;

        if ( type == WoundType.Minor )
        {
            _healingNeeded = 1;
        }
        else if ( type == WoundType.Moderate )
        {
            _healingNeeded = 3;
        }
        else if ( type == WoundType.Severe )
        {
            _healingNeeded = 5;
        }
        
        
    }

}

public enum BodyPart: int
{
    Head = 0,
    Chest = 1,
    LeftArm = 2,
    RightArm = 3,
    LeftLeg = 4,
    RightLeg = 5
}

public enum WoundType : int
{
    Minor = 0,
    Moderate = 1,
    Severe = 2,
    Etched = 3
}