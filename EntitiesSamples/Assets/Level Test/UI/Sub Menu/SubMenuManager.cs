using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubMenuManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _subMenuPrefab;

    [SerializeField]
    private WorkbenchPanelManager _workbenchPanel;
    
    private GameObject _pooledSubMenu;
    private SubMenu _menu;
    
    private Canvas _rootCanvas;

    private PlayerUIManager _manager;

    private void Awake()
    {
        _rootCanvas = GetComponent<Canvas>();
        
    }

    void Start()
    {
        _manager = PlayerUIManager.Instance; 
            
        _pooledSubMenu = Instantiate( _subMenuPrefab, transform );
        _menu = _pooledSubMenu.GetComponent<SubMenu>();
        _menu.RootCanvas = _rootCanvas;
        _pooledSubMenu.SetActive( false );
    }


    public SubMenu CreateStaticSubMenu()
    {
        _menu.ResetMenu();
        return _menu;
    }
    
    public void CreateSubMenu( ItemContainer container, Vector2 clickPosition )
    {
        _menu.ResetMenu();
        
        ItemData data = container.Item.Data;
        _menu.SetItem( container.Item );
        _pooledSubMenu.transform.position = clickPosition;
        

        if ( data.ItemType == ItemType.Weapon )
        {
              SetUpWeaponSubMenu( );
        }

        if ( _menu.OptionsCount <= 0 )
            return;
        
        _pooledSubMenu.SetActive( true );
        _menu.OpenMenu();
    }

    private void SetUpWeaponSubMenu()
    {
        _menu.AddOption( new SubMenuOption("Equip Primary", 0) );    
        _menu.AddOption( new SubMenuOption("Equip Secondary", 1) );    
        _menu.AddOption( new SubMenuOption("Modify", 2) );
        _menu.OnSelected = WeaponSubMenuOnSelect;
    }


    private void WeaponSubMenuOnSelect( int value )
    {
        switch ( value )
        {
            case 0:
                break;
            case 1: 
                break;
            case 2:
                ModifyWeapon();
                break;
        }
    }


    private void ModifyWeapon()
    {
        _workbenchPanel.PlaceWeaponOnBench( _menu.RelatedItem );
        _manager.OpenWorkbench();
    }
    
    public void OnCloseInventory()
    {
        if ( !_pooledSubMenu.activeInHierarchy )
            return;
        
        _pooledSubMenu.GetComponent<SubMenu>().Hide();
        _pooledSubMenu.SetActive( false );
    }
    
}
