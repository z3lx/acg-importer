![License](https://img.shields.io/badge/license-MIT-blue)
![Unity Version](https://img.shields.io/badge/unity-2019.3%2B-lightgrey)

# Unity AmbientCG Importer
acg-importer is a Unity package that simplifies the process of importing textures from AmbientCG into Unity.

https://github.com/z3lx/acg-importer/assets/57017122/9c112458-8436-445c-9ee2-280095b93327

## Features
- **Unity Custom Editor**: Utilize a custom editor within Unity for a user-friendly experience, complete with tooltips for more information.
- **Single and Bulk Import Options**: Import materials and textures individually or in bulk through both single and batch ZIP imports.
- **AmbientCG NextCloud Support**: Bulk import files downloaded via [Nextcloud](https://docs.ambientcg.com/patreon-rewards/nextcloud/downloading), an AmbientCG Patreon-only feature that allows for easier batch downloading.
- **Shader Flexibility**: Compatible out of the box with HDRP, URP, and BiRP lit shaders, as well as any custom shaders by adjusting shader properties during the import process.
- **Optimized Texture Handling**: Save only required textures and generate new swizzled ones when necessary depending on the shader properties.

## Getting started
### Using Git
Before proceeding with this method, ensure you have Git installed on your system.
1. Open Unity and navigate to **Window → Package Manager**.
2. Click on the top left button with a "**+**" on it to add a new package. In the dropdown, select "**Add package from git URL**".
3. Enter the following git URL: `https://github.com/z3lx/acg-importer.git`

### Manual Installation
1. Go to the [Releases](https://github.com/z3lx/acg-importer/releases) section of this repository.
2. Download the latest available Unity package.
3. Open your Unity project and navigate to the "**Assets**" tab at the top.
4. Choose "**Import Package**," then select "**Custom Package...**".
5. Locate and select the downloaded Unity package.
6. Follow the prompts to complete the import process.

## Usage
1. Open the import window located in **Tools → ACG Importer**.
2. Set your import preferences:
   | Parameter | Type | Description |
   |-|-|-|
   | **Input Path** | `string` | The directory or zip file to import. This can be a single zip file, a single folder, a parent directory containing multiple zip files, or a parent directory containing multiple subfolders with associated textures to import. |
   | **Output Path** | `string` | The relative path within the Unity project where the imported textures and materials will be saved. |
   | **Create Category Directory** | `bool` | If enabled, a separate directory named after the material category (e.g., 'Bricks' for 'Bricks090' material) will be created within the OutputPath. The imported textures and materials of that category will be saved in this directory. |
   | **Create Material Directory** | `bool` | If enabled, a separate directory named after the material will be created within the OutputPath. The imported textures and materials of that material will be saved in this directory. |
   | **Shader** | `Shader` | The shader to be used for the imported materials. |
   | **Shader Properties** | `List<ShaderProperty>` | The shader properties to be set on the material. |
   | **Type** | `Type` | The type of the shader property. |
   | **Name** | `string` | The name of the shader property. |
   | **Value** | `object` | The value of the shader property. |
3. Import textures and materials.

## Issues and Contributions
Feel free to report issues or contribute to the development of this project!

## License
This project is licensed under the [MIT License](https://github.com/z3lx/acg-importer/blob/main/LICENSE). Textures imported from ambientCG are licensed under [CC0](https://docs.ambientcg.com/license).
