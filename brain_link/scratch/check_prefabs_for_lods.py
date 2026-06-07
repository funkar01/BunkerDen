import re
import sys
import os

prefabs = [
    r"Assets\Assets\Prefabs\BunkerWall.prefab",
    r"Assets\Assets\Prefabs\FluorescentLamp.prefab",
    r"Assets\Assets\Prefabs\AmmoBoxLarge.prefab",
    r"Assets\Assets\Prefabs\OfficeChair.prefab",
    r"Assets\Assets\Prefabs\BunkerBarrel.prefab",
    r"Assets\Assets\3DModels\FBX\Generator_V1\SM_genirator.fbx",
    r"Assets\Assets\3DModels\FBX\electric-box-44\source\electric_box_44.fbx",
    r"Assets\Assets\3DModels\FBX\Box\box.fbx",
    r"Assets\Assets\3DModels\FBX\Generator_V2\model.fbx",
    r"Assets\Assets\3DModels\FBX\Generator_V5\Generator.fbx",
    r"Assets\Assets\3DModels\FBX\electrical-breaker-panel-box-lp-model\source\Tz-ExteriorElectricBox2.fbx",
    r"Assets\Assets\3DModels\FBX\MedicalKit\clo_medical_kit.fbx",
    r"Assets\Assets\3DModels\FBX\Generator_V3\generator.fbx",
    r"Assets\Assets\3DModels\FBX\Lever\SCI-FI_Lever.fbx",
    r"Assets\Assets\3DModels\FBX\SignBoard_1\wet_floor_sign.fbx",
    r"Assets\Assets\3DModels\FBX\Generator_V4\Power Generator.fbx",
    r"Assets\Assets\3DModels\FBX\Adventure map\Map_01.FBX"
]

project_root = r"i:\PG projects\BunkerDen"

lod_group_start = re.compile(r"^--- !u!205 &")
mesh_renderer_start = re.compile(r"^--- !u!23 &")

print("--- Prefab LOD & Renderer Audit ---")

for pref in prefabs:
    path = os.path.join(project_root, pref)
    if not os.path.exists(path):
        # Try checking if it's an FBX file and if there's a meta file or if we can read the raw asset.
        # FBX files are binary, so we might not be able to read them as YAML easily.
        # But we can read their .meta files or check if they are imported with LODs.
        if pref.endswith(".fbx") or pref.endswith(".FBX"):
            meta_path = path + ".meta"
            if os.path.exists(meta_path):
                # We can check the import settings for LODs if present in meta
                has_lod = False
                with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                    if "lod" in content.lower():
                        has_lod = True
                print(f"Model File '{pref}': FBX format. LOD in meta: {has_lod}")
            else:
                print(f"Model File '{pref}': FBX format. Meta missing.")
        else:
            print(f"File '{pref}': Not found.")
        continue
        
    # It is a .prefab file, which is serialized as YAML text in Unity
    has_lod_group = False
    renderer_count = 0
    
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        for line in f:
            if lod_group_start.match(line):
                has_lod_group = True
            if mesh_renderer_start.match(line):
                renderer_count += 1
                
    print(f"Prefab '{pref}': Has LODGroup={has_lod_group}, Renderers={renderer_count}")
