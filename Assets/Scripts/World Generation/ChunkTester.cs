// using UnityEngine;
// using System.Collections;
// using UnityEditor;

// [CustomEditor(typeof(ChunkTest))]
// public class ChunkTester : Editor {
//     public override void OnInspectorGUI() {
//         DrawDefaultInspector();

//         ChunkTest myScript = (ChunkTest)target;
//         if (GUILayout.Button("Create Chunk")) {
//             myScript.CreateChunk();
//         }
//     }
// }