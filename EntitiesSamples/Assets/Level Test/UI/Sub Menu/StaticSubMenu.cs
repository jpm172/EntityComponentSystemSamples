using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class StaticSubMenu : MonoBehaviour, IPointerClickHandler
{
    private PlayerUIManager _manager;
    
    public delegate void SelectAction(int value);
    public SelectAction OnSelected;
    
    [SerializeField]
    private List<SubMenuOption> _options;
    // Start is called before the first frame update
    void Start()
    {
        _manager = PlayerUIManager.Instance;
    }

    public void OnPointerClick( PointerEventData eventData )
    {
        if ( eventData.button != PointerEventData.InputButton.Left )
            return;

        SubMenu menu = _manager.CreateStaticSubMenu();
        
        menu.AddOptions( _options );
        menu.OnSelected = Select;

        menu.transform.position = transform.position;
        menu.gameObject.SetActive( true );
        menu.OpenMenu();
        
    }

    public void Select( int value )
    {
        OnSelected( value );
    }
    
}
