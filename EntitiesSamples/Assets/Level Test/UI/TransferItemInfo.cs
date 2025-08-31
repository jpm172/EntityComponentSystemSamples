using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TransferItemInfo : MonoBehaviour
{
    private TextMeshProUGUI _text;
    
    
    
    //use start instead of awake to let the ItemInfo properly update after being instantiated
    private void Start()
    {
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _text.text = GetComponent<ItemContainer>().Data.ItemName;
    }
    
}
