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

    private void Start()
    {
        _item = GetComponent<ItemInfo>();
        _manager = GetComponentInParent<DragManager>();
        _centerPoint = (transform as RectTransform).rect.center;
    }

    public void OnPointerDown( PointerEventData eventData )
    {
        _manager.SpawnItem( _item.Data, GetComponent<RectTransform>().position );
        //_manager.PickUpItem(this);
    }
}
