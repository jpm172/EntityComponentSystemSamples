using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPanelManager : DragManager
{
    [SerializeField]
    private BodyHealthManager _bodyManager;
    
    [SerializeField]
    private GameObject[] _bodyParts;
    protected override void Awake()
    {
        base.Awake();
        
    }

    protected override void TryPutIntoSlot( DragObject drag, Vector2 position )
    {
        if ( !GetBoundingBoxRect( _dragLayer ).Contains( position ) )
        {
            //TODO implement dropping items onto ground
            drag.Callback();
            return;
        }

        if ( drag.Container.Type != ItemType.Health )
            return;
        
        for(int i = 0; i < _bodyParts.Length; i++)
        {
            Rect rect = GetBoundingBoxRect( _bodyParts[i].GetComponent<RectTransform>() );
            if ( rect.Contains( position ) )
            {
                BodyLabel bodyLabel = _bodyParts[i].GetComponent<BodyLabel>();
                
                //_bodyManager.HealBodyPart( bodyLabel.BodyPart, (HealthItemInfo)drag.Container.Item );
                _bodyManager.HealBodyPartECS( bodyLabel.BodyPart, (HealthItemInfo)drag.Container.Item );
                drag.UpdateCallback();
                return;
            }
        }
        
    }

}
