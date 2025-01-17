using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragManager : MonoBehaviour
{

    private const string InventoryTag = "InventorySlot";
    
    [SerializeField]
    private RectTransform
        _defaultLayer = null,
        _dragLayer = null;


    [SerializeField]
    private GameObject _itemPrefab;
    
    private Rect _boundingBox;

    [SerializeField]
    private GameObject[] _inventorySlots;

    private DragObject _currentDraggedObject = null;
    public DragObject CurrentDraggedObject => _currentDraggedObject;

    public GameObject ItemPrefab => _itemPrefab;

    private void Awake()
    {
        _boundingBox = GetBoundingBoxRect(_dragLayer);
        
        _inventorySlots = GameObject.FindGameObjectsWithTag( InventoryTag );
    }

    public void RegisterDraggedObject(DragObject drag)
    {
        _currentDraggedObject = drag;
        drag.transform.SetParent(_dragLayer);
    }

    public void UnregisterDraggedObject(DragObject drag, Vector2 position)
    {
        _currentDraggedObject = null;
        if ( TryPutIntoSlot( drag, position ) )
        {
            return;
        }
        drag.transform.SetParent(_defaultLayer);
    }

    public bool IsWithinBounds(Vector2 position)
    {
        return _boundingBox.Contains(position);
    }

    public void SpawnItem( ItemInfo item )
    {
        DragObject newItem = Instantiate( ItemPrefab, _defaultLayer ).GetComponent<DragObject>();
        if(_currentDraggedObject != null)
            RegisterDraggedObject( newItem );
        
    }
    
    private bool TryPutIntoSlot(DragObject drag, Vector2 position)
    {
        for(int i = 0; i < _inventorySlots.Length; i++)
        {
            Rect rect = GetBoundingBoxRect( _inventorySlots[i].GetComponent<RectTransform>() );
            if ( rect.Contains( position ) )
            {
                ItemInfo item = drag.GetComponent<ItemInfo>();
                _inventorySlots[i].GetComponent<InventorySlot>().AddItem( item );
                Destroy( drag.gameObject );
                return true;
            }
        }

        return false;
    }

    /*
    private void SetBoundingBoxRect(RectTransform rectTransform)
    {
        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        var position = corners[0];

        Vector2 size = new Vector2(
            rectTransform.lossyScale.x * rectTransform.rect.size.x,
            rectTransform.lossyScale.y * rectTransform.rect.size.y);

        _boundingBox = new Rect(position, size);
    }
    */
    
    private Rect GetBoundingBoxRect(RectTransform rectTransform)
    {
        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        var position = corners[0];

        Vector2 size = new Vector2(
            rectTransform.lossyScale.x * rectTransform.rect.size.x,
            rectTransform.lossyScale.y * rectTransform.rect.size.y);

        return new Rect(position, size);
    }
    
}
