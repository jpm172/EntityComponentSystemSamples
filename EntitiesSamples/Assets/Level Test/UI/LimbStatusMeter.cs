using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LimbStatusMeter : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _text;

    [SerializeField]
    private Image _meterImage;
    
    [SerializeField]
    private Image _destroyedImage;


    public void UpdateStatus(Limb limb)
    {
        _text.text = $"{limb.CurrentHealth}|{limb.MaxHealth}";
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
