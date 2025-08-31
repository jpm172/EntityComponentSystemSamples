using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ValueButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{

    public Color DefaultColor = Color.white;
    public Color HoverColor = Color.gray;
    
    public int Value;

    private Image _backgroundImg;
    
    public delegate void OnClickDelegate(int value);
    public OnClickDelegate OnClick;


    private void Awake()
    {
        _backgroundImg = GetComponent<Image>();
    }

    public void OnPointerClick( PointerEventData eventData )
    {
        if ( eventData.button == PointerEventData.InputButton.Left )
        {
            OnClick(Value);
        }
    }

    public void OnPointerEnter( PointerEventData eventData )
    {
        _backgroundImg.color = HoverColor;
    }

    public void OnPointerExit( PointerEventData eventData )
    {
        _backgroundImg.color = DefaultColor;
    }
}
