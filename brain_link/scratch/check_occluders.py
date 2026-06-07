import re
import os

scene_path = r"i:\PG projects\BunkerDen\Assets\Assets\Scenes\BunkerScene_v2.unity"
meta_cache = {}

print("Building GUID cache...")
for root, dirs, files in os.walk(r"i:\PG projects\BunkerDen\Assets"):
    for file in files:
        if file.endswith(".meta"):
            meta_path = os.path.join(root, file)
            try:
                with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                    guid_m = re.search(r"guid:\s*([a-f0-9]{32})", content)
                    if guid_m:
                        guid = guid_m.group(1)
                        meta_cache[guid] = meta_path[:-5]
            except Exception as e:
                pass

print(f"Loaded {len(meta_cache)} GUIDs from meta files.")

# Match PrefabInstances and their properties
prefab_start = re.compile(r"^--- !u!1001 &(\d+)")
source_prefab_re = re.compile(r"m_SourcePrefab:.*guid:\s*([a-f0-9]{32})")
target_re = re.compile(r"target:\s*\{fileID:\s*-?\d+,\s*guid:\s*[a-f0-9]{32},\s*type:\s*\d+\}")
property_re = re.compile(r"propertyPath:\s*(.*)$")
value_re = re.compile(r"value:\s*(.*)$")

# We want to collect all static flags overridden on GameObjects of prefab instances.
# A prefab instance can have many modifications.
# Each modification is a block:
# - target: ...
#   propertyPath: ...
#   value: ...
# We need to associate the static flags override with the prefab instance.
# Since a prefab instance might instantiate a complex hierarchy, it can override static flags of MULTIPLE gameobjects in the prefab!
# So we need to store for each prefab instance, the overridden flags.

prefab_source = {} # prefab_instance_id -> source_prefab_path
prefab_flags = {} # prefab_instance_id -> list of (target_go_id, flags)
prefab_names = {} # prefab_instance_id -> list of (target_go_id, name)

current_prefab_id = None
in_prefab = False

target_go_id = None
prop_name = None

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        m = prefab_start.match(line)
        if m:
            in_prefab = True
            current_prefab_id = m.group(1)
            prefab_flags[current_prefab_id] = []
            prefab_names[current_prefab_id] = []
            continue
        if in_prefab:
            if line.startswith("--- !u!Delta") or line.startswith("--- !u!"):
                # We reached next block
                in_prefab = False
                current_prefab_id = None
                continue
                
            sp = source_prefab_re.search(line)
            if sp:
                guid = sp.group(1)
                prefab_source[current_prefab_id] = meta_cache.get(guid, f"Unknown GUID: {guid}")
                continue
                
            # Parse modifications
            # Format:
            # - target: {fileID: 2685133069501156306, guid: 4c585f83aa4377f40950abe38e0e813e, type: 3}
            #   propertyPath: m_StaticEditorFlags
            #   value: 85
            t_m = re.search(r"target:\s*\{fileID:\s*(-?\d+),", line)
            if t_m:
                target_go_id = t_m.group(1)
                prop_name = None
                continue
                
            if target_go_id:
                p_m = property_re.search(line)
                if p_m:
                    prop_name = p_m.group(1).strip()
                    continue
                v_m = value_re.search(line)
                if v_m:
                    val = v_m.group(1).strip()
                    if prop_name == "m_StaticEditorFlags":
                        prefab_flags[current_prefab_id].append((target_go_id, int(val)))
                    elif prop_name == "m_Name":
                        prefab_names[current_prefab_id].append((target_go_id, val))
                    # Reset after we see the value
                    target_go_id = None
                    prop_name = None

print("\n--- Structural Static Analysis ---")
occluder_structures = 0
non_occluder_structures = []

for pid, path in prefab_source.items():
    prefab_base_name = os.path.basename(path)
    
    # We want to check if this prefab is a structural element (walls, floors, ceiling, pillars)
    is_structural = False
    if "wall" in prefab_base_name.lower() or "floor" in prefab_base_name.lower() or "ceiling" in prefab_base_name.lower() or "map" in prefab_base_name.lower() or "structure" in prefab_base_name.lower() or "pillar" in prefab_base_name.lower() or "stairs" in prefab_base_name.lower():
        is_structural = True
        
    # Get the overridden names for this prefab instance
    overridden_name = None
    if prefab_names.get(pid):
        # Just take the first name override or find the one corresponding to the root GameObject
        overridden_name = prefab_names[pid][0][1]
    
    display_name = overridden_name if overridden_name else prefab_base_name
    
    # Check flags
    flags_list = prefab_flags.get(pid, [])
    if is_structural:
        # If we have static flags overrides
        if flags_list:
            for target_id, flags in flags_list:
                is_occluder = (flags & 2) != 0
                if is_occluder:
                    occluder_structures += 1
                else:
                    non_occluder_structures.append((display_name, prefab_base_name, flags))
        else:
            # If no override list is found in prefab instance, it uses the prefab's default flags.
            # We already know BunkerWall.prefab has static flags = 18 (which has Occluder Static = 2).
            # So if it has no overrides, it defaults to being an occluder!
            occluder_structures += 1

print(f"Structural Prefab Instances marked as Occluders: {occluder_structures}")
print(f"Structural Prefab Instances NOT marked as Occluders: {len(non_occluder_structures)}")

if non_occluder_structures:
    print("\n--- Structural Prefab Instances missing Occluder flag (overridden to non-occluder in scene): ---")
    for name, p_name, flags in non_occluder_structures[:30]:
        print(f"  Instance Name: '{name}' | Prefab: {p_name} | Overridden Flags: {flags}")
    if len(non_occluder_structures) > 30:
        print(f"  ... and {len(non_occluder_structures) - 30} more.")
