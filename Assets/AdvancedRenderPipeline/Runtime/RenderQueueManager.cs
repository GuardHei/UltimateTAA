using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public class RenderQueueManager : MonoBehaviour {
		public static readonly RenderQueueRange STRICT_OPAQUE_QUEUE = new RenderQueueRange(0, 2000);
		public static readonly RenderQueueRange ALPHATEST_QUEUE = new RenderQueueRange(2001, 2450);
		public static readonly RenderQueueRange OPAQUE_QUEUE = new RenderQueueRange(0, 2500);
		public static readonly RenderQueueRange TRANSPARENT_QUEUE = new RenderQueueRange(2501, 5000);
		public static readonly RenderQueueRange ALL_QUEUE = RenderQueueRange.all;
	}
}