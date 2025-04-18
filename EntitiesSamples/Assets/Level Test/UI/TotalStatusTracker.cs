using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TotalStatusTracker : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _healthText;
    
    [SerializeField]
    private TextMeshProUGUI _bleedRateText;
    
    [SerializeField]
    private TextMeshProUGUI _bleedCategoryText;

    private PlayerUIManager _manager;
    

    private void Start()
    {
        _manager = PlayerUIManager.Instance;
        UpdateText();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateText();
    }

    private void UpdateText()
    {
        _healthText.text = $"{_manager.PlayerCurrentHealth:0}|{_manager.PlayerMaxHealth:0}";
        
        float bleedRate = _manager.PlayerBleedRate;
        _bleedRateText.text = $"{bleedRate:0.00}";
        _bleedCategoryText.text = MyExtensionMethods.GetBleedCategory( bleedRate ).ToString(); 
    }
    
}




