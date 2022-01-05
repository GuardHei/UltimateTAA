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

 Reserved to exclude pixels from static velocity calculation. Can be used as custom bit after the velocity passes.

 1: Already calculated dynamic velocity
 0: Still requires the full screen static velocity pass

 ### Bits 3 - 7

 Unused

 ## Thin - GBuffer

 | GBuffer   | Format           | Channel R  | Channel G  | Channel B  | Channel A                             |
 | :-------- | :--------------- | :--------  | :--------  | :--------  | :------------------------------------ |
 | GBuffer 0 | RGBA16_SFloat    | Forward R  | Forward G  | Forward B  | Specular Occlusion / TAA Anti-flicker |
 | GBuffer 1 | R11G11B10_UFloat | Normal X   | Normal Y   | Normal Z   | N/A                                   | 
 | GBuffer 2 | RGBA8_UNorm      | Specular R | Specular G | Specular B | Linear Roughness                      |
 | Velocity  | RG16_SNorm       | Velocity X | Velocity Y | N/A        | N/A                                   |
 | Depth     | D24S8            | Depth      | Depth      | Depth      | Stencil                               |

 ## Render Pass Overview

 ### Occluder Depth Stencil Prepass

    Draw depth and stencil of all occluder objects.
 
 ### Depth Stencil Prepass

    Draw depth and stencil of all the rest objects.

 ### Dynamic Velocity Pass (WIP)

    Draw the velocity of the dynamic objects, toggle stencil[2] to 1.

 ### Static Velocity Pass (Roadmap)
    
    Draw the velocity of the static objects, using stencil[2] to kill dynamic pixels.

 ### Directional Light Shadowmap Pass (Roadmap)

    Draw shadowmap of the main directional light.

 ### Forward Opaque Lighting Pass

    Shade forward opaque materials. Output lighting (direct lighting + indirect diffuse + emissive) result and specular occlusion to RawColorTex. Output world space normal to GBuffer 1. Output specular color and linear roughness to GBuffer 2.

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

### Resolve Temporal Antialiasing Pass (WIP)

   Resolve taa.

### Tonemap Pass

   Color grade and tonemap.