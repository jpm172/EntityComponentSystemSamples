using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(LevelGenerator))]
public class LevelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        LevelGenerator level  = target as LevelGenerator;

        if ( level == null || !EditorApplication.isPlaying)
            return;
        
        if (GUILayout.Button("Generate Level")) 
        {
            level.GenerateLevel();
        }
        if (GUILayout.Button("Grow Rooms")) 
        {
            level.GrowRooms();
        }
        if (GUILayout.Button("Make Room Meshes")) 
        {
            level.MakeRoomMeshes();
        }
        if (GUILayout.Button("Paint Walls")) 
        {
            level.PaintWalls();
        }
        

    }
}
#endif
