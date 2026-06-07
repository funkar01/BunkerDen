import os
import re

log_path = os.path.expandvars(r"%LOCALAPPDATA%\Unity\Editor\Editor.log")

if not os.path.exists(log_path):
    print("Editor.log not found.")
    sys.exit(1)

print(f"Scanning Unity log: {log_path}\n")

# Keywords of interest
keywords = [
    "shader error", 
    "compilation failed", 
    "vfx", 
    "visualeffect", 
    "exception", 
    "nullreference", 
    "failed to compile",
    "culling",
    "render-pipelines"
]

log_entries = []

with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
    for i, line in enumerate(f, 1):
        line_lower = line.lower()
        if any(kw in line_lower for kw in keywords):
            # Exclude some very common harmless package cache logs
            if "ai.assistant" in line_lower or "licensing" in line_lower:
                continue
            log_entries.append((i, line.strip()))

print(f"Found {len(log_entries)} relevant log entries.")
for line_num, entry in log_entries[-50:]:  # Print last 50 entries
    print(f"Line {line_num}: {entry}")
