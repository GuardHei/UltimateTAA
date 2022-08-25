 # Advanced Render Pipeline

 ## Rendering Layers

 | Bit   | Usage          |
 | :---- | :------------- |
 | 0     | Default        |
 | 1     | Static objects |
 | 2     | Terrain        |

 ## Stencil Bits

 ### Bits 0 *(LSB)* - 1

 Reserved for TAA

 | Mask  | Usage   |
 | :---- | :------ |
 |  00   | Static  |
 |  01   | Dynamic |
 |  10   | Alpha   |
 |  11   | Custom  |

 ### Bit 2

 Reserved to exclude pixels from static velocity calculation. Can be used as a custom bit after the velocity passes.

 1: Already calculated dynamic velocity
 
 0: Still requires the full screen static velocity pass

 ### Bits 3 - 7

 Unused

 ## GBuffer Layout

 Originally forward+ design with a thin gbuffer

 | GBuffer   | Format            | Channel R  | Channel G  | Channel B       | Channel A                    |
 | :-------- | :---------------- | :--------  | :--------  | :-------------- | :--------------------------- |
 | GBuffer 0 | RGBA16_SFloat     | Forward R  | Forward G  | Forward B       | SSS Param / TAA Anti-flicker |
 | GBuffer 1 | A2R10G10B10_UNorm | Normal X   | Normal Y   | IBL Occlusion   | Material Shadow              | 
 | GBuffer 2 | RGBA8_UNorm       | Specular R | Specular G | Specular B      | Linear Roughness             |
 | Velocity  | RG16_SNorm        | Velocity X | Velocity Y | N/A             | N/A                          |
 | Depth     | D24S8             | Depth      | N/A        | N/A             | Stencil                      |

 Switching to a deferred path with optional gbuffer 3 & 4;
 Emssive objects will be drawn again after lighting calculation is done to save bandwidth

 | GBuffer   | Format                 | Channels                                                                                      |
 | :-------- | :--------------------- | :-------------------------------------------------------------------------------------------- |
 | GBuffer 0 | RGBA8_UNorm            | RGB: Albedo Color A: Material Shadow                                                          |
 | GBuffer 1 | RGBA8_UNorm            | RGB: Oct Normal X [12b] + Normal Y [12b] A: Linear Roughness                                  | 
 | GBuffer 2 | RGBA8_UNorm            | R: Metallic G: Ambient Occlusion B: Specular Occlusion A: Object Mask [4b] + Material ID [4b] |
 | GBuffer 3 | RGBA8_UNorm            | *Clear Coat Setup* R: Clear Coat Mask G: Linear Clear Coat Roughness BA: N/A                  |
 | GBuffer 3 | RGBA8_UNorm            | *Fabric Setup* RGB: Subsurface Color A: Opacity                                               |
 | GBuffer 4 | RGBA8_UNorm            | *Fabric Setup* RGB: Sheen Color A: Opacity Mask                                               |
 | Velocity  | RG16_SNorm             | RG: Velocity                                                                                  |
 | Depth     | D24S8                  | Depth Stencil                                                                                 |

 ## Render Pass Overview

 ### Occluder Depth Stencil Prepass

    Draw depth and stencil of all occluder objects.
 
 ### Depth Stencil Prepass

    Draw depth and stencil of all the rest objects.

 ### Dynamic Velocity Pass (WIP)

    Draw the velocity of the dynamic objects, toggle stencil[2] to 1.

 ### Static Velocity Pass
    
    Draw the velocity of the static objects, using stencil[2] to kill dynamic pixels.

 ### Downsample and Dilate Velocity Pass (Roadmap)

    Downsample velocity texture to quarter resolution (half width, half height), and each pixel represents the closet velocity of the 2x2 quad.

 ### Directional Light Shadowmap Pass (Roadmap)

    Draw shadowmap of the main directional light.

 ### Forward Opaque Lighting Pass

    Shade forward opaque materials. Output lighting (direct lighting + indirect diffuse + emissive) result and SSS Param to RawColorTex. Output world space normal (Oct Quad Encoded), IBL occlusion, and material shadow to GBuffer 1. Output specular color and linear roughness to GBuffer 2.

 ### Specular Image Based Lighting Pass

    Evaluate specular IBL.

 ### Screen Space Reflection Pass (Roadmap)

    Compute screen space reflection, output mixing intensity in the alpha channel.

 ### Integrate Indirect Specular Pass

    Mix IBL and SSR results together.

### Integrate Opaque Lighting Pass

    Integrate indirect specular result back the lighting buffer, output to ColorTex.

### Skybox Pass

    Draw Skybox Pass.

### Forward Transparent Lighting Pass (Roadmap)

    Shade forward transparent materials. Evaluate specular IBL within the forward pass.

### Stop NaN Propagation Pass (WIP)

    Replace NaN, Inf, -Inf with pure black (0, 0, 0, 1).

### Resolve Temporal Antialiasing Pass (WIP)

    Resolve taa.

### Tonemap Pass

    Color grade and tonemap.