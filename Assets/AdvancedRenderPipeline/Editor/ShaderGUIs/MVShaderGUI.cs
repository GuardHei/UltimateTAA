using AdvancedRenderPipeline.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Editor.ShaderGUIs {
    public class MVShaderGUI : ShaderGUI {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
            materialEditor.PropertiesDefaultGUI(properties);
            foreach(var target in materialEditor.targets) DoMV(target as Material);
        }

        public static void DoMV(Material mat) => mat.SetShaderPassEnabled(ShaderTagManager.MOTION_VECTORS_PASS, false);
    }

    public class StandardShaderGUI : ShaderGUI {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
            materialEditor.PropertiesDefaultGUI(properties);
            foreach(var target in materialEditor.targets) MVShaderGUI.DoMV(target as Material);
            
            var mat = materialEditor.target as Material;
            if (!mat) return;
            MaterialProperty map = FindProperty("_HeightMap", properties);
            Texture tex = map.textureValue;
            var enabled = mat.IsKeywordEnabled("_PARALLAX_MAP");
            if (tex == null && enabled) mat.DisableKeyword("_PARALLAX_MAP");
            else if (tex != null && !enabled) mat.EnableKeyword("_PARALLAX_MAP");
            EditorGUILayout.HelpBox("POM Enabled: " + mat.IsKeywordEnabled("_PARALLAX_MAP"), MessageType.Info);
        }
    }
    
    public class StandardStaticShaderGUI : ShaderGUI {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
            materialEditor.PropertiesDefaultGUI(properties);
            // foreach(var target in materialEditor.targets) MVShaderGUI.DoMV(target as Material);
            
            var mat = materialEditor.target as Material;
            if (!mat) return;
            MaterialProperty map = FindProperty("_HeightMap", properties);
            Texture tex = map.textureValue;
            var enabled = mat.IsKeywordEnabled("_PARALLAX_MAP");
            if (tex == null && enabled) mat.DisableKeyword("_PARALLAX_MAP");
            else if (tex != null && !enabled) mat.EnableKeyword("_PARALLAX_MAP");
            EditorGUILayout.HelpBox("POM Enabled: " + mat.IsKeywordEnabled("_PARALLAX_MAP"), MessageType.Info);
        }
    }
}