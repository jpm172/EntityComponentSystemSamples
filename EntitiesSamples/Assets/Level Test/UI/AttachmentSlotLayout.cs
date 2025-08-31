using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttachmentSlotLayout : MonoBehaviour
{
    [SerializeField]
    private AttachmentSlot _slotType;

    public AttachmentSlot SlotType => _slotType;

    [SerializeField]
    private AspectRatioFitter _imageFitter;
    [SerializeField]
    private Image _attachmentImage;
    
    
    public void Initialize()
    {
        _imageFitter = GetComponentInChildren<AspectRatioFitter>();
        _attachmentImage = _imageFitter.GetComponent<Image>();
        ClearSlot();
    }

    public void SetImage( Sprite attachmentSprite )
    {
        _attachmentImage.gameObject.SetActive( true );
        _attachmentImage.sprite = attachmentSprite;
        _imageFitter.aspectRatio = attachmentSprite.textureRect.size.x / attachmentSprite.textureRect.size.y;
    }

    public void ClearSlot()
    {
        _attachmentImage.gameObject.SetActive( false );
    }
    
}
