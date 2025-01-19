using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TransferItemInfo : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private void Awake()
    {
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _text.text = GetComponent<ItemInfo>().Data.ItemName;
    }
}
