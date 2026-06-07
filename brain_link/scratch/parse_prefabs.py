import re
import sys
import os

scene_path = r"i:\PG projects\BunkerDen\Assets\Assets\Scenes\BunkerScene_v2.unity"
meta_cache = {}

# Recursively build a cache of GUID to file paths for .meta files
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
                        # The actual asset path is the meta path without the ".meta" extension
                        asset_path = meta_path[:-5]
                        meta_cache[guid] = asset_path
            except Exception as e:
                pass

print(f"Loaded {len(meta_cache)} GUIDs from meta files.")

# Read the scene and extract PrefabInstance source prefabs
prefab_start = re.compile(r"^--- !u!1001 &(\d+)")
source_prefab_re = re.compile(r"^\s+m_SourcePrefab:\s*\{fileID:\s*\d+,\s*guid:\s*([a-f0-9]{32}),\s*type:\s*\d+\}")
gameobject_name_re = re.compile(r"^\s+m_Name:\s*(.*)$")

prefabs_used = {}
current_prefab_id = None
in_prefab = False

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        m = prefab_start.match(line)
        if m:
            in_prefab = True
            current_prefab_id = m.group(1)
            continue
        
        if in_prefab:
            sp_m = source_prefab_re.match(line)
            if sp_m:
                guid = sp_m.group(1)
                prefabs_used[current_prefab_id] = guid
                in_prefab = False

print("\n--- Prefab Instances in Scene ---")
prefab_counts = {}
for pid, guid in prefabs_used.items():
    path = meta_cache.get(guid, f"Unknown GUID: {guid}")
    rel_path = path.replace("i:\\PG projects\\BunkerDen\\", "")
    prefab_counts[rel_path] = prefab_counts.get(rel_path, 0) + 1

for path, count in sorted(prefab_counts.items(), key=lambda x: x[1], reverse=True):
    print(f"{path}: {count} instance(s)")
