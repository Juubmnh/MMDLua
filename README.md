# MMDLua

Using this embedded application in conjunction with various Lua scripts for MikuMikuDance, users can generate keyframe data to quickly create animations that would otherwise be cumbersome to produce by manually adjusting parameters. Although the project is still in its early experimental stages, I believe it will promote a new approach to animation creation in MMD, allowing users to focus more on the creative aspects of animation rather than the technical details.

# Usage

1. To make MMDLua linked to MMD, you need to first install MMDPlugin, which enables third-party dynamic libraries to be loaded into MMD. After installing MMDPlugin, you can place the `MMDLua.dll` file into the `plugin` folder and place the `MMDLua` folder containing the executable program in the same directory as MMD. Note that the file path is hard-coded and cannot be changed.
2. When you launch MMD, you will see a new menu item called `MMDLua` in the menu bar. Click on it to open the MMDLua interface. The user interface is a pseudo-floating window; actually, it is a full-screen transparent window. Even if the application retrieves data by auto-save, this does not affect the original project file, which means that users still need to save it manually. For unknown reasons, the application may sometimes freeze.
3. Our reusable Lua scripts offer a high degree of flexibility. You can call contents from CLR packages using NLua syntax as well as utilize pre-registered features to perform programmable animation editing via MMD data APIs in Scallion.
4. Under normal circumstances, MMDLua should detect the MMD process within 10 seconds; otherwise, it will terminate itself. If you notice that MMDLua remains running in the background after MMD has finished executing, please submit a bug report.

# Special Thanks

* MikuMikuDance by 樋口優
* [MMDPlugin](https://github.com/oigami/MMDPluginInstallManager) by おいがみ
* [MMDUtility](https://github.com/oigami/MMDUtility) by おいがみ
* [Scallion](https://github.com/paralleltree/Scallion) by Ryo Namiki
* [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) by HexaEngine
* [NLua](https://github.com/NLua/NLua) by NLua
* [lua-matrix](https://github.com/davidm/lua-matrix) by Michael Lutz, David Manura
* [LDoc](https://github.com/lunarmodules/ldoc) by Steve Donovan
* [NativeFileDialogSharp](https://github.com/milleniumbug/NativeFileDialogSharp) by milleniumbug
