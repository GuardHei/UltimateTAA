using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public static class Blitter {
		
		public static void Blit(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src, RenderTextureSubElement.Color);
			// MaterialManager.BlitMaterial.SetTexture(ShaderKeywordManager.MAIN_TEXTURE, src);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.Blit, MeshTopology.Triangles, 3);
		}
		
		public static void BlitDepth(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src, RenderTextureSubElement.Depth);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.Blit, MeshTopology.Triangles, 3);
		}
		
		public static void BlitStencil(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src, RenderTextureSubElement.Stencil);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.BlitStencil, MeshTopology.Triangles, 3);
		}

		public static void BlitDebugStencil(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest) {
			cmd.SetGlobalTexture(ShaderKeywordManager.STENCIL_TEXTURE, src, RenderTextureSubElement.Stencil);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.DebugStencil, MeshTopology.Triangles, 3);
		}

		public static void BlitDebugVelocity(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.DebugVelocity, MeshTopology.Triangles, 3);
		}

		public static void BlitDebugSmoothness(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.DebugSmoothness, MeshTopology.Triangles, 3);
		}

		public static void BlitDebugIBLOcclusion(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.DebugIBLOcclusion, MeshTopology.Triangles, 3);
		}
		
		public static void BlitDebugNaN(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.DebugNaN, MeshTopology.Triangles, 3);
		}

		public static void CustomBlit(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest, Material mat, int pass) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src, RenderTextureSubElement.Color);
			// MaterialManager.BlitMaterial.SetTexture(ShaderKeywordManager.MAIN_TEXTURE, src);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Triangles, 3);
		}

		public static void ScaledBlit(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src, RenderTextureSubElement.Color);
			// MaterialManager.BlitMaterial.SetTexture(ShaderKeywordManager.MAIN_TEXTURE, src);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.ScaledBlit, MeshTopology.Triangles, 3);
		}

		public static void ScaledBlitDepth(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src, RenderTextureSubElement.Depth);
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.ScaledBlit, MeshTopology.Triangles, 3);
		}
		
		public static void ScaledBlitWithDepth(this CommandBuffer cmd, RTHandle src, RenderTargetIdentifier dest, RenderTargetIdentifier depth) {
			cmd.SetGlobalTexture(ShaderKeywordManager.MAIN_TEXTURE, src);
			// MaterialManager.BlitMaterial.SetTexture(ShaderKeywordManager.MAIN_TEXTURE, src);
			cmd.SetRenderTarget(dest, depth);
			cmd.DrawProcedural(Matrix4x4.identity, MaterialManager.BlitMat, (int) BlitPass.ScaledBlit, MeshTopology.Triangles, 3);
		}

		public static void FullScreenPass(this CommandBuffer cmd, Material mat, int pass) {
			cmd.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Triangles, 3);
		}

		public static void FullScreenPass(this CommandBuffer cmd, RenderTargetIdentifier dest, Material mat, int pass, bool clearColor = false) {
			cmd.SetRenderTarget(dest, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			if (clearColor) cmd.ClearRenderTarget(false, true, Color.black);
			cmd.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Triangles, 3);
		}
		
		public static void FullScreenPassWithDepth(this CommandBuffer cmd, RenderTargetIdentifier dest, RenderTargetIdentifier depth, Material mat, int pass, bool clearColor = false, bool clearDepth = false) {
			cmd.SetRenderTarget(dest, depth);
			cmd.ClearRenderTarget(clearDepth, clearColor, Color.black);
			cmd.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Triangles, 3);
		}
	}

	public enum BlitPass {
		Blit,
		ScaledBlit,
		BlitStencil,
		DebugStencil,
		DebugVelocity,
		DebugSmoothness,
		DebugIBLOcclusion,
		DebugNaN
	}
}