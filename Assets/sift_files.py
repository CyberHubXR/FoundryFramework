import os
import shutil

def copy_files_with_extension(source_dir, dest_dir, extensions):
    """
    Recursively copy files with specified extensions from source directory to destination directory.
    
    Args:
        source_dir (str): Path to the source directory.
        dest_dir (str): Path to the destination directory.
        extensions (list): List of file extensions to be copied.
    """
    for root, _, files in os.walk(source_dir):
        for file in files:
            if '~' in file:
                continue
            if 'editor' in file.lower():
                continue
            if any(ext in file for ext in extensions):
                source_file = os.path.join(root, file)
                relative_path = os.path.relpath(source_file, source_dir)
                destination_file = os.path.join(dest_dir, relative_path)
                
                # Create destination directory if it doesn't exist
                os.makedirs(os.path.dirname(destination_file), exist_ok=True)
                
                # Copy the file
                shutil.copyfile(source_file, destination_file)
                print(f"Moving {source_file} to {destination_file}")

                # Delete the source file
                os.remove(source_file)

# Example usage:
source_directory = input("Enter the source directory: ")
destination_directory = input("Enter the destination directory: ")
extensions_to_copy = ['.mat', '.prefab', '.png', '.jpg', '.jpeg', '.fbx', '.shader', ]  # Specify the extensions you want to copy

copy_files_with_extension(source_directory, destination_directory, extensions_to_copy)
