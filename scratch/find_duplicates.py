import os
import re
from collections import defaultdict

def find_methods(directory):
    method_pattern = re.compile(r'(public|private|protected|internal)\s+(static\s+)?([\w\<\>\[\]]+)\s+(\w+)\s*\(')
    methods = defaultdict(list)

    for root, dirs, files in os.walk(directory):
        if '.git' in dirs:
            dirs.remove('.git')
        if 'bin' in dirs:
            dirs.remove('bin')
        if 'obj' in dirs:
            dirs.remove('obj')
            
        for file in files:
            if file.endswith('.cs'):
                path = os.path.join(root, file)
                try:
                    with open(path, 'r', encoding='utf-8') as f:
                        for line_num, line in enumerate(f, 1):
                            match = method_pattern.search(line)
                            if match:
                                method_name = match.group(4)
                                # Ignore common names like Main, Seed, etc.
                                if method_name in ['Main', 'Seed', 'AddServices', 'AddDataServices', 'AddDbContextData', 'InsertAsync', 'UpdateAsync', 'DeleteAsync', 'GetAsync', 'GetAllAsync']:
                                    continue
                                methods[method_name].append((path, line_num, line.strip()))
                except Exception as e:
                    print(f"Error reading {path}: {e}")

    return methods

def main():
    directory = r'c:\Projects\ZynstormECFPlatform'
    methods = find_methods(directory)
    
    duplicates = {name: locs for name, locs in methods.items() if len(locs) > 1}
    
    with open('duplicate_methods.txt', 'w', encoding='utf-8') as f:
        for name, locs in sorted(duplicates.items()):
            f.write(f"Method: {name}\n")
            for path, line, content in locs:
                f.write(f"  {path}:{line} -> {content}\n")
            f.write("\n")

if __name__ == "__main__":
    main()
