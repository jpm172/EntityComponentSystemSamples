using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IPointerDownHandler
{
    private static readonly Vector2 _emptySize = new Vector2( 40, 40 );
   
    
    [SerializeField]
    private Image _displayImage;

    [SerializeField]
    private Image _trasnferingImage;
    
    [SerializeField]
    private float _padding = 10;
    
    [SerializeField] 
    private RectTransform _containerRect;

    private DragManager _dragManager;
    [SerializeField]
    private ItemContainer _container;

    [SerializeField]
    private ItemType _slotItemType;

    [SerializeField] 
    private InventorySlotType _equipType;

    private EntityManager _entityManager;
    
    private bool _hasItem;

    public bool HasItem => _hasItem;

    public void Awake()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        _entityManager = world.EntityManager;
        
        _hasItem = false;
        _dragManager = GetComponentInParent<DragManager>();
        _container = GetComponent<ItemContainer>();
        _displayImage.gameObject.SetActive( false );
    }

    public void AddItem( ItemContainer item )
    {
        _displayImage.gameObject.SetActive( true );
        
        Vector2 spriteSize = item.Data.ItemSprite.textureRect.size;
        float xScale = _containerRect.rect.width  / (spriteSize.x+ _padding*2);
        float yScale = _containerRect.rect.height / (spriteSize.y+ _padding*2);
        float scale = Math.Min( xScale, yScale );
        //Debug.Log( xScale + ", " + yScale + " == " + scale );
        
        _displayImage.sprite = item.Data.ItemSprite;
        _displayImage.rectTransform.sizeDelta = spriteSize * scale;
        
        _container.Item = item.Item;
        _hasItem = true;
        
        EquipItem();
    }

    private void UpdateItem()
    {
        Vector2 spriteSize = _container.Data.ItemSprite.textureRect.size;
        float xScale = _containerRect.rect.width  / (spriteSize.x+ _padding*2);
        float yScale = _containerRect.rect.height / (spriteSize.y+ _padding*2);
        float scale = Math.Min( xScale, yScale );
        //Debug.Log( xScale + ", " + yScale + " == " + scale );
        
        _displayImage.sprite = _container.Data.ItemSprite;
        _displayImage.rectTransform.sizeDelta = spriteSize * scale;
    }
    

    public void SwapItem(DragObject drag)
    {
        /*
        ItemData swap = _container.Item.Data;
        _container.Item.Data = drag.TransferFromContainer.Data;
        drag.TransferFromContainer.Data = swap;
        drag.SwapCallback();
        AddItem( _container );
        */
        ItemInfo swap = _container.Item;
        _container.Item = drag.TransferFromContainer.Item;
        drag.TransferFromContainer.Item = swap;
        drag.SwapCallback();
        AddItem( _container );
    }

    public bool MatchesType(ItemContainer container)
    {
        return container.Type == _slotItemType;
    }

    public void GetTransferItem()
    {
        if ( !_hasItem )
            return;
        
        DragObject transferItem = _dragManager.SpawnItem( _container.Item, GetComponent<RectTransform>().position );
        transferItem.Callback = Callback;
        transferItem.SwapCallback = SwapCallback;
        transferItem.TransferFromContainer = _container;
        transferItem.SourceObject = gameObject;
    }

    public void RemoveItem()
    {
        _displayImage.sprite = null;
        _displayImage.gameObject.SetActive( false );
        _hasItem = false;
        UnequipItem();
    }

    private void EquipItem()
    {
        bool hasPlayer = _entityManager.CreateEntityQuery( typeof( PlayerInputs ) )
            .TryGetSingletonEntity<Entity>(out Entity player);
        if ( !hasPlayer )
            return;
        
        if ( _equipType == InventorySlotType.Primary )
        {
            CharacterInventory inv = _entityManager.GetComponentData<CharacterInventory>( player );
            //inv.PrimaryWeapon = ItemToWeapon();
            inv.PrimaryWeapon = ( (WeaponItemInfo) _container.Item ).Weapon;
            _entityManager.SetComponentData( player, inv );
        }
        else if ( _equipType == InventorySlotType.Secondary )
        {
            CharacterInventory inv = _entityManager.GetComponentData<CharacterInventory>( player );
            //inv.SecondaryWeapon = ItemToWeapon();
            inv.SecondaryWeapon = ( (WeaponItemInfo) _container.Item ).Weapon;
            _entityManager.SetComponentData( player, inv );
        }
    }

    private void UnequipItem()
    {
        bool hasPlayer = _entityManager.CreateEntityQuery( typeof( PlayerInputs ) )
            .TryGetSingletonEntity<Entity>(out Entity player);
        if ( !hasPlayer )
            return;
        
        if ( _equipType == InventorySlotType.Primary )
        {
            CharacterInventory inv = _entityManager.GetComponentData<CharacterInventory>( player );
            inv.PrimaryWeapon = new WeaponDesc{Null = true};
            _entityManager.SetComponentData( player, inv );
        }
        else if ( _equipType == InventorySlotType.Secondary )
        {
            CharacterInventory inv = _entityManager.GetComponentData<CharacterInventory>( player );
            inv.SecondaryWeapon = new WeaponDesc{Null = true};
            _entityManager.SetComponentData( player, inv );
        }
    }

    public void OnPointerDown( PointerEventData eventData )
    {
        GetTransferItem();
    }

    public void Callback()
    {
       RemoveItem();
    }

    public void SwapCallback()
    {
        UpdateItem();
    }
}

public enum InventorySlotType : int
{
    Primary = 0,
    Secondary = 1,
    Armor = 2,
    Helmet = 3,
}
