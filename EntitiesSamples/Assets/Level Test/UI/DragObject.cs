using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DragObject : MonoBehaviour, IPointerDownHandler
{
    private DragManager _manager = null;

    private Vector2 _centerPoint;
    private ItemInfo _item;
    public Vector2 _worldCenterPoint => transform.TransformPoint(_centerPoint);

    public delegate void CallbackDelegate();
    public CallbackDelegate Callback;

    public delegate void SwapCallbackDelegate();
    public SwapCallbackDelegate SwapCallback;
    
    public ItemInfo TransferFromObj;
    
    private void Start()
    {
        _item = GetComponent<ItemInfo>();
        _manager = GetComponentInParent<DragManager>();
        _centerPoint = (transform as RectTransform).rect.center;
    }

    public void OnPointerDown( PointerEventData eventData )
    {
        DragObject transfer = _manager.SpawnItem( _item.Data, GetComponent<RectTransform>().position );
        transfer.Callback = Callback;
        transfer.SwapCallback = SwapCallback;
        transfer.TransferFromObj = TransferFromObj;
        //_manager.PickUpItem(this);
    }
}
