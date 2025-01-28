using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = System.Random;

public class InventoryItemLayout : MonoBehaviour
{
    [SerializeField]
    protected float _padding = 10;

    protected ItemContainer _item;

    [SerializeField]
    protected TextMeshProUGUI _itemText;
    [SerializeField]
    protected Image _itemImage;

    [SerializeField]
    protected Image _transferingImage;

    private void Start()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
    }

    public void CallBack()
    {
        GetComponentInParent<InventoryManager>().RemovedItem();
        Destroy( gameObject );
    }

    public void SwapCallback()
    {
        Initialize();
    }
    
}
