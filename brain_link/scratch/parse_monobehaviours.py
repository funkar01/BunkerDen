import re
import sys
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
                        asset_path = meta_path[:-5]
                        meta_cache[guid] = asset_path
            except Exception as e:
                pass

print(f"Loaded {len(meta_cache)} GUIDs from meta files.")

mb_start = re.compile(r"^--- !u!114 &(\d+)")
script_re = re.compile(r"m_Script:.*guid:\s*([a-f0-9]{32})")
go_ref_re = re.compile(r"m_GameObject:.*fileID:\s*(\d+)")

mb_instances = {}
current_mb_id = None
in_mb = False

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        m = mb_start.match(line)
        if m:
            in_mb = True
            current_mb_id = m.group(1)
            mb_instances[current_mb_id] = {"script_guid": None, "go_id": None}
            continue
        
        if in_mb:
            if line.startswith("--- !u!"):
                in_mb = False
                continue
                
            s_m = script_re.search(line)
            if s_m:
                mb_instances[current_mb_id]["script_guid"] = s_m.group(1)
                
            go_m = go_ref_re.search(line)
            if go_m:
                mb_instances[current_mb_id]["go_id"] = go_m.group(1)

# Read GameObject names
go_names = {}
go_start = re.compile(r"^--- !u!1 &(\d+)")
name_re = re.compile(r"m_Name:\s*(.*)$")
in_go = False
cur_go_id = None

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        m = go_start.match(line)
        if m:
            in_go = True
            cur_go_id = m.group(1)
            continue
        if in_go:
            if line.startswith("--- !u!"):
                in_go = False
                continue
            nm = name_re.search(line)
            if nm:
                go_names[cur_go_id] = nm.group(1).strip()

script_counts = {}
package_scripts = {}

for mbid, info in mb_instances.items():
    guid = info["script_guid"]
    if not guid:
        continue
        
    if guid in meta_cache:
        path = meta_cache[guid]
        script_counts[path] = script_counts.get(path, 0) + 1
    else:
        package_scripts[guid] = package_scripts.get(guid, 0) + 1

print("\n--- Project Asset Script Counts in Scene ---")
for path, count in sorted(script_counts.items(), key=lambda x: x[1], reverse=True):
    rel_path = path.replace("i:\\PG projects\\BunkerDen\\", "")
    print(f"{rel_path}: {count} instance(s)")

print("\n--- Package/Built-in Script Counts in Scene ---")
guid_names = {
    "7a68c43fe1f2a47cfa234b5eeaa98012": "HDAdditionalLightData",
    "ac0b09e7857660247b1477e93731de29": "CinemachineVirtualCamera",
    "17315e2eb582b1442a8b9419139f4ff6": "Volume",
    "f2ef3abda873f1b4c8d5b128532454b5": "HDAdditionalCameraData",
    "6df677d2d31215b498fcc7fae66e60b2": "CinemachineBrain",
    "2b9b7e77a28e4694488db9f96b921315": "DecalProjector",
}

for guid, count in sorted(package_scripts.items(), key=lambda x: x[1], reverse=True):
    name = guid_names.get(guid, f"Unknown Package Script (GUID: {guid})")
    print(f"{name}: {count} instance(s)")
