namespace AdvancedRenderPipeline.Runtime {
	public class RenderLayerManager {

		public static readonly uint All = 4294967295;
		
		public static readonly uint DEFAULT = 1;
		public static readonly uint STATIC = 1 << 1;
		public static readonly uint TERRAIN = 1 << 2;
	}
}