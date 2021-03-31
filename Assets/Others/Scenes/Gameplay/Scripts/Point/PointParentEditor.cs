using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Assets.Others.Scenes.Gameplay.Scripts.Point
{
#if UNITY_EDITOR
    [CustomEditor(typeof(PointParent))]
    [CanEditMultipleObjects]
    [ExecuteInEditMode]
    class PointParentEditor : Editor
    {

        SerializedProperty m_difficulty;

        private void OnEnable()
        {            
            m_difficulty = this.serializedObject.FindProperty("Difficulty");
        }



        public override void OnInspectorGUI()
        {
            var target_parentpoint = target as PointParent;

            DrawDefaultInspector();

            //new
            this.serializedObject.Update();

            //buttons
            EditorGUILayout.Space(20);

            GUILayout.Label("Difficulty");

            EditorGUI.indentLevel += 10;

            GUILayout.BeginHorizontal();           

            for (var i=1; i<=5; i++)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Width(25), GUILayout.Height(25)))
                    m_difficulty.intValue = i;
                    //target_parentpoint.Difficulty = i;
            }            

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            for (var i = 6; i <= 10; i++)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Width(25), GUILayout.Height(25)))
                    m_difficulty.intValue = i;
                //target_parentpoint.Difficulty = i;
            }            

            GUILayout.EndHorizontal();

            EditorGUI.indentLevel -= 10;

            this.serializedObject.ApplyModifiedProperties();

        }
    }

#endif

}
