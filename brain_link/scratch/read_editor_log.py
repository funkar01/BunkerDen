import os
import shutil

log_path = os.path.expandvars(r'%LOCALAPPDATA%\Unity\Editor\Editor.log')
dest_path = r'C:\Users\bhanu\.gemini\antigravity\brain\e1ee5d7e-6286-47ff-8fa7-2271cd6f3d17\scratch\Editor_copy.log'

try:
    if os.path.exists(log_path):
        shutil.copy2(log_path, dest_path)
        print("Log copied successfully.")
        with open(dest_path, 'r', encoding='utf-8', errors='ignore') as f:
            lines = f.readlines()
            print("--- Last 100 lines of Editor.log ---")
            for line in lines[-100:]:
                print(line.strip())
    else:
        print(f"Editor.log not found at: {log_path}")
except Exception as e:
    print(f"Error: {e}")
