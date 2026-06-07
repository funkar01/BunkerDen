import re
import os

scene_path = r"i:\PG projects\BunkerDen\Assets\Assets\Scenes\BunkerScene_v2.unity"

if not os.path.exists(scene_path):
    print(f"Error: Scene file not found at {scene_path}")
    sys.exit(1)

print("Modifying scene lights in BunkerScene_v2.unity...")

# Script GUID for HDAdditionalLightData is 7a68c43fe1f2a47cfa234b5eeaa98012
light_data_guid = "7a68c43fe1f2a47cfa234b5eeaa98012"

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    lines = f.readlines()

new_lines = []
in_mb = False
is_light_data = False
current_mb_id = None
modified_count = 0

mb_start = re.compile(r"^--- !u!114 &(\d+)")
script_re = re.compile(r"m_Script:.*guid:\s*([a-f0-9]{32})")
shadow_update_re = re.compile(r"^(\s+m_ShadowUpdateMode:)\s*0")

for line in lines:
    new_line = line
    m = mb_start.match(line)
    if m:
        in_mb = True
        is_light_data = False
        current_mb_id = m.group(1)
        new_lines.append(line)
        continue
        
    if in_mb:
        if line.startswith("--- !u!"):
            in_mb = False
            is_light_data = False
            current_mb_id = None
            new_lines.append(line)
            continue
            
        s_m = script_re.search(line)
        if s_m:
            guid = s_m.group(1)
            if guid == light_data_guid:
                is_light_data = True
                
        if is_light_data:
            sh_m = shadow_update_re.match(line)
            if sh_m:
                new_line = sh_m.group(1) + " 1\n"
                modified_count += 1
                is_light_data = False # reset after replacement to prevent duplicates in block
                
    new_lines.append(new_line)

if modified_count > 0:
    with open(scene_path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    print(f"Successfully modified {modified_count} HDAdditionalLightData component(s) to use Cached/On Demand shadows.")
else:
    print("No changes made. Check if the scene already has On Demand shadows set.")
