using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ShaderTagManager {
    
	public static readonly ShaderTagId NONE = ShaderTagId.none;
	public static readonly ShaderTagId SRP_DEFAULT_UNLIT = new ShaderTagId("SRPDefaultUnlit");
	public static readonly ShaderTagId DEPTH = new ShaderTagId("Depth");
	public static readonly ShaderTagId DEPTH_NORMAL = new ShaderTagId("DepthNormal");
	public static readonly ShaderTagId STENCIL = new ShaderTagId("Stencil");
	
	public static readonly RenderQueueRange STRICT_OPAQUE = new RenderQueueRange(0, 2000);
	public static readonly RenderQueueRange ALPHATEST = new RenderQueueRange(2001, 2450);
	public static readonly RenderQueueRange OPAQUE = new RenderQueueRange(0, 2500);
	public static readonly RenderQueueRange TRANSPARENT = new RenderQueueRange(2501, 5000);
	public static readonly RenderQueueRange ALL = RenderQueueRange.all;
}
