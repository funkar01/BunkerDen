import re
import os

project_root = r"i:\PG projects\BunkerDen\Assets"
texture_exts = [".png", ".jpg", ".tga", ".tif", ".tiff", ".exr", ".hdr"]

fixed_normals = []
fixed_masks = []

print("Auditing and fixing texture meta files...")

for root, dirs, files in os.walk(project_root):
    for file in files:
        ext = os.path.splitext(file)[1].lower()
        if ext in texture_exts:
            asset_path = os.path.join(root, file)
            meta_path = asset_path + ".meta"
            
            if os.path.exists(meta_path):
                file_lower = file.lower()
                is_normal = "normal" in file_lower or "_n." in file_lower or "_n_" in file_lower or "nrm" in file_lower
                is_mask = "orm" in file_lower or "mask" in file_lower or "metallic" in file_lower or "roughness" in file_lower or "ao." in file_lower or "occlusion" in file_lower
                
                if is_normal or is_mask:
                    try:
                        with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
                            lines = f.readlines()
                        
                        modified = False
                        new_lines = []
                        
                        for line in lines:
                            new_line = line
                            
                            if is_normal:
                                # Fix textureType: 0 -> textureType: 1
                                if "textureType:" in line:
                                    val = line.split(":")[1].strip()
                                    if val == "0":
                                        new_line = line.replace("textureType: 0", "textureType: 1")
                                        modified = True
                                        
                                # Fix sRGBTexture: 1 -> sRGBTexture: 0 (Normal maps must be linear)
                                if "sRGBTexture:" in line:
                                    val = line.split(":")[1].strip()
                                    if val == "1":
                                        new_line = line.replace("sRGBTexture: 1", "sRGBTexture: 0")
                                        modified = True
                                        
                            if is_mask:
                                # Fix sRGBTexture: 1 -> sRGBTexture: 0 (Mask/Data maps must be linear)
                                if "sRGBTexture:" in line:
                                    val = line.split(":")[1].strip()
                                    if val == "1":
                                        new_line = line.replace("sRGBTexture: 1", "sRGBTexture: 0")
                                        modified = True
                            
                            new_lines.append(new_line)
                            
                        if modified:
                            with open(meta_path, 'w', encoding='utf-8') as f:
                                f.writelines(new_lines)
                                
                            rel_path = asset_path.replace(project_root + "\\", "")
                            if is_normal:
                                fixed_normals.append(rel_path)
                            else:
                                fixed_masks.append(rel_path)
                                
                    except Exception as e:
                        print(f"Error processing {meta_path}: {e}")

print("\n=== TEXTURE REPAIR COMPLETED ===")
print(f"Successfully fixed {len(fixed_normals)} Normal Maps:")
for path in fixed_normals:
    print(f"  [FIXED NORMAL] {path}")

print(f"\nSuccessfully fixed {len(fixed_masks)} Mask/Data Maps:")
for path in fixed_masks:
    print(f"  [FIXED MASK] {path}")
