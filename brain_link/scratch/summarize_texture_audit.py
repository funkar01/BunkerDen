import re
import os

project_root = r"i:\PG projects\BunkerDen\Assets"
texture_exts = [".png", ".jpg", ".tga", ".tif", ".tiff", ".exr", ".hdr"]

normal_issues = []
mask_issues = []
correct_normals = []
correct_masks = []

texture_type_re = re.compile(r"textureType:\s*(\d+)")
srgb_re = re.compile(r"sRGBTexture:\s*(\d+)")

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
                            content = f.read()
                            
                            t_type_m = texture_type_re.search(content)
                            srgb_m = srgb_re.search(content)
                            
                            t_type = int(t_type_m.group(1)) if t_type_m else None
                            srgb = int(srgb_m.group(1)) if srgb_m else None
                            
                            rel_path = asset_path.replace(project_root + "\\", "")
                            
                            if is_normal:
                                if t_type != 1:
                                    normal_issues.append((rel_path, t_type, srgb))
                                else:
                                    correct_normals.append(rel_path)
                                    
                            if is_mask:
                                if srgb != 0:
                                    mask_issues.append((rel_path, t_type, srgb))
                                else:
                                    correct_masks.append(rel_path)
                    except Exception as e:
                        pass

print("=== TEXTURE IMPORT SETTINGS SUMMARY ===")
print(f"Total Normal Maps analyzed: {len(normal_issues) + len(correct_normals)}")
print(f"  - Correctly imported as Normal Map: {len(correct_normals)}")
print(f"  - INCORRECTLY imported as Default Texture: {len(normal_issues)}")

print(f"\nTotal Mask/Data Maps analyzed: {len(mask_issues) + len(correct_masks)}")
print(f"  - Correctly imported with Linear Color (sRGB=0): {len(correct_masks)}")
print(f"  - INCORRECTLY imported with sRGB Color (sRGB=1): {len(mask_issues)}")

print("\n--- Top 10 Incorrect Normal Maps (by size/importance) ---")
for path, t_type, srgb in normal_issues[:10]:
    size_mb = round(os.path.getsize(os.path.join(project_root, path)) / 1024 / 1024, 2)
    print(f"  {path} ({size_mb} MB) | Type: {t_type} | sRGB: {srgb}")

print("\n--- Top 10 Incorrect Mask Maps (by size/importance) ---")
for path, t_type, srgb in mask_issues[:10]:
    size_mb = round(os.path.getsize(os.path.join(project_root, path)) / 1024 / 1024, 2)
    print(f"  {path} ({size_mb} MB) | sRGB: {srgb} | Type: {t_type}")
