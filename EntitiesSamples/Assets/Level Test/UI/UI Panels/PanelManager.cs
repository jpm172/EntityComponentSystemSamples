using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PanelManager : MonoBehaviour
{
   [SerializeField]
   protected RectTransform _dragLayer;
   public abstract void DropItem( DragObject drag );

   public RectTransform DragLayer => _dragLayer;

   protected Rect GetBoundingBoxRect(RectTransform rectTransform)
   {
      var corners = new Vector3[4];
      rectTransform.GetWorldCorners(corners);
      var position = corners[0];

      Vector2 size = new Vector2(
         rectTransform.lossyScale.x * rectTransform.rect.size.x,
         rectTransform.lossyScale.y * rectTransform.rect.size.y);

      return new Rect(position, size);
   }
}
