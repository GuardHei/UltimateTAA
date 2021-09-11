using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class CameraRenderer {
    
    public Camera camera;
    public AdvancedCameraType cameraType;

    internal ScriptableRenderContext _context; // Not persistent
    
    public abstract void Render(ScriptableRenderContext context);

    public abstract void Setup();

    public void Submit() => _context.Submit();

    public CameraRenderer(Camera camera) => this.camera = camera;

    public static CameraRenderer CreateCameraRenderer(Camera camera, AdvancedCameraType type) {
        switch (type) {
            case AdvancedCameraType.Game: return new GameCameraRenderer(camera);
            default: throw new InvalidOperationException("Does not support camera type: " + type);
        }
    }

    public static AdvancedCameraType DefaultToAdvancedCameraType(CameraType cameraType) {
        switch (cameraType) {
            case CameraType.Game: return AdvancedCameraType.Game;
            case CameraType.SceneView: return AdvancedCameraType.SceneView;
            case CameraType.Preview: return AdvancedCameraType.Preview;
            case CameraType.VR: return AdvancedCameraType.VR;
            case CameraType.Reflection: return AdvancedCameraType.Reflection;
            default: throw new InvalidOperationException("Does not support camera type: " + cameraType);
        }
    }
}

public enum AdvancedCameraType {
    Game = 1,
    SceneView = 2,
    Preview = 4,
    VR = 8,
    Reflection = 16,
    Shadow = 32
}
