using System;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI.CoroutineTween;

public class SubMenu : MonoBehaviour, IPointerClickHandler
{
    private static float _buffer = 10f;
    
    public Canvas RootCanvas;

    public GameObject OptionPrefab;
    public Transform OptionsTransform;
    
    [SerializeField]
    private List<SubMenuOption> _options;
    
    
    public delegate void SelectAction(int value);
    public SelectAction OnSelected;

    private GameObject _blocker;
    private bool _open;

    public void OnPointerClick( PointerEventData eventData )
    {
        if ( !_open )
        {
            OpenMenu();
        }
        else
        {
            Hide();
        }
        
    }

    public void AddOption(SubMenuOption newOption)
    {
        if(_options == null)
            _options = new List<SubMenuOption>();
        
        _options.Add( newOption );
        
    }

    public void ClearOptions()
    {
        if ( _options == null )
        {
            _options = new List<SubMenuOption>();
            return;
        }
        
        _options.Clear();
            
    }
    
    public void OpenMenu()
    {
        float maxWidth = Mathf.NegativeInfinity;
        RectTransform[] optionRects = new RectTransform[_options.Count];
        for ( int i = 0; i < _options.Count; i++ )
        {
            SubMenuOption o = _options[i];
            GameObject newOption = Instantiate( OptionPrefab, OptionsTransform );
            TextMeshProUGUI tmp = newOption.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = o.Text;
            if ( tmp.preferredWidth > maxWidth )
                maxWidth = tmp.preferredWidth;

            optionRects[i] = newOption.GetComponent<RectTransform>();
            
            ValueButton button = newOption.GetComponent<ValueButton>();
            button.Value = o.Value;
            button.OnClick = ButtonClicked;
        }


        foreach ( RectTransform rect in optionRects )
        {
            rect.sizeDelta = new Vector2(maxWidth + _buffer, rect.sizeDelta.y);
        }
        
        _blocker = CreateBlocker( RootCanvas );
        _open = true;
    }

    public void ButtonClicked( int value )
    {
        //Debug.Log( value );
        OnSelected(value);
        Hide();
    }
    
    /// <summary>
    /// Create a blocker that blocks clicks to other controls while the dropdown list is open.
    /// </summary>
    /// <remarks>
    /// Override this method to implement a different way to obtain a blocker GameObject.
    /// </remarks>
    /// <param name="rootCanvas">The root canvas the dropdown is under.</param>
    /// <returns>The created blocker object</returns>
    protected virtual GameObject CreateBlocker(Canvas rootCanvas)
    {
        // Create blocker GameObject.
        GameObject blocker = new GameObject("Blocker");

        // Setup blocker RectTransform to cover entire root canvas area.
        RectTransform blockerRect = blocker.AddComponent<RectTransform>();
        blockerRect.SetParent(rootCanvas.transform, false);
        blockerRect.anchorMin = Vector3.zero;
        blockerRect.anchorMax = Vector3.one;
        blockerRect.sizeDelta = Vector2.zero;

        // Make blocker be in separate canvas in same layer as dropdown and in layer just below it.
        Canvas blockerCanvas = blocker.AddComponent<Canvas>();
        blockerCanvas.overrideSorting = true;
        
        //Canvas dropdownCanvas = m_Dropdown.GetComponent<Canvas>();
        Canvas dropdownCanvas = GetComponent<Canvas>();
        blockerCanvas.sortingLayerID = dropdownCanvas.sortingLayerID;
        blockerCanvas.sortingOrder = dropdownCanvas.sortingOrder - 1;
        
        

        // Find the Canvas that this dropdown is a part of
        Canvas parentCanvas = null;
        //Transform parentTransform = m_Template.parent;
        Transform parentTransform = transform.parent;
        while (parentTransform != null)
        {
            parentCanvas = parentTransform.GetComponent<Canvas>();
            if (parentCanvas != null)
                break;

            parentTransform = parentTransform.parent;
        }

        // If we have a parent canvas, apply the same raycasters as the parent for consistency.
        if (parentCanvas != null)
        {
            Component[] components = parentCanvas.GetComponents<BaseRaycaster>();
            for (int i = 0; i < components.Length; i++)
            {
                Type raycasterType = components[i].GetType();
                if (blocker.GetComponent(raycasterType) == null)
                {
                    blocker.AddComponent(raycasterType);
                }
            }
        }
        else
        {
            // Add raycaster since it's needed to block.
            GetOrAddComponent<GraphicRaycaster>(blocker);
        }


        // Add image since it's needed to block, but make it clear.
        Image blockerImage = blocker.AddComponent<Image>();
        blockerImage.color = Color.clear;

        // Add button since it's needed to block, and to close the dropdown when blocking area is clicked.
        Button blockerButton = blocker.AddComponent<Button>();
        blockerButton.onClick.AddListener(Hide);

        return blocker;
    }


    public void Hide()
    {
        if ( _blocker != null )
        {
            Destroy( _blocker );
            for ( int i = 0; i < OptionsTransform.childCount; i++ )
            {
                Destroy(OptionsTransform.GetChild( i ).gameObject);
            }
        }

        _open = false;
    }
    
    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (!comp)
            comp = go.AddComponent<T>();
        return comp;
    }
}

[Serializable]
public class SubMenuOption
{
    [SerializeField]
    private string _text;
    
    [SerializeField]
    private int _value;

    /// <summary>
    /// The text associated with the option.
    /// </summary>
    public string Text { get { return _text; } set { _text = value; } }
    public int Value { get { return _value; } set { _value = value; } }

    public SubMenuOption() { }

    public SubMenuOption(string text, int value)
    {
        _text = text;
        _value = value;
    }
    
}
