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

 ### Bits 2 - 7

 Unused

 ## Thin - GBuffer

 | GBuffer   | Format        | Channel R  | Channel G  | Channel B  | Channel A        |
 | :-------- | :------------ | :--------  | :--------  | :--------  | :--------------- |
 | GBuffer 0 | RGBA16_SFloat | Forward R  | Forward G  | Forward B  | TAA Anti-flicker |
 | GBuffer 1 | RG16_SNorm    | Normal X   | Normal Y   | N/A        | N/A              | 
 | GBuffer 2 | RGBA8_UNorm   | Specular R | Specular G | Specular B | Roughness        |
 | Velocity  | RG16_SNorm    | Velocity X | Velocity Y | N/A        | N/A              |
 | Depth     | D24S8         | Depth      | Depth      | Depth      | Stencil          |

 ## Render Pass Overview

 ### Static Depth Stencil Prepass

    Draw depth and stencil of all static objects.
 
 ### Dynamic Depth Stencil Prepass with Motion Vector

    Draw depth and stencil of all dynamic objects. For certain materials, output motion vectors to the velocity texture.

 ### Static Motion Vector Pass
    
    Draw motion vectors of the static objects, using stencil[1:0] to kill dynamic pixels.

 ### Directional Light Shadowmap Pass

    Draw shadowmap of the main directional light.
 
 ### Forward Lighting Pass

    Shade forward materials. Output lighting result to GBuffer 0. Output normalized normals (Oct Quad Encode) to GBuffer 1. Output specular color and roughness to GBuffer 2.

 ###
