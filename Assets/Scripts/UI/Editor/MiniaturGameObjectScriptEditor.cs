using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ThesisHololens.UI.EditorMenu
{
    [CustomEditor(typeof(MiniaturGameObjectScript))]
    public class MiniaturGameObjectScriptEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default
            base.OnInspectorGUI();

            // Place the button at the bottom
            MiniaturGameObjectScript myScript = (MiniaturGameObjectScript)target;
            if (GUILayout.Button("Update preview"))
            {
                myScript.showGameObjectPreviewFromPrefab();
            }
        }
    }
}

