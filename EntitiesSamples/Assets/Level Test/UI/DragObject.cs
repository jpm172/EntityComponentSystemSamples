using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DragObject : MonoBehaviour, IPointerDownHandler
{
    private DragManager _manager = null;

    private Vector2 _centerPoint;
    private ItemContainer _container;
    public Vector2 _worldCenterPoint => transform.TransformPoint(_centerPoint);

    public delegate void UpdateCallbackDelegate();
    public UpdateCallbackDelegate UpdateCallback;
    
    public delegate void CallbackDelegate();
    public CallbackDelegate Callback;

    public delegate void SwapCallbackDelegate();
    public SwapCallbackDelegate SwapCallback;
    
    public ItemContainer TransferFromContainer;
    public GameObject SourceObject;

    public ItemContainer Container => _container;

    public bool IsTransferItem;

    public bool BlockAdd;

    //initialize when instansiating new item
    public void Initialize()
    {
        _container = GetComponent<ItemContainer>();
        _manager = PlayerUIManager.Instance.gameObject.GetComponent<DragManager>();
        //_manager = GetComponentInParent<DragManager>();
        //_centerPoint = (transform as RectTransform).rect.center;
        _centerPoint = GetComponent<RectTransform>().rect.center;
    }

    public void OnPointerDown( PointerEventData eventData )
    {
        if ( IsTransferItem )
            return;

        if ( eventData.button == PointerEventData.InputButton.Right )
        {
            Debug.Log( "right click" );
            //TODO: implement options menu
            return;
        }
        
        //DragObject transfer = _manager.SpawnItem( _container.Item.Data, GetComponent<RectTransform>().position );
        DragObject transfer = _manager.SpawnItem( _container.Item, GetComponent<RectTransform>().position );
        transfer.Callback = Callback;
        transfer.UpdateCallback = UpdateCallback;
        transfer.SwapCallback = SwapCallback;
        transfer.TransferFromContainer = TransferFromContainer;
        transfer.SourceObject = SourceObject;
        //_manager.PickUpItem(this);
    }
    
}
