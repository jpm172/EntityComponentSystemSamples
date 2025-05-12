using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Items/New Attachment Item", order = 1)]
public class AttachmentItemData : ItemData
{
    [Tooltip("Attachment Slot")]
    public AttachmentSlot Slot;
}
//

public enum AttachmentSlot
{
    Underbarrel,
    Sight,
    Magazine,
    Muzzle,
    Tactical
}