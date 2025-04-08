using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusEffectLayout : MonoBehaviour
{
    [SerializeField]
    private Image _background;
    [SerializeField]
    private TextMeshProUGUI _displayText;

    private static Color _buffColor = new Color(0.522f, 1, 0.518f);
    private static Color _neutralColor = Color.white;
    private static Color _debuffColor = new Color(1f, 0.518f, 0.557f);
    
    //DEBUFF COLOR - FF848E
    //BUFF COLOR - 85FF84
    
    public void Initialize( StatusEffectInfo effectInfo )
    {
        switch ( effectInfo.Quality )
        {
            case StatusEffectQuality.Buff:
                _background.color = _buffColor;
                break;
            case StatusEffectQuality.Debuff:
                _background.color = _debuffColor;
                break;
            default:
                _background.color = _neutralColor;
                break;
        }
    }

}
