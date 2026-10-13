AstraydeFrameWork Documentation
1. Overview

AstraydeFrameWork is a lightweight UI framework for Unity (URP). The current release focuses on two features:
- Window management (WindowManager + WindowBase)
- UI Blur for modal windows (URP Opaque Texture)

It depends on R3 (Reactive Extensions for Unity) and is designed to be extended by developers.

2. Requirements

Unity 2022.3 LTS or later (recommended)
URP only (Built-in Render Pipeline and HDRP are not supported)
R3 (not included): https://github.com/Cysharp/R3
UniTask (not included): https://github.com/Cysharp/UniTask

Quick setup: UPM and Unity NuGet

1. Open Project Settings.
2. Select Package Manager.
3. Add Scoped Registries:

OpenUPM
- Name: OpenUPM
- URL: https://package.openupm.com
- Scope(s): com.cysharp

Unity NuGet
- Name: Unity NuGet
- URL: https://unitynuget-registry.openupm.com
- Scope(s): org.nuget

3. Installation

Import the package into your Unity project and ensure it compiles.
Install R3 via Unity Package Manager or manual import.
Enable Opaque Texture in your URP Asset (required for UI Blur).

4. Quick Start
- Preparation
    1. AstraydeFramework assumes a single UICamera dedicated exclusively to UI rendering,
     Refer to the DevelopmentScene for an overview of the basic configuration.
- CreateWindowPrefab
    1. To create a empty window prefab, you can just Assets - Create - Astrayde - NewWindowPrefab,
     or, create a canvas touch the component below:
     UICanvasInitializer.cs
    2. Create your new WindowCompnent base on BaseWindow.cs
- OpenWindow - OpenWindowAsync
    There are two way to give assetPath of window to WindowManager.cs
    1. use OpenWindowAsync<T>() method (recommanded)
        - Add Class Attribute like this:
        [LocalAssetPath("Prefabs/Windows/YourWindowPrefab")]
        public class YourWindow : WindowBase
    2. use OpenWindowAsync(path) method
        - you can add set your own path of prefab and use it 

    Then, use your own customLoadMethod to load your resource instance, or we Load via Resource.Load()

5. Notes

UI Blur is URP-only and performance depends on resolution and platform.
The framework uses uGUI (UI Toolkit and DOTS are not supported).

6. Support

X: https://x.com/AstraydeGame

7. License

This asset is distributed under the Unity Asset Store standard license.
Third-party libraries such as R3 are licensed separately.

8. Release Notes

* 0.1.0
- 1st commit
