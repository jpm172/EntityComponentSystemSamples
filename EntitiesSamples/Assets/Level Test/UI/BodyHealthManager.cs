using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.UI;
using Math = System.Math;
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
    private LimbStatusMeter[] _meters;

    [SerializeField]
    public SubMenu _healingMenu;
    
    public Sprite[] WoundSprites;

    public GameObject WoundPrefab;

    private int _woundCount;

    private Dictionary<BodyPart, Limb> _bodyParts;

    private readonly BodyPart[] _bodyPartLabels =
    {
        BodyPart.Head,
        BodyPart.Chest,
        BodyPart.LeftArm,
        BodyPart.RightArm,
        BodyPart.LeftLeg,
        BodyPart.RightLeg
    };

    [SerializeField]
    private List<Wound> _wounds;
    
    // Start is called before the first frame update
    void Start()
    {
        InitializeLimbs();
        _wounds = new List<Wound>();
        _healingMenu.OnSelected += HealWounds;
    }

    private void InitializeLimbs()
    {
        _bodyParts = new Dictionary<BodyPart, Limb>(6);
        
        _bodyParts.Add( BodyPart.Head, new Limb( BodyPart.Head, _head, _meters[0], 50 ) );
        _bodyParts.Add( BodyPart.Chest, new Limb( BodyPart.Chest, _chest, _meters[1], 50 )  );
        _bodyParts.Add( BodyPart.LeftArm, new Limb( BodyPart.LeftArm, _leftArm, _meters[2], 50 ) );
        _bodyParts.Add( BodyPart.RightArm, new Limb( BodyPart.RightArm, _rightArm, _meters[3], 50 ) );
        _bodyParts.Add( BodyPart.LeftLeg, new Limb( BodyPart.LeftLeg, _leftLeg, _meters[4], 50 ) );
        _bodyParts.Add( BodyPart.RightLeg, new Limb( BodyPart.RightLeg, _rightLeg, _meters[5], 50 ) );
    }

    public void AddWound()
    {
        int bodyPartIndex = Random.Range( 0, _bodyPartLabels.Length );
        bodyPartIndex = 2;
        //AddRandomWound( _bodyPartLabels[bodyPartIndex], Random.Range( 2, 30 ) );
        AddRandomWound( _bodyPartLabels[bodyPartIndex], 10 );
        
        _woundCount++;
        
    }

    public void HealWounds( int value )
    {

        if ( _wounds.Count == 0 )
            return;
        
        PlayerUIManager manager = PlayerUIManager.Instance;
        if ( value == 0 )
        {
            while ( _wounds.Count > 0 && GetBestHealingItem( out HealthItemInfo bestItem ) )
            {
                bool removed = false;
                foreach ( BodyPart part in _bodyPartLabels )
                {
                    HealBodyPart( part, bestItem );
                    if ( ((HealthItemInfo)manager.AllItems[bestItem.Key]).HealthItem.CurrentCharges <= 0 )
                    {
                        removed = true;
                        manager.RemoveItem( bestItem.Key, true );
                        break;
                    }
                }

                if ( !removed )
                    manager.ItemUpdateEvent.Invoke();
            }
        }
    }
    
    

    private bool GetBestHealingItem(out HealthItemInfo bestItem)
    {
        PlayerUIManager manager = PlayerUIManager.Instance;
        bestItem = null;
        
        foreach ( int key in manager.HealthItemKeys )
        {
            HealthItemInfo item = (HealthItemInfo)manager.AllItems[key];
            
            if(item.Data.Stackable)
                continue;
            
            if ( bestItem == null || item.HealthItem.CurrentCharges > bestItem.HealthItem.CurrentCharges )
            {
                bestItem = item;
            }
        }

        if ( bestItem == null )
            return false;
        
        return true;
    }

    public void HealBodyPart( BodyPart bodyPart, HealthItemInfo usedItem )
    {
        PlayerUIManager manager = PlayerUIManager.Instance;
        //Debug.Log( usedItem.HealthItem.CurrentCharges );
        for ( int i = _wounds.Count - 1; i >= 0; i-- )
        {
            Wound w = _wounds[i];
            if ( w.AffectedPart == bodyPart )
            {
                int healAmount = Math.Min( w.HealingNeeded, usedItem.HealthItem.CurrentCharges );
                usedItem.HealthItem.CurrentCharges -= healAmount;
                w.HealingNeeded -= healAmount;

                _bodyParts[w.AffectedPart].Heal( healAmount );
                
                if ( w.HealingNeeded <= 0 )
                {
                    Destroy( w.WoundObj );
                    _wounds.RemoveAt( i );
                }
                
                if(usedItem.HealthItem.CurrentCharges <= 0)
                    break;
            }
        }

        manager.AllItems[usedItem.Key] = usedItem;
        
        //Debug.Log( ((HealthItemInfo)manager.AllItems[usedItem.Key] ).HealthItem.CurrentCharges);
        
        
    }

    private void AddRandomWound( BodyPart woundedBodyPart, int damage )
    {

        Image bodyPart = _bodyParts[woundedBodyPart].Image;
        
        ushort[] tris = bodyPart.sprite.triangles;
        Vector2[] verts = bodyPart.sprite.vertices;
        float ppu = bodyPart.sprite.pixelsPerUnit; 
        
        int randomIndex = Random.Range( 0, tris.Length / 3 ) * 3;//select a random tri from the body part's mesh

        Vector2 randomPos = RandomWithinTriangle( verts[tris[randomIndex]], verts[tris[randomIndex + 1]], verts[tris[randomIndex + 2]] );
        randomPos *= ppu;

        GameObject newWoundObj = Instantiate( WoundPrefab, bodyPart.transform );
        
        newWoundObj.GetComponent<RectTransform>().localPosition = randomPos;
        newWoundObj.GetComponent<RectTransform>().rotation = Quaternion.Euler( 0,0,Random.Range( -360, 360 ) );
        newWoundObj.GetComponent<Image>().sprite = WoundSprites[Random.Range( 0, WoundSprites.Length )];

        if ( _bodyParts[woundedBodyPart].CurrentHealth >= damage )
        {
            Wound newWound = new Wound( damage, woundedBodyPart, newWoundObj );
            _bodyParts[woundedBodyPart].Damage( newWound.HealingNeeded );
            _wounds.Add( newWound );
        }


    }

    private void AddRandomWoundDebug( BodyPart woundedBodyPart, int damage )
    {

        Image bodyPart = _bodyParts[woundedBodyPart].Image;
        
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
        
        
        Wound newWound = new Wound( WoundType.Severe, woundedBodyPart, newWoundObj );
        _bodyParts[woundedBodyPart].Damage( newWound.HealingNeeded );
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

    public BodyPart AffectedPart => _affectedPart;

    public int HealingNeeded
    {
        get => _healingNeeded;
        set => _healingNeeded = value;
    }

    
    public Wound( int damage, BodyPart part, GameObject woundObj )
    {
        
        _affectedPart = part;
        _woundObj = woundObj;
        _healingNeeded = damage;

        if ( damage <= 5 )
        {
            _type = WoundType.Minor;
        }
        else if ( damage <= 15 )
        {
            _type = WoundType.Moderate;
        }
        else
        {
            _type = WoundType.Severe;
        }
        
    }
    
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


public class Limb
{
    private BodyPart _bodyPart;
    private Image _image;
    private int _maxHealth;
    private int _currentHealth;
    private LimbStatusMeter _meter;

    public Image Image => _image;

    public BodyPart BodyPart => _bodyPart;

    public int CurrentHealth => _currentHealth;

    public int MaxHealth => _maxHealth;

    
    
    public Limb( BodyPart bodyPart, Image img, LimbStatusMeter meter, int maxHealth )
    {
        _bodyPart = bodyPart;
        _image = img;
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;
        _meter = meter;
    }

    public void Damage( int damage )
    {
        _currentHealth = Math.Max( 0, _currentHealth - damage );
        _image.color = Color.Lerp( Color.black, Color.white, (float)_currentHealth/_maxHealth );   
        _meter.UpdateStatus( this );
    }

    public void Heal( int amount )
    {
        _currentHealth = Math.Min( _maxHealth, _currentHealth + amount );
        _image.color = Color.Lerp( Color.black, Color.white, (float)_currentHealth/_maxHealth );
        _meter.UpdateStatus( this );
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