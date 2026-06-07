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
profile_re = re.compile(r"sharedProfile:.*guid:\s*([a-f0-9]{32})")
go_ref_re = re.compile(r"m_GameObject:.*fileID:\s*(\d+)")
is_global_re = re.compile(r"m_IsGlobal:\s*(\d+)")
weight_re = re.compile(r"m_Weight:\s*(.*)$")

volumes = []
current_mb_id = None
in_mb = False
cur_volume = {}

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        m = mb_start.match(line)
        if m:
            if cur_volume:
                volumes.append(cur_volume)
            in_mb = True
            current_mb_id = m.group(1)
            cur_volume = {"id": current_mb_id, "go_id": None, "profile_guid": None, "is_global": None, "weight": None}
            continue
            
        if in_mb:
            if line.startswith("--- !u!"):
                if cur_volume:
                    volumes.append(cur_volume)
                cur_volume = {}
                in_mb = False
                continue
                
            p_m = profile_re.search(line)
            if p_m:
                cur_volume["profile_guid"] = p_m.group(1)
                continue
            go_m = go_ref_re.search(line)
            if go_m:
                cur_volume["go_id"] = go_m.group(1)
                continue
            glob_m = is_global_re.search(line)
            if glob_m:
                cur_volume["is_global"] = int(glob_m.group(1))
                continue
            wt_m = weight_re.search(line)
            if wt_m:
                cur_volume["weight"] = wt_m.group(1).strip()
                continue

if cur_volume:
    volumes.append(cur_volume)

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

print("\n--- Volumes Found in Scene (using sharedProfile) ---")
valid_volumes = 0
for v in volumes:
    if v.get("profile_guid"):
        valid_volumes += 1
        go_name = go_names.get(v.get("go_id"), f"Unknown GameObject (ID: {v.get('go_id')})")
        profile_path = meta_cache.get(v["profile_guid"], f"Unknown Profile GUID: {v['profile_guid']}")
        rel_profile = profile_path.replace("i:\\PG projects\\BunkerDen\\", "")
        
        is_glob_str = "Global" if v.get("is_global") == 1 else "Local"
        weight = v.get("weight", "1")
        
        print(f"Volume '{go_name}' (ID: {v['id']}): Type={is_glob_str}, Weight={weight}")
        print(f"  Profile: {rel_profile}")

print(f"\nTotal Volumes with Profiles: {valid_volumes}")
print(f"Total MonoBehaviour Blocks scanned: {len(volumes)}")
