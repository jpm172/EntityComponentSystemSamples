using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.UI;
using Math = System.Math;
using Random = UnityEngine.Random;

public class BodyHealthManager : MonoBehaviour
{

    private PlayerUIManager _manager;

    private Entity _playerEntity;
    private EntityManager _entityManager;
    
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
    public StaticSubMenu _healingMenu;
    
    public Sprite[] WoundSprites;

    public GameObject WoundPrefab;
    public GameObject StatusEffectPrefab;

    [SerializeField]
    private Transform _genericStatusEffectsContainer;

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

    public Dictionary<BodyPart, Limb> BodyParts => _bodyParts;

    [SerializeField]
    private List<GameObject> _wounds;

    [SerializeField]
    private List<GameObject> _statusEffects;

    public Limb[] SerializedLimbs;
    
    //DEBUFF COLOR - FF848E
    //BUFF COLOR - 85FF84
    
    void Start()
    {
        _manager = PlayerUIManager.Instance;
        InitializeLimbs();
        SerializedLimbs = _bodyParts.Values.ToArray();
        _wounds = new List<GameObject>();
        _healingMenu.OnSelected += HealWounds;
        
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityManager.CreateEntityQuery( typeof( PlayerInputs ) )
            .TryGetSingletonEntity<Entity>(out _playerEntity);

    }

    private void FixedUpdate()
    {
        if ( _playerEntity == Entity.Null )
            return;

        DynamicBuffer<CharacterLimb> limbs = _entityManager.GetBuffer<CharacterLimb>( _playerEntity );
        MyCharacterComponent character = _entityManager.GetComponentData<MyCharacterComponent>( _playerEntity );
        float bleedRate = 0;
        foreach ( CharacterLimb limb in limbs )
        {
            bleedRate += limb.Bleed;
            _meters[(int)limb.Part].UpdateStatus( limb );
        }

        _manager.PlayerBleedRate = bleedRate;
        _manager.PlayerCurrentHealth = character.Health;


    }

    /*
    private void FixedUpdate()
    {
        return;
        foreach ( Limb limb in _bodyParts.Values )
        {
            limb.CumulativeDamage += limb.Bleed * Time.fixedDeltaTime;
            if ( limb.CumulativeDamage >= 1 )
            {
                int damage = (int)limb.CumulativeDamage;
                if ( limb.Destroyed )
                {
                    _bodyParts[BodyPart.Chest].Damage( damage );
                }
                else
                {
                    limb.Damage( damage );
                }
                
                limb.CumulativeDamage -= damage;
            }
        }
    }*/

    private void InitializeLimbs()
    {
        _bodyParts = new Dictionary<BodyPart, Limb>(6);
        
        _bodyParts.Add( BodyPart.Head, new Limb( BodyPart.Head, _head, _meters[0], this, 50 ) );
        _bodyParts.Add( BodyPart.Chest, new Chest( BodyPart.Chest, _chest, _meters[1], this, 300 )  );
        _bodyParts.Add( BodyPart.LeftArm, new Limb( BodyPart.LeftArm, _leftArm, _meters[2], this, 50 ) );
        _bodyParts.Add( BodyPart.RightArm, new Limb( BodyPart.RightArm, _rightArm, _meters[3], this, 50 ) );
        _bodyParts.Add( BodyPart.LeftLeg, new Limb( BodyPart.LeftLeg, _leftLeg, _meters[4], this, 50 ) );
        _bodyParts.Add( BodyPart.RightLeg, new Limb( BodyPart.RightLeg, _rightLeg, _meters[5], this, 50 ) );
    }



    public void AddStatusEffect(Entity effectEntity)
    {
        StatusEffectInfo info = _entityManager.GetComponentData<StatusEffectInfo>( effectEntity );
        GameObject newEffect = InstantiateStatusEffect( effectEntity, info );

        StatusEffectLayout effectLayout = newEffect.GetComponent<StatusEffectLayout>();
        effectLayout.Initialize( info );
        
        _statusEffects.Add( newEffect );
    }
    
    public void AddWoundECS(CharacterWound newWound)
    {
        GameObject newWoundObj = InstantiateWound( newWound.AffectedPart );
        _wounds.Add( newWoundObj );
    }
    
    public void AddWoundECS()
    {
        float damage = 10f;
        float bleed = Random.Range( 0f, 12f );
        bleed = 0.5f;
        
        
        _entityManager.GetBuffer<DamageInfo>( _playerEntity ).Add( new DamageInfo( damage, bleed ) );
    }

    public void RemoveWoundECS( int index )
    {
        Destroy( _wounds[index] );
        _wounds.RemoveAt( index );
    }

    public void RemoveStatusEffectECS( int index )
    {
        Destroy( _statusEffects[index] );
        _statusEffects.RemoveAt( index );
    }

    public void HealWounds( int value )
    {
        if ( value == 0 )//heal all
        {
            HealBodyECS();
        }
        else if ( value == 1 )
        {
            HealBody(WoundType.Severe);
        }
        else if ( value == 2 )
        {
            HealBody(WoundType.Moderate);
        }
        else if ( value == 3 )
        {
            HealBody(WoundType.Minor);
        }
        
        
        
        _manager.PlayerBleedRate = GetTotalBleedRate();
    }

    
    private void HealBodyECS()
    {
        bool hasItem = GetBestHealingItem( out HealthItemInfo bestItem );

        if ( !hasItem )
            return;
        
        MyCharacterComponent character = _entityManager.GetComponentData<MyCharacterComponent>( _playerEntity );
        DynamicBuffer<CharacterWound> wounds = _entityManager.GetBuffer<CharacterWound>( _playerEntity );
        DynamicBuffer<CharacterLimb> body = _entityManager.GetBuffer<CharacterLimb>( _playerEntity );

        while ( wounds.Length> 0 && hasItem )
        {
            for ( int i = 0; i < wounds.Length; i++ )
            {
                ref CharacterWound wound = ref wounds.ElementAt( i );
                float2 healResult = wound.Heal(ref bestItem.HealthItem);
            
                ref CharacterLimb limb = ref body.ElementAt( (int) wound.AffectedPart );
                character.Health += limb.Heal( healResult.x, healResult.y );
            
                if ( wound.Healed )
                {
                    wounds.RemoveAt( i );
                    RemoveWoundECS( i );
                }

                if ( bestItem.HealthItem.CurrentCharges <= 0 )
                {
                    _manager.RemoveItem( bestItem.Key, true );
                    hasItem = GetBestHealingItem( out bestItem ); 
                    continue;
                }

                _manager.ItemUpdateEvent.Invoke();
            }
        }
        
        _entityManager.SetComponentData( _playerEntity, character );
    }
    
    private void HealBody()
    {
        bool hasItem = GetBestHealingItem( out HealthItemInfo bestItem );
            
        foreach ( BodyPart part in _bodyPartLabels )
        {
            if ( _bodyParts[part].Wounds.Count == 0 && _bodyParts[part].Healthy )
                continue;
                
                
            while ( _bodyParts[part].Wounds.Count > 0 && hasItem )
            {
                HealBodyPart( part, bestItem );
                if ( ((HealthItemInfo)_manager.AllItems[bestItem.Key]).HealthItem.CurrentCharges <= 0 )
                {
                    _manager.RemoveItem( bestItem.Key, true );
                    hasItem = GetBestHealingItem( out bestItem ); 
                    continue;
                }
                    
                    
                _manager.ItemUpdateEvent.Invoke();
            }

        }
    }

    private void HealBody( WoundType targetWound )
    {
        bool hasItem = GetBestHealingItem( out HealthItemInfo bestItem );
            
        foreach ( BodyPart part in _bodyPartLabels )
        {
            if ( _bodyParts[part].Wounds.Count == 0 && _bodyParts[part].Healthy )
                continue;
                
                
            while ( _bodyParts[part].Wounds.Count > 0 && hasItem && HealBodyPart( part, targetWound, bestItem ) )
            {
                if ( ((HealthItemInfo)_manager.AllItems[bestItem.Key]).HealthItem.CurrentCharges <= 0 )
                {
                    _manager.RemoveItem( bestItem.Key, true );
                    hasItem = GetBestHealingItem( out bestItem ); 
                    continue;
                }
                    
                    
                _manager.ItemUpdateEvent.Invoke();
            }

        }
    }


    public void HealBodyPartECS( BodyPart bodyPart, HealthItemInfo usedItem )
    {
        /*
        if ( usedItem.Data.Stackable )
        {
            //UseSpecialHealingItem( bodyPart, usedItem );
            return;
        }
        */

        _manager.QuickUseItem(usedItem, bodyPart);
        return;
        
        MyCharacterComponent character = _entityManager.GetComponentData<MyCharacterComponent>( _playerEntity );
        DynamicBuffer<CharacterWound> wounds = _entityManager.GetBuffer<CharacterWound>( _playerEntity );
        DynamicBuffer<CharacterLimb> body = _entityManager.GetBuffer<CharacterLimb>( _playerEntity );
        
        for ( int i = wounds.Length - 1; i >= 0; i-- )
        {
            ref CharacterWound wound = ref wounds.ElementAt( i );
            if ( wound.AffectedPart == bodyPart )
            {
                float2 healResult = wound.Heal(ref usedItem.HealthItem);
                ref CharacterLimb limb = ref body.ElementAt( (int) wound.AffectedPart );
                character.Health += limb.Heal( healResult.x, healResult.y );
                
                if ( wound.Healed )
                {
                    wounds.RemoveAt( i );
                    RemoveWoundECS( i );
                }
            }
            _entityManager.SetComponentData( _playerEntity, character );
        }
        _manager.AllItems[usedItem.Key] = usedItem;
        
    }

    public void HealBodyPart( BodyPart bodyPart, HealthItemInfo usedItem )
    {
        if ( usedItem.Data.Stackable )
        {
            UseSpecialHealingItem( bodyPart, usedItem );
            return;
        }
        
        Limb limb = _bodyParts[bodyPart];
        for ( int i = limb.Wounds.Count - 1; i >= 0; i-- )
        {
            Wound w = limb.Wounds[i];
            
            int healAmount = Math.Min( w.HealingNeeded, usedItem.HealthItem.CurrentCharges );
            usedItem.HealthItem.CurrentCharges -= healAmount;
            w.HealingNeeded -= healAmount;

            float newBleed = w.MaxBleed * w.HealProgress;
            float bleedingHealed = w.Bleed - newBleed;
            w.Bleed = newBleed;

            _bodyParts[bodyPart].Heal( healAmount, bleedingHealed );
            
            if ( w.HealingNeeded <= 0 )
            {
                Destroy( w.WoundObj );
                limb.Wounds.RemoveAt( i );
            }
            
            if(usedItem.HealthItem.CurrentCharges <= 0)
                break;
        }

        if ( limb.Wounds.Count == 0 && !limb.Healthy )
        {
            int healAmount =  (int)Math.Min( limb.MissingHealth, usedItem.HealthItem.CurrentCharges );
            usedItem.HealthItem.CurrentCharges -= healAmount;
            limb.Heal( healAmount );
        }
        
        _manager.PlayerBleedRate = GetTotalBleedRate();
        _manager.AllItems[usedItem.Key] = usedItem;
    }
    
    public bool HealBodyPart( BodyPart bodyPart, WoundType targetWound, HealthItemInfo usedItem )
    {
        Limb limb = _bodyParts[bodyPart];
        bool healed = false;
        
        for ( int i = limb.Wounds.Count - 1; i >= 0; i-- )
        {
            Wound w = limb.Wounds[i];
            
            if(w.Type != targetWound)
                continue;
            healed = true;

            int healAmount = Math.Min( w.HealingNeeded, usedItem.HealthItem.CurrentCharges );
            usedItem.HealthItem.CurrentCharges -= healAmount;
            w.HealingNeeded -= healAmount;

            float newBleed = w.MaxBleed * w.HealProgress;
            float bleedingHealed = w.Bleed - newBleed;
            w.Bleed = newBleed;

            _bodyParts[bodyPart].Heal( healAmount, bleedingHealed );
            
            if ( w.HealingNeeded <= 0 )
            {
                Destroy( w.WoundObj );
                limb.Wounds.RemoveAt( i );
            }
            
            if(usedItem.HealthItem.CurrentCharges <= 0)
                break;
        }

        if ( limb.Wounds.Count == 0 && !limb.Healthy )
        {
            int healAmount =  (int)Math.Min( limb.MissingHealth, usedItem.HealthItem.CurrentCharges );
            usedItem.HealthItem.CurrentCharges -= healAmount;
            limb.Heal( healAmount );
        }
        


        _manager.PlayerBleedRate = GetTotalBleedRate();
        _manager.AllItems[usedItem.Key] = usedItem;

        return healed;
    }
    

    private void UseSpecialHealingItem(BodyPart bodyPart, HealthItemInfo usedItem)
    {
        Limb limb = _bodyParts[bodyPart];
        if ( usedItem.Data.ItemName.Equals( "Tourniquet" )  )
        {
            if ( bodyPart == BodyPart.Chest || bodyPart == BodyPart.Head )
                return;
            
            foreach ( Wound wound in limb.Wounds )
            {
                wound.Bleed = 0;
            }
            limb.HealBleed( limb.Bleed );

            usedItem.HealthItem.CurrentCharges--;

            if ( usedItem.HealthItem.CurrentCharges <= 0 )
            {
                usedItem.Quantity--;
                usedItem.HealthItem.CurrentCharges = usedItem.HealthItem.MaxCharges;
            }
                
            _manager.PlayerBleedRate = GetTotalBleedRate();
            _manager.AllItems[usedItem.Key] = usedItem;
        }
    }
    
    private bool GetBestHealingItem(out HealthItemInfo bestItem)
    {
        bestItem = null;
        
        foreach ( int key in _manager.HealthItemKeys )
        {
            HealthItemInfo item = (HealthItemInfo)_manager.AllItems[key];
            
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


    private float GetTotalBleedRate()
    {
        float result = 0;
        foreach ( Limb limb in _bodyParts.Values )
        {
            result += limb.Bleed;
        }

        return result;
    }

    private GameObject InstantiateWound(BodyPart bodyPart)
    {
        Image bodyPartImg = _bodyParts[bodyPart].Image;

        Sprite bodyPartSprite = bodyPartImg.sprite;
        
        ushort[] tris = bodyPartSprite.triangles;
        Vector2[] verts = bodyPartSprite.vertices;
        float ppu = bodyPartSprite.pixelsPerUnit; 
        
        int randomIndex = Random.Range( 0, tris.Length / 3 ) * 3;//select a random tri from the body part's mesh

        Vector2 randomPos = RandomWithinTriangle( verts[tris[randomIndex]], verts[tris[randomIndex + 1]], verts[tris[randomIndex + 2]] );
        randomPos *= ppu;

        GameObject newWoundObj = Instantiate( WoundPrefab, bodyPartImg.transform );
        
        newWoundObj.GetComponent<RectTransform>().localPosition = randomPos;
        newWoundObj.GetComponent<RectTransform>().rotation = Quaternion.Euler( 0,0,Random.Range( -360, 360 ) );
        newWoundObj.GetComponent<Image>().sprite = WoundSprites[Random.Range( 0, WoundSprites.Length )];

        return newWoundObj;
    }

    private GameObject InstantiateStatusEffect(Entity effectEntity, StatusEffectInfo info)
    {
        GameObject newEffectObj = Instantiate( StatusEffectPrefab );
        
        if ( info.Type == StatusEffectType.BasicStats )
        {
            newEffectObj.transform.SetParent( _genericStatusEffectsContainer, false );
        }
        else if(info.Type == StatusEffectType.BodyStats)
        {
            //BodyStatusEffect bodyEffect = _entityManager.GetComponentData<BodyStatusEffect>( effectEntity );//
        }
        
        return newEffectObj;
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
        _bodyParts[woundedBodyPart].Wounds.Add( newWound );
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
    
}

[Serializable]
public class Wound
{
    [SerializeField]
    private WoundType _type;
    [SerializeField]
    private BodyPart _affectedPart;

    private int _maxHealing;
    private int _healingNeeded;
    private float _maxBleed;
    private float _bleed;
    
    
    private GameObject _woundObj;

    public GameObject WoundObj => _woundObj;

    public BodyPart AffectedPart => _affectedPart;

    public WoundType Type => GetWoundType();
    
    public int HealingNeeded
    {
        get => _healingNeeded;
        set => _healingNeeded = value;
    }

    public int MaxHealing => _maxHealing;

    public float Bleed
    {
        get => _bleed;
        set => _bleed = math.max(0, value);
    }

    public float MaxBleed => _maxBleed;

    public float HealProgress => (float) _healingNeeded / _maxHealing;

    public Wound( WoundInfo info, BodyPart part, GameObject woundObj )
    {
        
        _affectedPart = part;
        _woundObj = woundObj;
        _healingNeeded = info.Damage;
        _maxHealing = _healingNeeded;
        _bleed = info.Bleed;
        _maxBleed = _bleed;

        if ( _healingNeeded <= 5 )
        {
            _type = WoundType.Minor;
        }
        else if ( _healingNeeded <= 15 )
        {
            _type = WoundType.Moderate;
        }
        else
        {
            _type = WoundType.Severe;
        }
        
    }

    public Wound( int damage, float bleed, BodyPart part, GameObject woundObj )
    {
        
        _affectedPart = part;
        _woundObj = woundObj;
        _healingNeeded = damage;
        _bleed = bleed;

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

    public WoundType GetWoundType()
    {
        if ( _healingNeeded >= 25 || _bleed >= 10 )
        {
            return WoundType.Severe;
        }
        else if ( _healingNeeded >= 15 || _bleed >= 5 )
        {
            return WoundType.Moderate;
        }
        else
        {
            return WoundType.Minor;
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

public struct WoundInfo
{
    public int Damage;
    public float Bleed;

    public WoundInfo(int damage, float bleed)
    {
        Damage = damage;
        Bleed = bleed;
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

public enum BleedCategory : int
{
    None = 0,
    Trickle = 1,
    SlowBleed = 2,
    SteadyBleed = 3,
    HeavyBleed = 4,
    Hemorrhage = 5,
    Exodus = 6
    
}