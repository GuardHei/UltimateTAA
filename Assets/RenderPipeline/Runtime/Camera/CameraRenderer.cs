using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public unsafe abstract class CameraRenderer {
    
    public Camera camera;
    public AdvancedCameraType cameraType;

    public int outputWidth;
    public int outputHeight;
    public float xRatio;
    public float yRatio;
    
    public Vector2 Ratio => new Vector2(xRatio, yRatio);

    public int internalWidth => Mathf.CeilToInt(outputWidth * xRatio);

    public int internalHeight => Mathf.CeilToInt(outputHeight * yRatio);

    internal ScriptableRenderContext _context; // Not persistent
    internal CullingResults _cullingResults;
    
    public abstract void Render(ScriptableRenderContext context);

    public abstract void Setup();

    public void ExecuteCommand(CommandBuffer buffer, bool clear = true) {
        _context.ExecuteCommandBuffer(buffer);
        if (clear) buffer.Clear();
    }
    
    public void ExecuteCommandAsync(CommandBuffer buffer, ComputeQueueType queueType, bool clear = true) {
        _context.ExecuteCommandBufferAsync(buffer, queueType);
        if (clear) buffer.Clear();
    }

    public void Submit() => _context.Submit();
    
    public virtual void SetResolution(int w, int h) {
        outputWidth = w;
        outputHeight = h;
    }
    
    public virtual void SetRatio(float x, float y) {
        xRatio = x;
        yRatio = y;
    }

    public virtual void SetResolutionAndRatio(int w, int h, float x, float y) {
        outputWidth = w;
        outputHeight = h;
        xRatio = x;
        yRatio = y;
    }

    public CameraRenderer(Camera camera) => this.camera = camera;

    public static CameraRenderer CreateCameraRenderer(Camera camera, AdvancedCameraType type) {
        switch (type) {
            case AdvancedCameraType.Game: return new GameCameraRenderer(camera);
            case AdvancedCameraType.SceneView: return new GameCameraRenderer(camera);
            case AdvancedCameraType.Preview: return new GameCameraRenderer(camera);
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
    Reflection = 16
}
