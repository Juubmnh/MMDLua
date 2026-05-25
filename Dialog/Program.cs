using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.D3D9;
using Hexa.NET.ImGui.Backends.Win32;
using HexaGen.Runtime;
using MMDLuaDialog;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D9;
using Silk.NET.Maths;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;
using IDirect3DDevice9 = Silk.NET.Direct3D9.IDirect3DDevice9;

var hInstance = PInvoke.GetModuleHandle((string?)null);

Bridge mmdBridge = new();

var stopwatch = new Stopwatch();
stopwatch.Start();
Func<bool> shouldTerminate = () =>
{
    if (stopwatch.Elapsed.TotalSeconds < 10)
    {
        return !mmdBridge.TargetHWnd.IsNull && !PInvoke.IsWindow(mmdBridge.TargetHWnd);
    }
    else
    {
        stopwatch.Stop();
        shouldTerminate = () =>
        {
            return mmdBridge.TargetHWnd.IsNull || !PInvoke.IsWindow(mmdBridge.TargetHWnd);
        };
        return shouldTerminate();
    }
};

HWND hWnd;
unsafe
{
    var pClassName = Utils.StringToUTF16Ptr("MMDLua_WNDCLASS");

    var wndClass = new WNDCLASSEXW()
    {
        cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
        style = WNDCLASS_STYLES.CS_CLASSDC,
        lpfnWndProc = WindowProc,
        cbClsExtra = 0,
        cbWndExtra = 0,
        hInstance = new(hInstance.DangerousGetHandle()),
        hIcon = HICON.Null,
        hCursor = HCURSOR.Null,
        hbrBackground = HBRUSH.Null,
        lpszMenuName = null,
        lpszClassName = pClassName,
        hIconSm = HICON.Null
    };

    ExHelper.ThrowIfFails((BOOL)PInvoke.RegisterClassEx(wndClass));

    hWnd = PInvoke.CreateWindowEx(
       WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW,
       new(pClassName),
       "MMDLua",
       WINDOW_STYLE.WS_POPUP,
       0, 0,
       PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN), PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN),
       HWND.Null,
       null,
       hInstance,
       null);

    Utils.Free(pClassName);

    ExHelper.ThrowIfFails(!hWnd.IsNull);
}

ExHelper.ThrowIfFails(PInvoke.SetLayeredWindowAttributes(hWnd, new(0), 255, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA));

var margins = new MARGINS()
{
    cxLeftWidth = -1,
    cxRightWidth = -1,
    cyTopHeight = -1,
    cyBottomHeight = -1
};
ExHelper.ThrowIfFails(PInvoke.DwmExtendFrameIntoClientArea(hWnd, margins));

PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOW);
ExHelper.ThrowIfFails(PInvoke.UpdateWindow(hWnd));

var d3d9 = D3D9.GetApi();
var imguiContext = ImGui.CreateContext();
var imguiIO = ImGui.GetIO();

unsafe
{
    ImGuiImplWin32.SetCurrentContext(imguiContext);
    ImGuiImplWin32.Init(hWnd.Value);
}

PresentParameters d3dpp = default;
ComPtr<IDirect3D9> pD3D = null;
ComPtr<IDirect3DDevice9> pD3DDevice = null;
unsafe
{
    pD3D = d3d9.Direct3DCreate9(D3D9.SdkVersion);

    d3dpp.Windowed = true;                       // Run in windowed mode
    d3dpp.SwapEffect = Swapeffect.Discard;       // Discard old frames
    d3dpp.BackBufferFormat = Format.A8R8G8B8;    // Use current display format
    d3dpp.EnableAutoDepthStencil = true;
    d3dpp.AutoDepthStencilFormat = Format.D16;
    d3dpp.PresentationInterval = D3D9.PresentIntervalOne;

    // Create the Direct3D device
    HResult hr = pD3D.CreateDevice(
        D3D9.AdapterDefault,                    // Use the primary display adapter
        Devtype.Hal,                            // Use hardware rasterization
        hWnd,                                   // Handle to the window
        D3D9.CreateHardwareVertexprocessing,    // Use software vertex processing
        ref d3dpp,                              // Presentation parameters
        ref pD3DDevice                          // Pointer to the created device
    );

    ImGuiImplD3D9.SetCurrentContext(imguiContext);
    ImGuiImplD3D9.Init(new((Hexa.NET.ImGui.Backends.D3D9.IDirect3DDevice9*)pD3DDevice.Handle));
}

bool running = true;
bool deviceLost = false;
uint resizeWidth = 0, resizeHeight = 0;
MSG msg = default;
Vector4 clearColor = new(0, 0, 0, 0);
bool showMainData = false;

string filterRegex = ".*";
bool isValidFilterRegex = true;

string directoryToAdd = string.Empty;
string fileToDrop = string.Empty;
unsafe
{
    imguiIO.Fonts.AddFontFromFileTTF("C:\\Windows\\Fonts\\msyh.ttc", 17);

    while (running)
    {
        while (PInvoke.PeekMessage(out msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
        {
            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
            if (msg.message == PInvoke.WM_QUIT)
                running = false;
        }

        if (shouldTerminate())
        {
            running = false;
        }

        var exStyle = (uint)PInvoke.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        if (imguiIO.WantCaptureMouse)
        {
            exStyle &= ~(uint)WINDOW_EX_STYLE.WS_EX_TRANSPARENT;
        }
        else
        {
            exStyle |= (uint)WINDOW_EX_STYLE.WS_EX_TRANSPARENT;
        }
        PInvoke.SetLastError(WIN32_ERROR.NO_ERROR);
        ExHelper.ThrowIfFails((BOOL)PInvoke.SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)exStyle));

        if (deviceLost)
        {
            ResultCode hr = (ResultCode)pD3DDevice.TestCooperativeLevel();
            if (hr == ResultCode.D3DERR_DEVICELOST)
            {
                Thread.Sleep(10);
                continue;
            }
            if (hr == ResultCode.D3DERR_DEVICENOTRESET)
                ResetDevice();
            deviceLost = false;
        }

        if (resizeWidth != 0 && resizeHeight != 0)
        {
            d3dpp.BackBufferWidth = resizeWidth;
            d3dpp.BackBufferHeight = resizeHeight;
            resizeWidth = resizeHeight = 0;
            ResetDevice();
        }

        ImGuiImplD3D9.NewFrame();
        ImGuiImplWin32.NewFrame();
        ImGui.NewFrame();

        //ImGui.ShowDemoWindow();

        ImGui.SetNextWindowSize(new(360, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("MMD Lua");
        ImGui.Checkbox("Show MMD Main Data", ref showMainData);
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SeparatorText("Scripts");
        if (ImGui.Button("Run"))
        {
            Task.Run(mmdBridge.RunScripts);
        }
        ImGui.SameLine();
        if (ImGui.Button("Save as Lua"))
        {
            mmdBridge.SaveAsLua();
        }
        if (ImGui.InputText("Filter Regex", ref filterRegex, PInvoke.MAX_PATH))
        {
            try
            {
                _ = new Regex(filterRegex);
                isValidFilterRegex = true;
            }
            catch (Exception ex)
            {
                isValidFilterRegex = false;
                mmdBridge.ExceptionHandler(ex);
            }
        }
        if (ImGui.BeginTable("Scripts", 2, ImGuiTableFlags.Borders))
        {
            mmdBridge.Saveable.LuaScripts.ForEach(scr =>
            {
                if (isValidFilterRegex && !Regex.IsMatch(scr.File.FullName, filterRegex))
                {
                    return;
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if (ImGui.Selectable(mmdBridge.Saveable.GetScriptDisplayName(scr), ref scr.selected))
                {
                    if (scr.selected)
                    {
                        mmdBridge.Saveable.SelectedScripts.Add(scr);
                    }
                    else
                    {
                        mmdBridge.Saveable.SelectedScripts.Remove(scr);
                    }
                }
                ImGui.TableNextColumn();
                ImGui.Text(scr.File.DirectoryName);
            });
            ImGui.EndTable();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SeparatorText("Realtime Parameters");
        if (mmdBridge.RequiringParameters is not null && !mmdBridge.RequiringParameters.Task.IsCompleted)
        {
            mmdBridge.Parameters.ForEach(param =>
            {
                if (param is Parameter<int> intParam)
                {
                    ImGui.InputInt(intParam.name, ref intParam.value);
                }
                else if (param is Parameter<float> floatParam)
                {
                    ImGui.InputFloat(floatParam.name, ref floatParam.value);
                }
                else if (param is Parameter<Vector2> vec2Param)
                {
                    ImGui.InputFloat2(vec2Param.name, ref vec2Param.value);
                }
                else if (param is Parameter<Vector3> vec3Param)
                {
                    ImGui.InputFloat3(vec3Param.name, ref vec3Param.value);
                }
                else if (param is Parameter<double> doubleParam)
                {
                    ImGui.InputDouble(doubleParam.name, ref doubleParam.value);
                }
                else if (param is Parameter<string> stringParam)
                {
                    ImGui.InputText(stringParam.name, ref stringParam.value, PInvoke.MAX_PATH);
                }
            });
            if (mmdBridge.Parameters.Count == 0 || ImGui.Button("Submit"))
            {
                mmdBridge.RequiringParameters.TrySetResult();
            }
            if (mmdBridge.Parameters.Count != 0)
            {
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    mmdBridge.RequiringParameters.TrySetCanceled();
                }
            }
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SeparatorText("Source Directories");
        if (ImGui.BeginTable("Sources", 1, ImGuiTableFlags.Borders))
        {
            mmdBridge.Saveable.LuaSources.ForEach(src =>
            {
                ImGui.TableNextColumn();
                ImGui.Selectable(src.Directory.FullName, ref src.selected);
            });
            ImGui.EndTable();
        }
        ImGui.InputText("Path", ref directoryToAdd, PInvoke.MAX_PATH);
        if (ImGui.Button("Add"))
        {
            mmdBridge.TryAddLuaSource(directoryToAdd);
        }
        ImGui.SameLine();
        if (ImGui.Button("Refresh"))
        {
            mmdBridge.RefreshLuaScripts();
        }
        ImGui.SameLine();
        if (ImGui.Button("Delete"))
        {
            mmdBridge.DeleteSelectedLuaSources();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SeparatorText("Log");
        if (ImGui.BeginChild("Log", ImGuiChildFlags.Borders))
        {
            if (ImGui.Button("Clear"))
            {
                mmdBridge.Log.Clear();
            }
            ImGui.SameLine();
            if (ImGui.Button("Export"))
            {
                File.WriteAllText(mmdBridge.LogFilePath, mmdBridge.Log.ToString());
            }
            ImGui.TextWrapped(mmdBridge.Log.ToString());
            ImGui.EndChild();
        }
        ImGui.End();

        if (showMainData)
        {
            ImGui.SetNextWindowSize(new(360, 400), ImGuiCond.FirstUseEver);
            ImGui.Begin("MMD Main Data", ref showMainData);
            ImGui.Text($"Application average {1000 / imguiIO.Framerate:F3} ms/frame ({imguiIO.Framerate:F1} FPS)");
            ImGui.Text($"MMD HWnd: {mmdBridge.TargetHWnd}");
            if (ImGui.Button("Request Data"))
                mmdBridge.RequestData();
            if (ImGui.Button("Save Project"))
                mmdBridge.RequestSave();
            ImGui.InputText("VMD File", ref fileToDrop, PInvoke.MAX_PATH);
            if (ImGui.Button("Stimulate File Dropping"))
                mmdBridge.RequestDropFile(fileToDrop);
            ImGui.Separator();
            mmdBridge.ShowMainData();
            ImGui.Separator();
            if (ImGui.Button("Terminate Process"))
                running = false;
            ImGui.End();
        }

        ImGui.EndFrame();

        pD3DDevice.SetRenderState(Renderstatetype.Zenable, 0);
        pD3DDevice.SetRenderState(Renderstatetype.Alphablendenable, 0);
        pD3DDevice.SetRenderState(Renderstatetype.Scissortestenable, 0);
        uint clearColorDx = (uint)(
            ((int)(clearColor.W * 255.0f) << 24) |                 // Alpha
            ((int)(clearColor.X * clearColor.W * 255.0f) << 16) |  // Red
            ((int)(clearColor.Y * clearColor.W * 255.0f) << 8) |   // Green
            ((int)(clearColor.Z * clearColor.W * 255.0f))          // Blue
        );
        pD3DDevice.Clear(0, (Rect*)null, D3D9.ClearTarget | D3D9.ClearZbuffer, clearColorDx, 1.0f, 0);
        if (pD3DDevice.BeginScene() >= 0)
        {
            ImGui.Render();
            ImGuiImplD3D9.RenderDrawData(ImGui.GetDrawData());
            pD3DDevice.EndScene();
        }

        ResultCode result = (ResultCode)pD3DDevice.Present((Box2D<int>*)null, (Box2D<int>*)null, 0, (RGNData*)null);
        if (result == ResultCode.D3DERR_DEVICELOST)
            deviceLost = true;
    }
}

ImGuiImplD3D9.Shutdown();
ImGuiImplWin32.Shutdown();
ImGui.DestroyContext();

pD3DDevice.Release();
pD3D.Release();

mmdBridge.SaveState();
ExHelper.ThrowIfFails(PInvoke.DestroyWindow(hWnd));

LRESULT WindowProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
{
    if (ImGuiImplWin32.WndProcHandler(hWnd, msg, wParam, lParam) != 0)
    {
        return (LRESULT)1;
    }

    switch (msg)
    {
        case PInvoke.WM_SIZE:
            if (wParam == PInvoke.SIZE_MINIMIZED)
                return (LRESULT)0;
            resizeWidth = (uint)(lParam & 0xFFFF); // Queue resize
            resizeHeight = (uint)((lParam >> 16) & 0xFFFF);
            return (LRESULT)0;

        case PInvoke.WM_SYSCOMMAND:
            if ((wParam & 0xfff0) == PInvoke.SC_KEYMENU) // Disable ALT application menu
                return (LRESULT)0;
            break;

        case PInvoke.WM_DESTROY:
            Environment.Exit(0);
            return (LRESULT)0;
    }

    return PInvoke.DefWindowProc(hWnd, msg, wParam, lParam);
}

void ResetDevice()
{
    ImGuiImplD3D9.InvalidateDeviceObjects();
    ResultCode hr = (ResultCode)pD3DDevice.Reset(ref d3dpp);
    if (hr == ResultCode.D3DERR_INVALIDCALL)
        Trace.Assert(false);
    ImGuiImplD3D9.CreateDeviceObjects();
}
