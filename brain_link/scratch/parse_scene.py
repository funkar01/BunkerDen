import re
import sys
import os

scene_path = r"i:\PG projects\BunkerDen\Assets\Assets\Scenes\BunkerScene_v2.unity"

if not os.path.exists(scene_path):
    print(f"Error: Scene file not found at {scene_path}")
    sys.exit(1)

print(f"Parsing scene: {scene_path}")

# Regexes
gameobject_start = re.compile(r"^--- !u!1 &(\d+)")
component_start = re.compile(r"^--- !u!(\d+) &(\d+)")
name_re = re.compile(r"^\s+m_Name:\s*(.*)$")
static_flags_re = re.compile(r"^\s+m_StaticEditorFlags:\s*(\d+)$")
light_mode_re = re.compile(r"^\s+m_Lightmapping:\s*(\d+)$")
light_type_re = re.compile(r"^\s+m_Type:\s*(\d+)$")

gameobjects = {}
components = []
current_go_id = None
current_go_name = ""
current_go_static = 0

class_counts = {}
lights = []
reflection_probes = []
renderers_count = 0
mesh_filters_count = 0
lod_groups_count = 0
decals_count = 0
volumes_count = 0

# Class ID mapping
# 1: GameObject
# 4: Transform
# 23: MeshRenderer
# 33: MeshFilter
# 108: Light
# 205: LODGroup
# 215: ReflectionProbe
# 114: MonoBehaviour (scripts, Volumes, Decals, etc.)

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    in_gameobject = False
    in_light = False
    light_data = {}
    
    for line in f:
        # Detect GameObject
        go_match = gameobject_start.match(line)
        if go_match:
            in_gameobject = True
            current_go_id = go_match.group(1)
            current_go_name = ""
            current_go_static = 0
            continue
            
        # Detect other components
        comp_match = component_start.match(line)
        if comp_match:
            in_gameobject = False
            class_id = comp_match.group(1)
            comp_id = comp_match.group(2)
            class_counts[class_id] = class_counts.get(class_id, 0) + 1
            
            if class_id == '23':
                renderers_count += 1
            elif class_id == '33':
                mesh_filters_count += 1
            elif class_id == '205':
                lod_groups_count += 1
            elif class_id == '108':
                in_light = True
                light_data = {"id": comp_id, "type": None, "mode": None}
            else:
                in_light = False
            continue
            
        if in_gameobject:
            name_m = name_re.match(line)
            if name_m:
                current_go_name = name_m.group(1).strip()
            static_m = static_flags_re.match(line)
            if static_m:
                current_go_static = int(static_m.group(1))
                gameobjects[current_go_id] = {
                    "name": current_go_name,
                    "static": current_go_static
                }
                
        if in_light:
            type_m = light_type_re.match(line)
            if type_m:
                light_data["type"] = int(type_m.group(1))
            mode_m = light_mode_re.match(line)
            if mode_m:
                light_data["mode"] = int(mode_m.group(1))
                lights.append(light_data)
                in_light = False

print(f"Total GameObjects parsed: {len(gameobjects)}")
static_go_count = sum(1 for go in gameobjects.values() if go["static"] > 0)
print(f"Static GameObjects (any flags): {static_go_count}")
print(f"Dynamic GameObjects: {len(gameobjects) - static_go_count}")

# Class ID mapping details
class_names = {
    "1": "GameObject",
    "4": "Transform",
    "23": "MeshRenderer",
    "33": "MeshFilter",
    "108": "Light",
    "114": "MonoBehaviour (Scripts/Volumes/Decals)",
    "205": "LODGroup",
    "215": "ReflectionProbe",
    "64": "MeshCollider",
    "65": "BoxCollider",
    "135": "SphereCollider",
    "136": "CapsuleCollider",
}

print("\n--- Component Counts ---")
for cid, cnt in sorted(class_counts.items(), key=lambda x: x[1], reverse=True):
    name = class_names.get(cid, f"Unknown Class {cid}")
    print(f"{name}: {cnt}")

print("\n--- Lights Summary ---")
# Light Type: 0 = Spot, 1 = Directional, 2 = Point, 3 = Area
# Lightmapping mode: 0 = Realtime, 1 = Mixed, 2 = Baked
light_types = {0: "Spot", 1: "Directional", 2: "Point", 3: "Area"}
light_modes = {0: "Realtime", 1: "Mixed", 2: "Baked"}

for l in lights:
    t_str = light_types.get(l.get("type"), f"Unknown type {l.get('type')}")
    m_str = light_modes.get(l.get("mode"), f"Unknown mode {l.get('mode')}")
    print(f"Light ID {l['id']}: Type={t_str}, Mode={m_str}")

print(f"\nTotal Lights Found: {len(lights)}")
print(f"Total MeshRenderers: {renderers_count}")
print(f"Total MeshFilters: {mesh_filters_count}")
print(f"Total LODGroups: {lod_groups_count}")
