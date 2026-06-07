import re
import sys
import os

scene_path = r"i:\PG projects\BunkerDen\Assets\Assets\Scenes\BunkerScene_v2.unity"

# We want to associate Components with GameObjects
# First, scan the scene to build a map of:
# comp_id -> gameobject_id
# class_id -> comp_id -> properties
# gameobject_id -> name, static

go_map = {} # go_id -> {"name": "", "static": 0, "components": []}
comp_to_go = {} # comp_id -> go_id
comp_data = {} # comp_id -> {"class": "", "props": {}}

# Let's read the file and build associations
gameobject_start = re.compile(r"^--- !u!1 &(\d+)")
component_start = re.compile(r"^--- !u!(\d+) &(\d+)")
name_re = re.compile(r"^\s+m_Name:\s*(.*)$")
static_flags_re = re.compile(r"^\s+m_StaticEditorFlags:\s*(\d+)$")
go_ref_re = re.compile(r"^\s+m_GameObject:\s*\{fileID:\s*(\d+)\}")

current_go_id = None
current_comp_id = None
current_class_id = None

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        go_m = gameobject_start.match(line)
        if go_m:
            current_go_id = go_m.group(1)
            go_map[current_go_id] = {"name": "", "static": 0, "components": []}
            current_comp_id = None
            current_class_id = None
            continue
            
        comp_m = component_start.match(line)
        if comp_m:
            current_class_id = comp_m.group(1)
            current_comp_id = comp_m.group(2)
            comp_data[current_comp_id] = {"class": current_class_id, "props": {}}
            current_go_id = None
            continue
            
        if current_go_id:
            name_m = name_re.match(line)
            if name_m:
                go_map[current_go_id]["name"] = name_m.group(1).strip()
            static_m = static_flags_re.match(line)
            if static_m:
                go_map[current_go_id]["static"] = int(static_m.group(1))
                
        if current_comp_id:
            go_ref_m = go_ref_re.match(line)
            if go_ref_m:
                ref_go = go_ref_m.group(1)
                comp_to_go[current_comp_id] = ref_go
                if ref_go in go_map:
                    go_map[ref_go]["components"].append(current_comp_id)

# Now, we parse properties for Lights (class 108) and MonoBehaviours (class 114)
# Let's do a second pass to collect properties for components that are of interest.
interest_comps = {}
for cid, info in comp_data.items():
    if info["class"] in ["108", "114"]:
        interest_comps[cid] = {"class": info["class"], "props": {}}

# Property regexes for lights and additional light data
shadows_re = re.compile(r"^\s+m_Shadows:\s*(.*)$")
shadow_res_re = re.compile(r"^\s+m_ShadowResolution:\s*(.*)$")
vol_dimmer_re = re.compile(r"^\s+m_VolumetricDimmer:\s*(.*)$")
vol_shadow_re = re.compile(r"^\s+m_VolumetricShadowDimmer:\s*(.*)$")
intensity_re = re.compile(r"^\s+m_Intensity:\s*(.*)$")
use_vol_re = re.compile(r"^\s+useVolumetric:\s*(\d+)")
shadow_update_mode_re = re.compile(r"^\s+m_ShadowUpdateMode:\s*(\d+)")

current_comp_id = None
with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        comp_m = component_start.match(line)
        if comp_m:
            cid = comp_m.group(2)
            if cid in interest_comps:
                current_comp_id = cid
            else:
                current_comp_id = None
            continue
            
        if current_comp_id:
            # Check properties
            shadows_m = shadows_re.search(line)
            if shadows_m:
                interest_comps[current_comp_id]["props"]["shadows"] = shadows_m.group(1).strip()
            
            res_m = shadow_res_re.search(line)
            if res_m:
                interest_comps[current_comp_id]["props"]["shadow_res"] = res_m.group(1).strip()
                
            vol_dim_m = vol_dimmer_re.search(line)
            if vol_dim_m:
                interest_comps[current_comp_id]["props"]["vol_dimmer"] = vol_dim_m.group(1).strip()
                
            vol_sd_m = vol_shadow_re.search(line)
            if vol_sd_m:
                interest_comps[current_comp_id]["props"]["vol_shadow"] = vol_sd_m.group(1).strip()

            int_m = intensity_re.search(line)
            if int_m:
                interest_comps[current_comp_id]["props"]["intensity"] = int_m.group(1).strip()
                
            uvol_m = use_vol_re.search(line)
            if uvol_m:
                interest_comps[current_comp_id]["props"]["use_volumetric"] = uvol_m.group(1).strip()

            sum_m = shadow_update_mode_re.search(line)
            if sum_m:
                interest_comps[current_comp_id]["props"]["shadow_update_mode"] = sum_m.group(1).strip()

print("--- Lights & HDAdditionalLightData Analysis ---")
# Let's find GameObjects with Lights
for go_id, go_info in go_map.items():
    has_light = False
    light_comp_id = None
    hd_light_comp_id = None
    
    for cid in go_info["components"]:
        if cid in interest_comps:
            if interest_comps[cid]["class"] == "108":
                has_light = True
                light_comp_id = cid
            elif interest_comps[cid]["class"] == "114":
                # Let's check if it's HDAdditionalLightData by looking for HDAdditionalLightData fields
                props = interest_comps[cid]["props"]
                if "vol_dimmer" in props or "use_volumetric" in props:
                    hd_light_comp_id = cid
                    
    if has_light:
        print(f"\nGameObject: {go_info['name']} (ID: {go_id}) [Static: {go_info['static']}]")
        
        # Base Light properties
        if light_comp_id:
            lprops = interest_comps[light_comp_id]["props"]
            print(f"  Light Component: Shadows={lprops.get('shadows', 'N/A')}, Intensity={lprops.get('intensity', 'N/A')}")
            
        # HD Additional Light Data properties
        if hd_light_comp_id:
            hprops = interest_comps[hd_light_comp_id]["props"]
            print(f"  HDAdditionalLightData Component:")
            print(f"    Shadow Resolution: {hprops.get('shadow_res', 'N/A')}")
            print(f"    Volumetric Dimmer: {hprops.get('vol_dimmer', 'N/A')}")
            print(f"    Volumetric Shadow Dimmer: {hprops.get('vol_shadow', 'N/A')}")
            print(f"    Use Volumetric: {hprops.get('use_volumetric', 'N/A')}")
            print(f"    Shadow Update Mode: {hprops.get('shadow_update_mode', 'N/A')}")
        else:
            print("  No HDAdditionalLightData found.")
