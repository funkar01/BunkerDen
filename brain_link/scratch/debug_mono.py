import re
scene_path = r"i:\PG projects\BunkerDen\Assets\Assets\Scenes\BunkerScene_v2.unity"

print("Scanning for first 10 MonoBehaviour definitions:")
count = 0
with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for i, line in enumerate(f, 1):
        if "!u!114" in line:
            print(f"Line {i}: {repr(line)}")
            count += 1
            if count >= 10:
                break
