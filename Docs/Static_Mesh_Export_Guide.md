# GLTF Static Mesh Export - 3D Printing Guide

## What's New

Automatic enhancements to static mesh export:
- **Vertex validation** - Normals/tangents validated and corrected
- **Alpha masks** - Baked into diffuse as RGBA texture  
- **PBR materials** - Smart setup with automatic alpha modes
- **Full textures** - Emissive, Occlusion, Normal, Metallic/Roughness

**All automatic, zero configuration.**

---

## Quick Start

### Export
1. Select model → Export GLTF Static
2. Choose output folder
3. All optimizations applied automatically

### Output
```
model.gltf
model.bin
texture_diffuse.png
texture_diffuse_with_alpha.png    (if masked)
texture_normal.png
texture_occlusion.png
```

---

## Blender Workflow

### Import
```
File → Import → glTF 2.0 
Select model → Import glTF 2.0
```

### Setup Transparency (If Needed)
1. Select material
2. Blend Mode → "Alpha Clip" (sharp) or "Blend" (soft)
3. Adjust Alpha Clip Threshold (default 0.5)

### Prepare for Printing
```
1. Check scale (Object Properties → Scale)
2. Object → Transform → Apply All Transforms
3. Mesh → Cleanup → Smart Cleanup
   ✓ Degenerate Faces
   ✓ Unused Vertices
```

### Export to Slicer
**STL (most common):**
```
File → Export As → Stereolithography (.stl)
Export
```

**3MF (better):**
```
File → Export As → 3D Manufacturing Format (.3mf)
Export
```

---

## Features

### Vertex Validation
- Zero normals → Safe default (0, 0, 1)
- Non-unit normals → Auto-normalized
- Invalid tangents → Generated perpendicular to normal
- Bad handedness → Fixed to ±1

**Result:** Clean rendering, no artifacts

### Alpha Masks
- **Detects:** Diffuse + mask textures together
- **Combines:** RGB + grayscale into RGBA
- **Exports:** `texture_diffuse_with_alpha.png`
- **Sets:** Material to MASK mode

**Result:** Transparent parts (capes, fur, wings) import correctly

### Smart Materials
**Detection:**
- By texture type (has mask)
- By material name (cape, fur, wing, feather, hair, foliage, leaf, chain)
- By texture filename patterns

**Modes:**
- **OPAQUE** - Solid (default)
- **MASK** - Sharp edges (capes, leaves)
- **BLEND** - Soft transparency

### Textures Exported
| Type | Format | Color Space |
|------|--------|-------------|
| Diffuse | PNG RGB | sRGB |
| Diffuse+Alpha | PNG RGBA | sRGB |
| Normal | PNG RGB | Linear |
| Metallic/Roughness | PNG RGB | Linear |
| Occlusion | PNG RGB | Linear |
| Emissive | PNG RGB | sRGB |

---

## Troubleshooting

### Geometry Distorted
```
Mesh → Normals → Recalculate
Object → Transform → Apply All Transforms
Check scale: 1, 1, 1
```

### Alpha Not Showing
```
1. Material Properties → Blend Mode: "Alpha Clip"
2. Viewport: Material Preview (Z key)
3. Adjust Alpha Clip Threshold (0.5 default)
```

### Normal Maps Wrong
```
Image Texture (normal):
  ✓ Color Space: Linear (NOT sRGB!)
Principled BSDF:
  ✓ Normal input: Connected
```

### Slicer Errors
```
Mesh → Cleanup → Smart Cleanup
(Run all options)
Export fresh STL
Try again
```

### Wrong Size
```
Press S (scale)
Type new scale (2 = double, 0.5 = half)
Press Enter
Object → Transform → Apply All Transforms
```

---

## Technical

### Normal Validation
```
1. length² < 0.0001 → Use (0,0,1)
2. |length² - 1.0| > 0.001 → Normalize
Result: Valid unit-length normal
```

### Tangent Validation  
```
1. length² < 0.0001 → Generate perpendicular
2. |length² - 1.0| > 0.001 → Normalize
3. W component → Ensure ±1
Result: Valid tangent with handedness
```

### Alpha Combining
```
Input: Diffuse DDS + Mask DDS
1. Convert to bitmaps
2. Take diffuse RGB
3. Use mask grayscale as alpha
4. Save PNG RGBA
Output: texture_diffuse_with_alpha.png
```

---

---

## Material Detection Examples

**"character_cape_Material"** → Contains "cape" → MASK mode  
**"wolf_fur_Material"** → Contains "fur" → MASK mode  
**"dragon_wing_Material"** → Contains "wing" → MASK mode  
**Diffuse + Mask textures** → Both present → MASK mode  
**Diffuse only** → No mask → OPAQUE mode  

---

## Color Space

Set in Blender Image Texture nodes:

| Texture | Color Space |
|---------|-------------|
| Diffuse/Color | sRGB |
| Normal | Linear |
| Metallic/Roughness | Linear |
| Occlusion | Linear |
| Emissive | sRGB |

---

## Files Modified

- `GltfStaticMeshBuilder.cs` - Mesh building + validation
- `GltfTextureHandler.cs` - Texture handling + combining  
- `AlphaMaskCombiner.cs` - Diffuse + mask merging

---

**All improvements automatic and transparent. Nothing breaks existing workflows.**
