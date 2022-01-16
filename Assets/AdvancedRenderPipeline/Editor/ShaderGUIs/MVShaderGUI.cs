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
            if (mat.HasProperty("_HeightMap"))
                CoreUtils.SetKeyword(mat, "_PARALLAX_MAP", mat.GetTexture("_HeightMap") && mat.GetFloat("_HeightScale") > float.Epsilon);

            EditorGUILayout.HelpBox("POM Enabled: " + mat.IsKeywordEnabled("_PARALLAX_MAP"), MessageType.Info);
        }
    }
    
    public class StandardStaticShaderGUI : ShaderGUI {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
            materialEditor.PropertiesDefaultGUI(properties);
            var mat = materialEditor.target as Material;
            if (!mat) return;
            if (mat.HasProperty("_HeightMap"))
                CoreUtils.SetKeyword(mat, "_PARALLAX_MAP", mat.GetTexture("_HeightMap") && mat.GetFloat("_HeightScale") > float.Epsilon);

            EditorGUILayout.HelpBox("POM Enabled: " + mat.IsKeywordEnabled("_PARALLAX_MAP"), MessageType.Info);
        }
    }
}