using AdvancedRenderPipeline.Runtime;
using UnityEditor;
using UnityEngine;

namespace AdvancedRenderPipeline.Editor.ShaderGUIs {
    public class MVShaderGUI : ShaderGUI {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
            materialEditor.PropertiesDefaultGUI(properties);

            foreach(var target in materialEditor.targets) {
                var material = target as Material;
                // Debug.Log(material.name + " " + material.GetShaderPassEnabled(ShaderTagManager.MOTION_VECTORS_PASS));
                material.SetShaderPassEnabled(ShaderTagManager.MOTION_VECTORS_PASS, false);
            }
        }
    }
}