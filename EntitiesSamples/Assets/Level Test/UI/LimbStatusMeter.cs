using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LimbStatusMeter : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _healthText;
    [SerializeField]
    private TextMeshProUGUI _bleedText;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private Image _meterImage;
    
    [SerializeField]
    private Image _destroyedImage;


    
    
    public void UpdateStatus(Limb limb)
    {
        _healthText.text = $"{limb.CurrentHealth}|{limb.MaxHealth}";
        _bleedText.text = $"{limb.Bleed:0.0}\nSec";
        if ( limb.Bleed <= Mathf.Epsilon )
        {
            _canvasGroup.alpha = 0.5f;
        }
        else
        {
            _canvasGroup.alpha = 1;
        }

        _meterImage.fillAmount = (float)limb.CurrentHealth / limb.MaxHealth;

        if ( limb.CurrentHealth <= 0 )
        {
            _destroyedImage.enabled = true;
        }
        else if(_destroyedImage.enabled)
        {
            _destroyedImage.enabled = false;
        }
    }
}
