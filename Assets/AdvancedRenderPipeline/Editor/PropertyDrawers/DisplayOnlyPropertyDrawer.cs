using AdvancedRenderPipeline.Runtime.CustomAttributes;
using UnityEditor;
using UnityEngine;

namespace AdvancedRenderPipeline.Editor.PropertyDrawers {
    
    [CustomPropertyDrawer(typeof(DisplayOnlyAttribute))]
    public class DisplayOnlyPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var temp = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = temp;
        }
    }
}