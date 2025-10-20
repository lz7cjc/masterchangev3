#!/usr/bin/env python3
"""
Unity Unused Package Finder - Standalone Script
Finds unused Package Manager packages AND Asset Store assets
Run this from your Unity project root directory
"""

import json
import os
import re
from pathlib import Path
from collections import defaultdict

def pascal_case(text):
    """Convert package name to PascalCase"""
    words = re.split(r'[-_]', text)
    return ''.join(word.capitalize() for word in words if word)

def generate_possible_namespaces(package_name):
    """Generate possible namespace variations from package name"""
    parts = package_name.split('.')
    namespaces = set()
    
    if len(parts) >= 3:
        # Standard: com.unity.textmeshpro -> Unity.TextMeshPro
        org = pascal_case(parts[1])
        pkg_name = pascal_case(parts[2])
        
        namespaces.add(f"{org}.{pkg_name}")
        namespaces.add(pkg_name)
        
        # Multi-part names: com.unity.render-pipelines.universal
        if len(parts) > 3:
            full_name = '.'.join(pascal_case(p) for p in parts[2:])
            namespaces.add(f"{org}.{full_name}")
            
            # Also try without dots
            combined = ''.join(pascal_case(p) for p in parts[2:])
            namespaces.add(f"{org}.{combined}")
    
    # Common abbreviations and special cases
    special_cases = {
        'com.unity.textmeshpro': ['TMPro', 'TextMeshPro'],
        'com.unity.inputsystem': ['UnityEngine.InputSystem', 'InputSystem'],
        'com.unity.addressables': ['UnityEngine.AddressableAssets', 'Addressables'],
        'com.unity.cinemachine': ['Cinemachine'],
        'com.unity.postprocessing': ['UnityEngine.Rendering.PostProcessing'],
    }
    
    if package_name in special_cases:
        namespaces.update(special_cases[package_name])
    
    return list(namespaces)

def read_manifest(project_path):
    """Read and parse the Packages/manifest.json file"""
    manifest_path = project_path / 'Packages' / 'manifest.json'
    
    if not manifest_path.exists():
        print(f"❌ Error: Could not find {manifest_path}")
        return None
    
    with open(manifest_path, 'r', encoding='utf-8') as f:
        manifest = json.load(f)
    
    dependencies = manifest.get('dependencies', {})
    
    # Filter out Unity core modules
    packages = {
        name: version 
        for name, version in dependencies.items() 
        if not name.startswith('com.unity.modules.')
    }
    
    return packages

def is_asset_package_folder(folder_path, assets_path):
    """Determine if a folder is likely an Asset Store package or third-party asset"""
    folder_name = folder_path.name.lower()
    
    # Skip common Unity/project folders
    skip_folders = {
        'editor', 'resources', 'plugins', 'streamingassets', 
        'scenes', 'scripts', 'prefabs', 'materials', 'textures',
        'animations', 'sounds', 'sprites', 'fonts', 'shaders'
    }
    
    if folder_name in skip_folders:
        return False
    
    # Don't consider root-level organizational folders
    if folder_path.parent == assets_path and folder_name in {'scripts', 'scenes', 'prefabs'}:
        return False
    
    # Look for indicators of third-party packages
    indicators = [
        (folder_path / 'README.md').exists(),
        (folder_path / 'README.txt').exists(),
        (folder_path / 'LICENSE').exists(),
        (folder_path / 'LICENSE.txt').exists(),
        (folder_path / 'CHANGELOG.md').exists(),
        (folder_path / 'package.json').exists(),
        # Has its own Editor and Runtime folders (package structure)
        (folder_path / 'Editor').exists() and (folder_path / 'Runtime').exists(),
        # Has multiple subfolders suggesting it's a complete package
        len(list(folder_path.iterdir())) > 3
    ]
    
    return any(indicators)

def find_asset_packages(project_path):
    """Find potential Asset Store packages in the Assets folder"""
    assets_path = project_path / 'Assets'
    asset_packages = []
    
    if not assets_path.exists():
        return asset_packages
    
    # Look through top-level and second-level folders
    for folder in assets_path.iterdir():
        if folder.is_dir() and not folder.name.startswith('.'):
            if is_asset_package_folder(folder, assets_path):
                asset_packages.append(folder)
            else:
                # Check one level deeper
                for subfolder in folder.iterdir():
                    if subfolder.is_dir() and not subfolder.name.startswith('.'):
                        if is_asset_package_folder(subfolder, assets_path):
                            asset_packages.append(subfolder)
    
    return asset_packages

def get_scripts_in_folder(folder_path):
    """Get all C# scripts in a folder and its subfolders"""
    return list(folder_path.rglob('*.cs'))

def get_class_names_from_script(script_path):
    """Extract class names from a C# script"""
    class_names = []
    try:
        with open(script_path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
            # Match class definitions (simplified)
            class_pattern = re.compile(r'\b(?:public|internal|private)?\s+(?:abstract|sealed|static)?\s*class\s+(\w+)')
            class_names = class_pattern.findall(content)
    except Exception:
        pass
    return class_names

def scan_for_references(project_path, class_names, exclude_folder):
    """Scan project for references to specific class names, excluding the source folder"""
    assets_path = project_path / 'Assets'
    reference_count = 0
    
    # Search in C# scripts
    for cs_file in assets_path.rglob('*.cs'):
        # Skip files in the excluded folder
        if exclude_folder in cs_file.parents:
            continue
        
        try:
            with open(cs_file, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                for class_name in class_names:
                    # Look for class usage (not perfect but good enough)
                    if re.search(r'\b' + re.escape(class_name) + r'\b', content):
                        reference_count += 1
                        break  # Count each file only once
        except Exception:
            pass
    
    # Search in Unity files (prefabs, scenes)
    for unity_file in list(assets_path.rglob('*.prefab')) + list(assets_path.rglob('*.unity')):
        try:
            with open(unity_file, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                for class_name in class_names:
                    if class_name in content:
                        reference_count += 1
                        break
        except Exception:
            pass
    
    return reference_count

def scan_scripts_for_namespaces(project_path):
    """Scan all C# scripts for using statements"""
    namespace_usage = defaultdict(int)
    assets_path = project_path / 'Assets'
    
    if not assets_path.exists():
        return namespace_usage
    
    cs_files = list(assets_path.rglob('*.cs'))
    using_pattern = re.compile(r'using\s+([a-zA-Z0-9_.]+)\s*;')
    
    for cs_file in cs_files:
        try:
            with open(cs_file, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                matches = using_pattern.findall(content)
                for namespace in matches:
                    namespace_usage[namespace] += 1
        except Exception:
            pass
    
    return namespace_usage

def analyze_asset_packages(project_path):
    """Analyze Asset Store packages for usage"""
    print("=" * 60)
    print("🛒 ANALYZING ASSET STORE / THIRD-PARTY PACKAGES")
    print("=" * 60)
    print()
    
    asset_packages = find_asset_packages(project_path)
    
    if not asset_packages:
        print("   No Asset Store packages detected")
        print()
        return
    
    print(f"📁 Found {len(asset_packages)} potential asset packages")
    print()
    
    unused_assets = []
    used_assets = []
    
    for package_folder in asset_packages:
        print(f"   Analyzing: {package_folder.relative_to(project_path / 'Assets')}...", end=' ')
        
        # Get all scripts in this package
        scripts = get_scripts_in_folder(package_folder)
        
        if not scripts:
            print("(no scripts)")
            continue
        
        # Extract class names
        all_classes = []
        for script in scripts:
            all_classes.extend(get_class_names_from_script(script))
        
        if not all_classes:
            print("(no classes found)")
            continue
        
        # Check for references outside this folder
        ref_count = scan_for_references(project_path, all_classes, package_folder)
        
        package_info = {
            'path': package_folder.relative_to(project_path / 'Assets'),
            'scripts': len(scripts),
            'classes': len(all_classes),
            'references': ref_count
        }
        
        if ref_count == 0:
            unused_assets.append(package_info)
            print(f"✗ ({len(scripts)} scripts, 0 external refs)")
        else:
            used_assets.append(package_info)
            print(f"✓ ({len(scripts)} scripts, {ref_count} external refs)")
    
    print()
    print("=" * 60)
    print(f"⚠️  POTENTIALLY UNUSED ASSETS ({len(unused_assets)})")
    print("=" * 60)
    print()
    
    if unused_assets:
        for asset in unused_assets:
            print(f"📦 {asset['path']}")
            print(f"   Scripts: {asset['scripts']}")
            print(f"   Classes: {asset['classes']}")
            print(f"   External References: {asset['references']}")
            print()
    else:
        print("✅ All detected asset packages appear to be in use!")
        print()
    
    print("=" * 60)
    print(f"✅ USED ASSETS ({len(used_assets)})")
    print("=" * 60)
    print()
    
    if used_assets:
        for asset in used_assets:
            print(f"📦 {asset['path']}")
            print(f"   Scripts: {asset['scripts']}")
            print(f"   External References: {asset['references']}")
            print()
    
    return unused_assets, used_assets

def analyze_package_manager_packages(project_path):
    """Analyze Package Manager packages"""
    print("=" * 60)
    print("📦 ANALYZING PACKAGE MANAGER PACKAGES")
    print("=" * 60)
    print()
    
    packages = read_manifest(project_path)
    
    if packages is None:
        return None, None
    
    print(f"   Found {len(packages)} packages (excluding core modules)")
    print()
    
    print("📁 Scanning C# files for namespace usage...")
    namespace_usage = scan_scripts_for_namespaces(project_path)
    print(f"   Found {len(namespace_usage)} unique namespaces")
    print()
    
    unused_packages = []
    used_packages = []
    
    for package_name, version in sorted(packages.items()):
        possible_namespaces = generate_possible_namespaces(package_name)
        
        ref_count = 0
        matched_namespaces = []
        
        for ns in possible_namespaces:
            if ns in namespace_usage:
                ref_count += namespace_usage[ns]
                matched_namespaces.append(ns)
        
        package_info = {
            'name': package_name,
            'version': version,
            'references': ref_count,
            'matched_namespaces': matched_namespaces
        }
        
        if ref_count == 0:
            unused_packages.append(package_info)
        else:
            used_packages.append(package_info)
    
    print("=" * 60)
    print(f"⚠️  POTENTIALLY UNUSED PACKAGES ({len(unused_packages)})")
    print("=" * 60)
    print()
    
    if unused_packages:
        for pkg in unused_packages:
            print(f"📦 {pkg['name']}")
            print(f"   Version: {pkg['version']}")
            print(f"   References: {pkg['references']}")
            print()
    else:
        print("✅ All Package Manager packages appear to be in use!")
        print()
    
    print("=" * 60)
    print(f"✅ USED PACKAGES ({len(used_packages)})")
    print("=" * 60)
    print()
    
    for pkg in used_packages:
        print(f"📦 {pkg['name']}")
        print(f"   Version: {pkg['version']}")
        print(f"   References: {pkg['references']}")
        if pkg['matched_namespaces']:
            print(f"   Namespaces: {', '.join(pkg['matched_namespaces'][:3])}")
        print()
    
    return unused_packages, used_packages

def main():
    import sys
    
    if len(sys.argv) > 1:
        project_path = Path(sys.argv[1])
    else:
        project_path = Path.cwd()
    
    if not project_path.exists():
        print(f"❌ Error: Project path does not exist: {project_path}")
        sys.exit(1)
    
    print()
    print("🎯 Unity Unused Package & Asset Finder")
    print(f"   Project: {project_path}")
    print()
    
    # Analyze Package Manager packages
    pm_unused, pm_used = analyze_package_manager_packages(project_path)
    
    print()
    
    # Analyze Asset Store packages
    asset_unused, asset_used = analyze_asset_packages(project_path)
    
    # Final summary
    print()
    print("=" * 60)
    print("📊 FINAL SUMMARY")
    print("=" * 60)
    
    if pm_unused is not None:
        print(f"Package Manager packages: {len(pm_used or [])} used, {len(pm_unused or [])} potentially unused")
    
    if asset_unused is not None:
        print(f"Asset Store packages: {len(asset_used or [])} used, {len(asset_unused or [])} potentially unused")
    
    print()
    print("⚠️  WARNING: Always test your project after removing packages!")
    print("   Some packages may be used indirectly or required as dependencies.")
    print()

if __name__ == '__main__':
    main()