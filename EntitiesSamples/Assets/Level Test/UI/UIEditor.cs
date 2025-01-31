using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerUIManager))]
public class UIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PlayerUIManager manager = target as PlayerUIManager;

        if ( manager == null || !EditorApplication.isPlaying )
            return;

        if ( GUILayout.Button( "Serialize Items" ) )
        {
            manager.SerializeItems();
        }
        
        if ( GUILayout.Button( "Wound Player" ) )
        {
            manager.AddWound();
        }
    }
}

#endif