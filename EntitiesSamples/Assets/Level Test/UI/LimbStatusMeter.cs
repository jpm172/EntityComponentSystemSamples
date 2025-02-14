using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LimbStatusMeter : MonoBehaviour
{

    private static Color _green = new Color(0.03921569f, 0.8352941f, 0.03921569f);
    private static Color _red = new Color(0.8352941f, 0.07843138f, 0.03529412f);
    
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


    public void Initialize(Limb limb)
    {
        _meterImage.color = ( limb.BodyPart == BodyPart.Chest ) ? _red : _green; 
        UpdateStatus( limb );
    }
    
    //0AD50A - green
    //D51409 - red
    public void UpdateStatus(Limb limb)
    {

        if ( limb.BodyPart == BodyPart.Chest )
        {
            UpdateChestStatus( limb );
            return;
        }
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

    private void UpdateChestStatus( Limb limb )
    {
        _healthText.text = $"{Mathf.Abs(limb.CurrentHealth)}";
        _bleedText.text = $"{limb.Bleed:0.0}\nSec";
        if ( limb.Bleed <= Mathf.Epsilon )
        {
            _canvasGroup.alpha = 0.5f;
        }
        else
        {
            _canvasGroup.alpha = 1;
        }
    }
}
