import re
import sys
import os

scene_path = r"i:\PG projects\BunkerDen\Assets\Assets\Scenes\BunkerScene_v2.unity"

reflection_probe_start = re.compile(r"^--- !u!215 &(\d+)")
mode_re = re.compile(r"^\s+m_Mode:\s*(\d+)")
refresh_mode_re = re.compile(r"^\s+m_RefreshMode:\s*(\d+)")
resolution_re = re.compile(r"^\s+m_Resolution:\s*(\d+)")
gameobject_ref_re = re.compile(r"^\s+m_GameObject:\s*\{fileID:\s*(\d+)\}")

probes = []
in_probe = False
current_probe = {}

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        m = reflection_probe_start.match(line)
        if m:
            if current_probe:
                probes.append(current_probe)
            in_probe = True
            current_probe = {"id": m.group(1), "mode": None, "refresh": None, "resolution": None, "go_id": None}
            continue
            
        if in_probe:
            mode_m = mode_re.match(line)
            if mode_m:
                current_probe["mode"] = int(mode_m.group(1))
                continue
            refresh_m = refresh_mode_re.match(line)
            if refresh_m:
                current_probe["refresh"] = int(refresh_m.group(1))
                continue
            res_m = resolution_re.match(line)
            if res_m:
                current_probe["resolution"] = int(res_m.group(1))
                continue
            go_m = gameobject_ref_re.match(line)
            if go_m:
                current_probe["go_id"] = go_m.group(1)
                continue
            
            # If we hit another component or gameobject, stop
            if line.startswith("--- !u!"):
                probes.append(current_probe)
                current_probe = {}
                in_probe = False

if current_probe:
    probes.append(current_probe)

# Let's read GameObject names to match probe IDs to names
go_names = {}
go_start = re.compile(r"^--- !u!1 &(\d+)")
name_re = re.compile(r"^\s+m_Name:\s*(.*)$")
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
            nm = name_re.match(line)
            if nm:
                go_names[cur_go_id] = nm.group(1).strip()
                in_go = False
            if line.startswith("--- !u!"):
                in_go = False

# Reflection Probe Modes: 0 = Baked, 1 = Realtime, 2 = Custom
# Refresh Modes: 0 = On Awake, 1 = Every Frame, 2 = Via Scripting
modes = {0: "Baked", 1: "Realtime", 2: "Custom"}
refresh_modes = {0: "On Awake", 1: "Every Frame", 2: "Via Scripting"}

print(f"Total Reflection Probes found in file: {len(probes)}")
realtime_count = 0
baked_count = 0
custom_count = 0

for p in probes:
    go_name = go_names.get(p.get("go_id"), "Unknown GameObject")
    m_val = p.get("mode")
    m_str = modes.get(m_val, f"Unknown ({m_val})")
    
    r_val = p.get("refresh")
    r_str = refresh_modes.get(r_val, f"Unknown ({r_val})")
    
    res = p.get("resolution", "Default")
    
    print(f"Probe '{go_name}' (ID {p['id']}): Mode={m_str}, Refresh={r_str}, Resolution={res}")
    
    if m_val == 0:
        baked_count += 1
    elif m_val == 1:
        realtime_count += 1
    elif m_val == 2:
        custom_count += 1

print("\n--- Summary ---")
print(f"Baked: {baked_count}")
print(f"Realtime: {realtime_count}")
print(f"Custom: {custom_count}")
