# UltimateTAA

## Roadmap

1. Setup basic lighting loop (only one directional light) - Fin.
2. Implement basic post processing pipeline - Fin.
3. Implement basic TAA - Fin.
4. Implement basic motion vector pass - Fin.
5. Implement more advanced TAA - Fin.
6. Implement more advanced materials - Fin.
7. Implement precomputed radiance transfer GI with realtime relighting - Fin.
   1. Offline capture - Fin.
   2. Runtime radiance update
      1. Single bounce - Fin.
      2. Multi bounce - Fin.
      3. Indirect shadow - Fin.
   3. Runtime irradiance prefilter - Fin.
   4. Runtime irradiance sampling - Fin.
8. Implement Cascaded Directional Shadow - 50% Fin.
   1. Cascaded shadowmap render - Fin.
   2. Cascaded hard shadow sample - Fin.
   3. Cascaded PCF soft shadow sample - WIP.
   4. Cascaded PCSS soft shadow sample - WIP.

## Preview

Dynamic Diffuse GI Test 1
https://user-images.githubusercontent.com/30316509/167318723-7e36ca47-cabe-4092-8db7-6bae51ac07b5.mp4

Dynamic Diffuse GI Test 2
https://user-images.githubusercontent.com/30316509/167318672-e426d779-b720-4a7e-9e76-68659d5fe0f7.mp4

Dynamic Diffuse GI Test 3
https://user-images.githubusercontent.com/30316509/167318664-84ab9bf6-e36a-44aa-a0f9-d17580d8d2cb.mp4


|![Damaged Helmet](https://s2.loli.net/2022/01/25/Vo3DmB1CNzSR4Yd.png)|
|:--:|
|*Damaged Helmet (glTF 2.0)*|

|![PBR Spheres](https://s2.loli.net/2022/01/25/UMAF5EV8Tzys2jk.png)|
|:--:|
|*PBR Spheres*|

|![More PBR Materials - 1](https://s2.loli.net/2022/03/21/F2UDa1fZuAm8xnv.png)|![More PBR Materials - 0](https://s2.loli.net/2022/01/25/EfDWN8nrlavX5Rc.png)|
|:--:|:--:|
|*More PBR Materials*|*More PBR Materials*|

|![Temporal Dithered Parallax Occlusion Mapping - 0](https://s2.loli.net/2022/03/21/zikSqJVCgGAtrus.png)|![Temporal Dithered Parallax Occlusion Mapping - 1](https://s2.loli.net/2022/03/21/rTpoxZqD5PFH4XI.png)|
|:--:|:--:|
|*Temporal Dithered Parallax Occlusion Mapping - 0*|*Temporal Dithered Parallax Occlusion Mapping - 1*|

|![Without Clear Coat](https://s2.loli.net/2022/03/23/MybepV257dfnHkU.png)|![With Clear Coat](https://s2.loli.net/2022/03/23/e3Il7Vtc4XCERSH.png)|
|:--:|:--:|
|*Clear Coat = 0 (Metallic = 1, Smoothness = .5)*|*Clear Coat = 1 (Metallic = 1, Smoothness = .5)*|

|![Without Anisotropy](https://s2.loli.net/2022/03/23/sQjldm6bgTtcLIv.png)|![With Anisotropy](https://s2.loli.net/2022/03/23/pVTKQrqxPHjRwOm.png)|
|:--:|:--:|
|*Anisotropy = 0*|*Anisotropy = 1*|

|![fabric_material.png](https://s2.loli.net/2022/03/24/ULAGiqzVlEet9CR.png)|![non_fabric_material.png](https://s2.loli.net/2022/03/24/wkDgsW2LdI8leK4.png)|
|:--:|:--:|
|Velvet Fabric PBR|Original PBR|

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

 ## Thin - GBuffer

 | GBuffer   | Format            | Channel R       | Channel G  | Channel B       | Channel A                    |
 | :-------- | :---------------- | :-------------- | :--------  | :-------------- | :--------------------------- |
 | GBuffer 0 | RGBA16_SFloat     | Forward R       | Forward G  | Forward B       | SSS Param / TAA Anti-flicker |
 | GBuffer 1 | R16G16_UNorm      | Normal X        | Normal Y   | N/A             | N/A                          | 
 | GBuffer 2 | RGBA8_UNorm       | Specular R      | Specular G | Specular B      | Linear Roughness             |
 | GBuffer 3 | R8_UNorm          | IBL Occlusion   | N/A        | N/A             | N/A                          | 
 | Velocity  | RG16_SNorm        | Velocity X      | Velocity Y | N/A             | N/A                          |
 | Depth     | D24S8             | Depth           | N/A        | N/A             | Stencil                      |

 ## Render Pass Overview

 ### Cascaded Shadowmap Pass

    Render the cascaded shadowmap for the main directional light (only support 1 directional light shadow at the moment).

 ### Diffuse Probe Radiance Update Pass

    Update the radiance of the diffuse probes.

 ### Diffuse Probe Irradiance Update Pass

    Prefilter the irradiance of the diffuse probes.

 ### Diffuse Probe Irradiance Padding Pass

    Add the 1-px padding of the irradiance maps for bilinear filtering.

 ### Occluder Depth Stencil Prepass

    Draw depth and stencil of all occluder objects.
 
 ### Depth Stencil Prepass

    Draw depth and stencil of all the rest objects.

 ### Dynamic Velocity Pass

    Draw the velocity of the dynamic objects, toggle stencil[2] to 1.

 ### Static Velocity Pass
    
    Draw the velocity of the static objects, using stencil[2] to kill dynamic pixels.

 ### Downsample and Dilate Velocity Pass (Roadmap)

    Downsample velocity texture to quarter resolution (half width, half height), and each pixel represents the closet velocity of the 2x2 quad.

 ### Directional Light Shadowmap Pass (Roadmap)

    Draw shadowmap of the main directional light.

 ### Forward Opaque Lighting Pass

    Shade forward opaque materials. Output lighting (direct lighting + indirect diffuse + emissive) result and SSS Param to RawColorTex. Output world space normal (Oct Quad Encoded) to GBuffer 1. Output specular color and linear roughness to GBuffer 2. Output IBL occlusion to GBuffer 3.

 ### Specular Image Based Lighting Pass

    Evaluate specular IBL.

 ### Screen Space Reflection Pass (WIP)

    Compute screen space reflection, output mixing intensity in the alpha channel.

 ### Integrate Indirect Specular Pass

    Mix IBL and SSR results together.

### Integrate Opaque Lighting Pass

    Integrate indirect specular result back the lighting buffer, output to ColorTex.

### Skybox Pass

    Draw Skybox Pass.

### Forward Transparent Lighting Pass (Roadmap)

    Shade forward transparent materials. Evaluate specular IBL within the forward pass.

### Stop NaN Propagation Pass

    Replace NaN, Inf, -Inf with pure black (0, 0, 0, 1).

### Resolve Temporal Antialiasing Pass

    Resolve taa.

### Tonemap Pass

    Color grade and tonemap.
