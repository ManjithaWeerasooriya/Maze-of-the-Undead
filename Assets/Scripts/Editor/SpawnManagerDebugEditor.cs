using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomEditor(typeof(SpawnManager))]
public class SpawnManagerDebugEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpawnManager spawnManager = (SpawnManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Info", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Current Wave", spawnManager.currentWave.ToString());
        EditorGUILayout.LabelField("Zombie Count", spawnManager.GetZombieCount() + " / " + spawnManager.maxZombies);

        EditorGUILayout.Space();

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Force Spawn Zombie", GUILayout.Height(30)))
            {
                MethodInfo method = typeof(SpawnManager).GetMethod("SpawnZombie", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(spawnManager, null);
                }
            }

            if (GUILayout.Button("Force Next Wave", GUILayout.Height(30)))
            {
                MethodInfo method = typeof(SpawnManager).GetMethod("StartWave", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    spawnManager.currentWave++;
                    method.Invoke(spawnManager, null);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Debug buttons only available during Play mode", MessageType.Info);
        }
    }
}
