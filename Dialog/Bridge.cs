using Hexa.NET.ImGui;
using NativeFileDialogSharp;
using NLua;
using Scallion.DomainModels;
using Scallion.DomainModels.Components;
using Silk.NET.Maths;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Win32.Foundation;

namespace MMDLuaDialog;

internal class CameraKeyFrameData
{
    public int frame_no;
    public int pre_index;
    public int next_index; // 次のキーフレームがあるときに0以外になる
    public float length;
    public Vector3D<float> xyz;
    public Vector3D<float> rxyz;
    public byte[] hokan1_x = new byte[6]; // x, y, z, 回転, 距離, 視野角
    public byte[] hokan1_y = new byte[6];
    public byte[] hokan2_x = new byte[6];
    public byte[] hokan2_y = new byte[6];
    public int is_perspective;
    public int view_angle;
    public int is_selected; // 1で選択している。0で選択していない
    public int looking_model_index;
    public int looking_bone_index;
}

internal class BoneCurrentData
{
    public string name_jp = string.Empty;
    public string name_en = string.Empty;
    public Vector3D<float> init_xyz;
    public Vector3D<float> xyz;
    public Vector3D<float> xyz2;
}

internal class BoneKeyFrame
{
    public int frame_number;
    public int pre_index;
    public int next_index;
    public byte[] interpolation_curve_x1 = new byte[4];
    public byte[] interpolation_curve_y1 = new byte[4];
    public byte[] interpolation_curve_x2 = new byte[4];
    public byte[] interpolation_curve_y2 = new byte[4];
    public Vector3D<float> xyz;
    public Vector4D<float> rotation_q;
}

internal class MorphKeyFrame
{
    public int frame_number;
    public int pre_index;
    public int next_index;
    public float value;
    public byte is_selected;
}

internal class RelationSetting
{
    public int parent_model_index;
    public int parent_bone_index;
}

internal class ConfigurationKeyFrame
{
    public int frame_number;
    public int pre_index;
    public int next_index;
    public byte is_visible;
    public byte[] is_ik_enabled = []; // [ik_count]
    public List<RelationSetting> relation_setting = []; // [ik_count]
}

internal class MMDModelData
{
    public string name_jp = string.Empty;
    public string name_en = string.Empty; // もしかしたら別の領域に分かれてるかも
    public string comment_jp = string.Empty;
    public string comment_en = string.Empty;
    public string file_path = string.Empty;

    public List<BoneCurrentData> bone_current_data = []; // [bone_count]

    public byte keyframe_editor_toplevel_rows;

    public List<BoneKeyFrame> bone_keyframe = [];
    public List<MorphKeyFrame> morph_keyframe = [];
    public List<ConfigurationKeyFrame> configuration_keyframe = [];

    public byte render_order;
    public int bone_count;
    public int morph_count;
    public int ik_count;
    public byte is_visible;
    public int selected_bone;
    public int[] selected_morph_indices = new int[4];
    public int vscroll;
    public int last_frame_number;
    public int parentable_bone_count;
}

internal class MMDMainData
{
    public Vector2D<int> mouse_xy;
    public Vector2D<int> pre_mouse_xy;

    public int key_up;
    public int key_down;
    public int key_left;
    public int key_right;
    public int key_shift; // keyは0から3までの数値を取る。押している間は3になる。
    public int key_space;
    public int key_f9;
    public int key_x_or_f11; // f11の場合は2になる
    public int key_z;
    public int key_c;
    public int key_v;
    public int key_d;
    public int key_a;
    public int key_b;
    public int key_g;
    public int key_s;
    public int key_i;
    public int key_h;
    public int key_k;
    public int key_p;
    public int key_u;
    public int key_j;
    public int key_f;
    public int key_r;
    public int key_l;
    public int key_close_bracket;
    public int key_backslash;
    public int key_tab;
    public int key_enter;
    public int key_ctrl;
    public int key_alt;

    public Vector3D<float> xyz;
    public Vector3D<float> rxyz;

    public int counter;
    public float counter_f;

    public List<CameraKeyFrameData> camera_keyframe = []; // [10000]

    public List<MMDModelData> model_data = []; // [255] 起動時nullモデルを読み込むと順番にポインタが入る

    public int select_model;
    public int select_bone_type; // 0:選択、1:BOX選択、2:カメラモード、3:回転、4:移動

    public int mouse_over_move; // xyz回転(9,10,11)、xyz移動(12,13,14)

    public int left_frame;
    public int pre_left_frame;
    public int now_frame;

    public byte[] edit_interpolation_curve = new byte[4]; // x1 y1 x2 y2

    public byte is_camera_select;
    public byte[] is_model_bone_select = new byte[127];

    public Vector2D<int> output_size_xy;
    public float length;

    public string pmm_path = string.Empty;
}

internal class LuaSource(DirectoryInfo directory)
{
    [JsonIgnore] public DirectoryInfo Directory { get; } = directory;
    [JsonInclude] private readonly string directoryName = directory.FullName;
    public bool selected;

    [JsonConstructor]
    public LuaSource(string directoryName, bool selected) : this(new DirectoryInfo(directoryName))
    {
        this.selected = selected;
    }
}

internal class LuaScript(FileInfo file)
{
    [JsonIgnore] public FileInfo File { get; } = file;
    [JsonInclude] private readonly string fileName = file.FullName;
    public bool selected;

    [JsonConstructor]
    public LuaScript(string fileName, bool selected) : this(new FileInfo(fileName))
    {
        this.selected = selected;
    }
}

[method: JsonConstructor]
internal class Saveable(List<LuaSource> luaSources, List<LuaScript> luaScripts, IEnumerable<int> selectedScriptIndices)
{
    public List<LuaSource> LuaSources { get; } = luaSources;
    public List<LuaScript> LuaScripts { get; } = luaScripts;
    [JsonIgnore] public List<LuaScript> SelectedScripts { get; } = [.. selectedScriptIndices.Select(i => luaScripts[i])];
    public IEnumerable<int> SelectedScriptIndices => SelectedScripts.Select(scr => LuaScripts.IndexOf(scr));

    public Saveable() : this([], [], []) { }
    public string GetScriptDisplayName(int index)
    {
        var script = LuaScripts[index];
        var order = SelectedScripts.IndexOf(script) + 1;
        return (order == 0 ? script.File.Name : $"{script.File.Name} ({order})") + $"###lua_script_{index}";
    }
}

internal interface IParameter
{
    public string Name { get; }
    public object? Value { get; }
}

internal class Parameter<T>(string name, T value) : IParameter
{
    public string name = name;
    public T value = value;

    string IParameter.Name => name;
    object? IParameter.Value => value;
}

internal class DefaultParameter { }

internal class Bridge
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { IncludeFields = true, PropertyNameCaseInsensitive = true, WriteIndented = true };
    private readonly EventWaitHandle _pipeListenerEvent, _saveProjectEvent, _saveProjectFinishedEvent, _dropFileEvent;
    private Lua _lua;
    private readonly Queue<object?> _parameterQueue = [];

    public string ConfigFilePath { get; } = Path.Combine(AppContext.BaseDirectory, "config.json");
    public string VmdFilePath { get; } = Path.Combine(AppContext.BaseDirectory, "motion.vmd");
    public string PmmFilePath { get; } = Path.Combine(AppContext.BaseDirectory, "project.pmm");
    public string LogFilePath { get; } = Path.Combine(AppContext.BaseDirectory, "log.txt");

    public Encoding ShiftJIS { get; }
    public HWND TargetHWnd { get; private set; }
    public MMDMainData MainData { get; } = new();
    public Saveable Saveable { get; } = new();
    public List<IParameter> Parameters { get; } = [];
    public int CurrentScriptIndex { get; private set; } = -1;
    public List<bool> OutputEnabled { get; private set; } = [];
    public TaskCompletionSource? RequiringParameters { get; set; }
    public StringBuilder Log { get; private set; } = new();

    public Bridge()
    {
        if (File.Exists(ConfigFilePath))
        {
            var serialized = File.ReadAllText(ConfigFilePath);
            var saveable = JsonSerializer.Deserialize<Saveable>(serialized, _jsonSerializerOptions);
            if (saveable is not null)
            {
                Saveable = saveable;
            }
        }

        _pipeListenerEvent = new(false, EventResetMode.AutoReset, @"Global\MMDLuaPipeListenerEvent");
        _saveProjectEvent = new(false, EventResetMode.AutoReset, @"Global\MMDLuaSaveProjectEvent");
        _saveProjectFinishedEvent = new(false, EventResetMode.AutoReset, @"Global\MMDLuaSaveProjectFinishedEvent");
        _dropFileEvent = new(false, EventResetMode.AutoReset, @"Global\MMDLuaDropFileEvent");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ShiftJIS = Encoding.GetEncoding("shift_jis");

        ResetLuaState();

        Task.Run(PipeProc);
    }

    public void SaveState()
    {
        var serialized = JsonSerializer.Serialize(Saveable, _jsonSerializerOptions);
        File.WriteAllText(ConfigFilePath, serialized);
    }

    public void ExceptionHandler(Exception ex)
    {
        var currentEx = ex;
        do
        {
            Log.AppendLine(currentEx.Message);
            currentEx = currentEx.InnerException;
        } while (currentEx is not null);
    }

    public void ExceptionHandler(string message)
    {
        Log.AppendLine(message);
    }

    public void RequestData()
    {
        _pipeListenerEvent.Set();
    }

    public void RequestSave()
    {
        _saveProjectEvent.Set();
    }

    public void RequestDropFile(string fileToDrop)
    {
        var fileInfo = new FileInfo(fileToDrop);
        if (fileInfo.Exists)
        {
            var destFile = VmdFilePath;
            if (File.Exists(destFile))
            {
                File.Delete(destFile);
            }
            File.Copy(fileInfo.FullName, destFile);
            _dropFileEvent.Set();
        }
    }

    public void RequestDropFile()
    {
        _dropFileEvent.Set();
    }

    private static int _bytesRead;

    private static T PipeRead<T>(NamedPipeServerStream pipeServer) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        _bytesRead += size;
        var buffer = new byte[size];
        pipeServer.ReadExactly(buffer, 0, size);
        return MemoryMarshal.Read<T>(buffer);
    }

    private static T[] PipeReadArray<T>(NamedPipeServerStream pipeServer, int count) where T : unmanaged
    {
        var result = new T[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = PipeRead<T>(pipeServer);
        }
        return result;
    }

    private static List<T> PipeReadArray<T, C>(NamedPipeServerStream pipeServer) where T : unmanaged where C : unmanaged, INumber<C>
    {
        var count = int.CreateChecked(PipeRead<C>(pipeServer));
        List<T> result = [];
        for (int i = 0; i < count; i++)
        {
            result.Add(PipeRead<T>(pipeServer));
        }
        return result;
    }

    private static string GetString(Encoding encoding, byte[] bytes)
    {
        var str = encoding.GetString(bytes);
        str = str.TrimStart((char)0);
        var idx = str.IndexOf((char)0);
        if (idx != -1) str = str[0..idx];
        return str;
    }

    private void PipeProc()
    {
        while (true)
        {
            try
            {
                using var pipeServer = new NamedPipeServerStream("MMDLuaPipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None);
                pipeServer.WaitForConnection();

                _bytesRead = 0;
                var pipeDataType = PipeRead<int>(pipeServer);
                if (pipeDataType == 0)
                {
                    ExceptionHandler($"Reading MMD hWnd ({_bytesRead})");
                    TargetHWnd = PipeRead<HWND>(pipeServer);
                    ExceptionHandler($"Reading finished ({_bytesRead})");
                }
                else if (pipeDataType == 1)
                {
                    ExceptionHandler($"Reading mouse positions ({_bytesRead})");
                    MainData.mouse_xy = PipeRead<Vector2D<int>>(pipeServer);
                    MainData.pre_mouse_xy = PipeRead<Vector2D<int>>(pipeServer);

                    ExceptionHandler($"Reading key states ({_bytesRead})");
                    MainData.key_up = PipeRead<int>(pipeServer);
                    MainData.key_down = PipeRead<int>(pipeServer);
                    MainData.key_left = PipeRead<int>(pipeServer);
                    MainData.key_right = PipeRead<int>(pipeServer);
                    MainData.key_shift = PipeRead<int>(pipeServer);
                    MainData.key_space = PipeRead<int>(pipeServer);
                    MainData.key_f9 = PipeRead<int>(pipeServer);
                    MainData.key_x_or_f11 = PipeRead<int>(pipeServer);
                    MainData.key_z = PipeRead<int>(pipeServer);
                    MainData.key_c = PipeRead<int>(pipeServer);
                    MainData.key_v = PipeRead<int>(pipeServer);
                    MainData.key_d = PipeRead<int>(pipeServer);
                    MainData.key_a = PipeRead<int>(pipeServer);
                    MainData.key_b = PipeRead<int>(pipeServer);
                    MainData.key_g = PipeRead<int>(pipeServer);
                    MainData.key_s = PipeRead<int>(pipeServer);
                    MainData.key_i = PipeRead<int>(pipeServer);
                    MainData.key_h = PipeRead<int>(pipeServer);
                    MainData.key_k = PipeRead<int>(pipeServer);
                    MainData.key_p = PipeRead<int>(pipeServer);
                    MainData.key_u = PipeRead<int>(pipeServer);
                    MainData.key_j = PipeRead<int>(pipeServer);
                    MainData.key_f = PipeRead<int>(pipeServer);
                    MainData.key_r = PipeRead<int>(pipeServer);
                    MainData.key_l = PipeRead<int>(pipeServer);
                    MainData.key_close_bracket = PipeRead<int>(pipeServer);
                    MainData.key_backslash = PipeRead<int>(pipeServer);
                    MainData.key_tab = PipeRead<int>(pipeServer);
                    MainData.key_enter = PipeRead<int>(pipeServer);
                    MainData.key_ctrl = PipeRead<int>(pipeServer);
                    MainData.key_alt = PipeRead<int>(pipeServer);

                    ExceptionHandler($"Reading camera transformation ({_bytesRead})");
                    MainData.xyz = PipeRead<Vector3D<float>>(pipeServer);
                    MainData.rxyz = PipeRead<Vector3D<float>>(pipeServer);

                    ExceptionHandler($"Reading counters ({_bytesRead})");
                    MainData.counter = PipeRead<int>(pipeServer);
                    MainData.counter_f = PipeRead<float>(pipeServer);

                    ExceptionHandler($"Reading camera keyframes ({_bytesRead})");
                    MainData.camera_keyframe = new(10000);
                    for (int i = 0; i < 10000; i++)
                    {
                        CameraKeyFrameData cameraKeyFrame = new()
                        {
                            frame_no = PipeRead<int>(pipeServer),
                            pre_index = PipeRead<int>(pipeServer),
                            next_index = PipeRead<int>(pipeServer),
                            length = PipeRead<float>(pipeServer),
                            xyz = PipeRead<Vector3D<float>>(pipeServer),
                            rxyz = PipeRead<Vector3D<float>>(pipeServer),
                            hokan1_x = PipeReadArray<byte>(pipeServer, 6),
                            hokan1_y = PipeReadArray<byte>(pipeServer, 6),
                            hokan2_x = PipeReadArray<byte>(pipeServer, 6),
                            hokan2_y = PipeReadArray<byte>(pipeServer, 6),
                            is_perspective = PipeRead<int>(pipeServer),
                            view_angle = PipeRead<int>(pipeServer),
                            is_selected = PipeRead<int>(pipeServer),
                            looking_model_index = PipeRead<int>(pipeServer),
                            looking_bone_index = PipeRead<int>(pipeServer)
                        };
                        MainData.camera_keyframe.Add(cameraKeyFrame);
                    }

                    var modelCount = PipeRead<int>(pipeServer);
                    ExceptionHandler($"Model data count: {modelCount}");
                    MainData.model_data = new(modelCount);
                    for (int i = 0; i < modelCount; i++)
                    {
                        MMDModelData modelData = new()
                        {
                            name_jp = GetString(ShiftJIS, PipeReadArray<byte>(pipeServer, 50)),
                            name_en = GetString(Encoding.UTF8, PipeReadArray<byte>(pipeServer, 50)),
                            comment_jp = GetString(ShiftJIS, PipeReadArray<byte>(pipeServer, 256)),
                            comment_en = GetString(Encoding.UTF8, PipeReadArray<byte>(pipeServer, 292)),
                            file_path = GetString(Encoding.Unicode, PipeReadArray<byte>(pipeServer, 256 * 2)),

                            bone_count = PipeRead<int>(pipeServer),
                            morph_count = PipeRead<int>(pipeServer),
                            ik_count = PipeRead<int>(pipeServer)
                        };

                        modelData.bone_current_data = new(modelData.bone_count);
                        ExceptionHandler($"Model data [{i}] bone current keyframe count: {modelData.bone_count}");
                        for (int j = 0; j < modelData.bone_count; j++)
                        {
                            BoneCurrentData boneCurrentData = new()
                            {
                                name_jp = GetString(ShiftJIS, PipeReadArray<byte>(pipeServer, 20)),
                                name_en = GetString(Encoding.UTF8, PipeReadArray<byte>(pipeServer, 20)),
                                init_xyz = PipeRead<Vector3D<float>>(pipeServer),
                                xyz = PipeRead<Vector3D<float>>(pipeServer),
                                xyz2 = PipeRead<Vector3D<float>>(pipeServer)
                            };
                            modelData.bone_current_data.Add(boneCurrentData);
                        }

                        modelData.keyframe_editor_toplevel_rows = PipeRead<byte>(pipeServer);
                        ExceptionHandler($"Reading model keyframes ({_bytesRead})");

                        var count = PipeRead<int>(pipeServer);
                        ExceptionHandler($"Model data [{i}] bone keyframe count: {count}");
                        ExceptionHandler($"Reading ({_bytesRead})");
                        modelData.bone_keyframe = new(count);
                        for (int j = 0; j < count; j++)
                        {
                            BoneKeyFrame boneKeyFrame = new()
                            {
                                frame_number = PipeRead<int>(pipeServer),
                                pre_index = PipeRead<int>(pipeServer),
                                next_index = PipeRead<int>(pipeServer),

                                interpolation_curve_x1 = PipeReadArray<byte>(pipeServer, 4),
                                interpolation_curve_y1 = PipeReadArray<byte>(pipeServer, 4),
                                interpolation_curve_x2 = PipeReadArray<byte>(pipeServer, 4),
                                interpolation_curve_y2 = PipeReadArray<byte>(pipeServer, 4),

                                xyz = PipeRead<Vector3D<float>>(pipeServer),

                                rotation_q = PipeRead<Vector4D<float>>(pipeServer)
                            };
                            modelData.bone_keyframe.Add(boneKeyFrame);
                        }

                        count = PipeRead<int>(pipeServer);
                        ExceptionHandler($"Model data [{i}] morph keyframe count: {count}");
                        ExceptionHandler($"Reading ({_bytesRead})");
                        modelData.morph_keyframe = new(count);
                        for (int j = 0; j < count; j++)
                        {
                            MorphKeyFrame morphKeyFrame = new()
                            {
                                frame_number = PipeRead<int>(pipeServer),
                                pre_index = PipeRead<int>(pipeServer),
                                next_index = PipeRead<int>(pipeServer),
                                value = PipeRead<float>(pipeServer),
                                is_selected = PipeRead<byte>(pipeServer)
                            };
                            modelData.morph_keyframe.Add(morphKeyFrame);
                        }

                        count = PipeRead<int>(pipeServer);
                        ExceptionHandler($"Model data [{i}] configuration keyframe count: {count}");
                        ExceptionHandler($"Reading ({_bytesRead})");
                        modelData.configuration_keyframe = new(count);
                        for (int j = 0; j < count; j++)
                        {
                            ConfigurationKeyFrame configurationKeyFrame = new()
                            {
                                frame_number = PipeRead<int>(pipeServer),
                                pre_index = PipeRead<int>(pipeServer),
                                next_index = PipeRead<int>(pipeServer),
                                is_visible = PipeRead<byte>(pipeServer),
                                is_ik_enabled = PipeReadArray<byte>(pipeServer, modelData.ik_count),
                                relation_setting = new(modelData.ik_count)
                            };
                            for (int k = 0; k < modelData.ik_count; k++)
                            {
                                RelationSetting relationSetting = new()
                                {
                                    parent_model_index = PipeRead<int>(pipeServer),
                                    parent_bone_index = PipeRead<int>(pipeServer)
                                };
                                configurationKeyFrame.relation_setting.Add(relationSetting);
                            }
                            modelData.configuration_keyframe.Add(configurationKeyFrame);
                        }

                        modelData.render_order = PipeRead<byte>(pipeServer);
                        modelData.is_visible = PipeRead<byte>(pipeServer);
                        modelData.selected_bone = PipeRead<int>(pipeServer);
                        modelData.selected_morph_indices = PipeReadArray<int>(pipeServer, 4);
                        modelData.vscroll = PipeRead<int>(pipeServer);
                        modelData.last_frame_number = PipeRead<int>(pipeServer);
                        modelData.parentable_bone_count = PipeRead<int>(pipeServer);

                        MainData.model_data.Add(modelData);
                        ExceptionHandler($"Reading single model data finished ({_bytesRead})");
                    }

                    ExceptionHandler($"Reading others ({_bytesRead})");
                    MainData.select_model = PipeRead<int>(pipeServer);
                    MainData.select_bone_type = PipeRead<int>(pipeServer);

                    MainData.mouse_over_move = PipeRead<int>(pipeServer);

                    MainData.left_frame = PipeRead<int>(pipeServer);
                    MainData.pre_left_frame = PipeRead<int>(pipeServer);
                    MainData.now_frame = PipeRead<int>(pipeServer);

                    MainData.edit_interpolation_curve = PipeReadArray<byte>(pipeServer, 4);

                    MainData.is_camera_select = PipeRead<byte>(pipeServer);
                    MainData.is_model_bone_select = PipeReadArray<byte>(pipeServer, 127);

                    MainData.output_size_xy = PipeRead<Vector2D<int>>(pipeServer);

                    MainData.length = PipeRead<float>(pipeServer);

                    MainData.pmm_path = GetString(Encoding.Unicode, PipeReadArray<byte>(pipeServer, 256 * 2));

                    ExceptionHandler($"Reading finished ({_bytesRead})");
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler(ex);
            }
        }
    }

    private static void ShowList(string label, int count, Action<int> proc)
    {
        if (ImGui.TreeNode(label))
        {
            for (int i = 0; i < count; i++)
            {
                proc(i);
            }
            ImGui.TreePop();
        }
    }

    public unsafe void ShowMainData()
    {
        ImGui.InputInt2("mouse_xy", ref MainData.mouse_xy.X);
        ImGui.InputInt2("pre_mouse_xy", ref MainData.pre_mouse_xy.X);

        ImGui.InputInt("key_up", ref MainData.key_up);
        ImGui.InputInt("key_down", ref MainData.key_down);
        ImGui.InputInt("key_left", ref MainData.key_left);
        ImGui.InputInt("key_right", ref MainData.key_right);
        ImGui.InputInt("key_shift", ref MainData.key_shift);
        ImGui.InputInt("key_space", ref MainData.key_space);
        ImGui.InputInt("key_f9", ref MainData.key_f9);
        ImGui.InputInt("key_x_or_f11", ref MainData.key_x_or_f11);
        ImGui.InputInt("key_z", ref MainData.key_z);
        ImGui.InputInt("key_c", ref MainData.key_c);
        ImGui.InputInt("key_v", ref MainData.key_v);
        ImGui.InputInt("key_d", ref MainData.key_d);
        ImGui.InputInt("key_a", ref MainData.key_a);
        ImGui.InputInt("key_b", ref MainData.key_b);
        ImGui.InputInt("key_g", ref MainData.key_g);
        ImGui.InputInt("key_s", ref MainData.key_s);
        ImGui.InputInt("key_i", ref MainData.key_i);
        ImGui.InputInt("key_h", ref MainData.key_h);
        ImGui.InputInt("key_k", ref MainData.key_k);
        ImGui.InputInt("key_p", ref MainData.key_p);
        ImGui.InputInt("key_u", ref MainData.key_u);
        ImGui.InputInt("key_j", ref MainData.key_j);
        ImGui.InputInt("key_f", ref MainData.key_f);
        ImGui.InputInt("key_r", ref MainData.key_r);
        ImGui.InputInt("key_l", ref MainData.key_l);
        ImGui.InputInt("key_close_bracket", ref MainData.key_close_bracket);
        ImGui.InputInt("key_backslash", ref MainData.key_backslash);
        ImGui.InputInt("key_tab", ref MainData.key_tab);
        ImGui.InputInt("key_enter", ref MainData.key_enter);
        ImGui.InputInt("key_ctrl", ref MainData.key_ctrl);
        ImGui.InputInt("key_alt", ref MainData.key_alt);

        ImGui.InputFloat3("xyz", ref MainData.xyz.X);
        ImGui.InputFloat3("rxyz", ref MainData.rxyz.X);

        ImGui.InputInt("counter", ref MainData.counter);
        ImGui.InputFloat("counter_f", ref MainData.counter_f);

        ShowList("camera_keyframe", MainData.camera_keyframe.Count, i =>
        {
            if (ImGui.TreeNode(i.ToString()))
            {
                ImGui.InputInt("frame_no", ref MainData.camera_keyframe[i].frame_no);
                ImGui.InputInt("pre_index", ref MainData.camera_keyframe[i].pre_index);
                ImGui.InputInt("next_index", ref MainData.camera_keyframe[i].next_index);
                ImGui.InputFloat("length", ref MainData.camera_keyframe[i].length);
                ImGui.InputFloat3("xyz", ref MainData.camera_keyframe[i].xyz.X);
                ImGui.InputFloat3("rxyz", ref MainData.camera_keyframe[i].rxyz.X);
                fixed (void* ptr = MainData.camera_keyframe[i].hokan1_x) ImGui.InputScalarN("hokan1_x", ImGuiDataType.S8, ptr, 6, "%d");
                fixed (void* ptr = MainData.camera_keyframe[i].hokan1_y) ImGui.InputScalarN("hokan1_y", ImGuiDataType.S8, ptr, 6, "%d");
                fixed (void* ptr = MainData.camera_keyframe[i].hokan2_x) ImGui.InputScalarN("hokan2_x", ImGuiDataType.S8, ptr, 6, "%d");
                fixed (void* ptr = MainData.camera_keyframe[i].hokan2_y) ImGui.InputScalarN("hokan2_y", ImGuiDataType.S8, ptr, 6, "%d");
                ImGui.InputInt("is_perspective", ref MainData.camera_keyframe[i].is_perspective);
                ImGui.InputInt("view_angle", ref MainData.camera_keyframe[i].view_angle);
                ImGui.InputInt("is_selected", ref MainData.camera_keyframe[i].is_selected);
                ImGui.InputInt("looking_model_index", ref MainData.camera_keyframe[i].looking_model_index);
                ImGui.InputInt("looking_bone_index", ref MainData.camera_keyframe[i].looking_bone_index);

                ImGui.TreePop();
            }
        });

        ShowList("model_data", MainData.model_data.Count, i =>
        {
            if (ImGui.TreeNode(i.ToString()))
            {
                ImGui.Text($"name_jp: {MainData.model_data[i].name_jp}");
                ImGui.Text($"name_en: {MainData.model_data[i].name_en}");
                ImGui.Text($"comment_jp: {MainData.model_data[i].comment_jp}");
                ImGui.Text($"comment_en: {MainData.model_data[i].comment_en}");
                ImGui.Text($"file_path: {MainData.model_data[i].file_path}");

                ShowList("bone_current_data", MainData.model_data[i].bone_current_data.Count, j =>
                {
                    if (ImGui.TreeNode(j.ToString()))
                    {
                        ImGui.Text($"name_jp: {MainData.model_data[i].bone_current_data[j].name_jp}");
                        ImGui.Text($"name_en: {MainData.model_data[i].bone_current_data[j].name_en}");
                        ImGui.InputFloat3("init_xyz", ref MainData.model_data[i].bone_current_data[j].init_xyz.X);
                        ImGui.InputFloat3("xyz", ref MainData.model_data[i].bone_current_data[j].xyz.X);
                        ImGui.InputFloat3("xyz2", ref MainData.model_data[i].bone_current_data[j].xyz2.X);

                        ImGui.TreePop();
                    }
                });

                fixed (void* ptr = &MainData.model_data[i].keyframe_editor_toplevel_rows) ImGui.InputScalar("keyframe_editor_toplevel_rows", ImGuiDataType.S8, ptr, "%d");

                ShowList("bone_keyframe", MainData.model_data[i].bone_keyframe.Count, j =>
                {
                    if (ImGui.TreeNode(j.ToString()))
                    {
                        ImGui.InputInt("frame_number", ref MainData.model_data[i].bone_keyframe[j].frame_number);
                        ImGui.InputInt("pre_index", ref MainData.model_data[i].bone_keyframe[j].pre_index);
                        ImGui.InputInt("next_index", ref MainData.model_data[i].bone_keyframe[j].next_index);
                        fixed (void* ptr = MainData.model_data[i].bone_keyframe[j].interpolation_curve_x1) ImGui.InputScalarN("interpolation_curve_x1", ImGuiDataType.S8, ptr, 4, "%d");
                        fixed (void* ptr = MainData.model_data[i].bone_keyframe[j].interpolation_curve_y1) ImGui.InputScalarN("interpolation_curve_y1", ImGuiDataType.S8, ptr, 4, "%d");
                        fixed (void* ptr = MainData.model_data[i].bone_keyframe[j].interpolation_curve_x2) ImGui.InputScalarN("interpolation_curve_x2", ImGuiDataType.S8, ptr, 4, "%d");
                        fixed (void* ptr = MainData.model_data[i].bone_keyframe[j].interpolation_curve_y2) ImGui.InputScalarN("interpolation_curve_y2", ImGuiDataType.S8, ptr, 4, "%d");
                        ImGui.InputFloat3("xyz", ref MainData.model_data[i].bone_keyframe[j].xyz.X);
                        ImGui.InputFloat4("rotation_q", ref MainData.model_data[i].bone_keyframe[j].rotation_q.X);

                        ImGui.TreePop();
                    }
                });

                ShowList("morph_keyframe", MainData.model_data[i].morph_keyframe.Count, j =>
                {
                    if (ImGui.TreeNode(j.ToString()))
                    {
                        ImGui.InputInt("frame_number", ref MainData.model_data[i].morph_keyframe[j].frame_number);
                        ImGui.InputInt("pre_index", ref MainData.model_data[i].morph_keyframe[j].pre_index);
                        ImGui.InputInt("next_index", ref MainData.model_data[i].morph_keyframe[j].next_index);
                        ImGui.InputFloat("value", ref MainData.model_data[i].morph_keyframe[j].value);
                        fixed (void* ptr = &MainData.model_data[i].morph_keyframe[j].is_selected) ImGui.InputScalar("is_selected", ImGuiDataType.S8, ptr, "%d");

                        ImGui.TreePop();
                    }
                });

                ShowList("configuration_keyframe", MainData.model_data[i].configuration_keyframe.Count, j =>
                {
                    if (ImGui.TreeNode(j.ToString()))
                    {
                        ImGui.InputInt("frame_number", ref MainData.model_data[i].configuration_keyframe[j].frame_number);
                        ImGui.InputInt("pre_index", ref MainData.model_data[i].configuration_keyframe[j].pre_index);
                        ImGui.InputInt("next_index", ref MainData.model_data[i].configuration_keyframe[j].next_index);
                        fixed (void* ptr = &MainData.model_data[i].configuration_keyframe[j].is_visible) ImGui.InputScalar("is_visible", ImGuiDataType.S8, ptr, "%d");

                        ImGui.TreePop();
                    }

                    ShowList("is_ik_enabled", MainData.model_data[i].configuration_keyframe[j].is_ik_enabled.Length, k =>
                    {
                        fixed (void* ptr = &MainData.model_data[i].configuration_keyframe[j].is_ik_enabled[k]) ImGui.InputScalar(k.ToString(), ImGuiDataType.S8, ptr, "%d");
                    });

                    ShowList("relation_setting", MainData.model_data[i].configuration_keyframe[j].relation_setting.Count, k =>
                    {
                        if (ImGui.TreeNode(k.ToString()))
                        {
                            ImGui.InputInt("parent_model_index", ref MainData.model_data[i].configuration_keyframe[j].relation_setting[k].parent_model_index);
                            ImGui.InputInt("parent_bone_index", ref MainData.model_data[i].configuration_keyframe[j].relation_setting[k].parent_bone_index);

                            ImGui.TreePop();
                        }
                    });
                });

                fixed (void* ptr = &MainData.model_data[i].render_order) ImGui.InputScalar("render_order", ImGuiDataType.S8, ptr, "%d");
                ImGui.InputInt("bone_count", ref MainData.model_data[i].bone_count);
                ImGui.InputInt("morph_count", ref MainData.model_data[i].morph_count);
                ImGui.InputInt("ik_count", ref MainData.model_data[i].ik_count);
                fixed (void* ptr = &MainData.model_data[i].is_visible) ImGui.InputScalar("is_visible", ImGuiDataType.S8, ptr, "%d");
                ImGui.InputInt("selected_bone", ref MainData.model_data[i].selected_bone);
                fixed (void* ptr = MainData.model_data[i].selected_morph_indices) ImGui.InputScalarN("selected_morph_indices", ImGuiDataType.S32, ptr, 4, "%d");
                ImGui.InputInt("vscroll", ref MainData.model_data[i].vscroll);
                ImGui.InputInt("last_frame_number", ref MainData.model_data[i].last_frame_number);
                ImGui.InputInt("parentable_bone_count", ref MainData.model_data[i].parentable_bone_count);

                ImGui.TreePop();
            }
        });

        ImGui.InputInt("select_model", ref MainData.select_model);
        ImGui.InputInt("select_bone_type", ref MainData.select_bone_type);

        ImGui.InputInt("mouse_over_move", ref MainData.mouse_over_move);

        ImGui.InputInt("left_frame", ref MainData.left_frame);
        ImGui.InputInt("pre_left_frame", ref MainData.pre_left_frame);
        ImGui.InputInt("now_frame", ref MainData.now_frame);

        fixed (void* ptr = MainData.edit_interpolation_curve) ImGui.InputScalarN("edit_interpolation_curve", ImGuiDataType.S8, ptr, 4, "%d");

        fixed (void* ptr = &MainData.is_camera_select) ImGui.InputScalar("is_camera_select", ImGuiDataType.S8, ptr, "%d");
        ShowList("is_model_bone_select", MainData.is_model_bone_select.Length, i =>
        {
            fixed (void* ptr = &MainData.is_model_bone_select[i]) ImGui.InputScalar(i.ToString(), ImGuiDataType.S8, ptr, "%d");
        });

        ImGui.InputInt2("output_size_xy", ref MainData.output_size_xy.X);
        ImGui.InputFloat("length", ref MainData.length);

        ImGui.Text($"pmm_path: {MainData.pmm_path}");
    }

    public void TryAddLuaSource(string dirPath)
    {
        try
        {
            var dir = new DirectoryInfo(dirPath);
            if (!Saveable.LuaSources.Exists(src => src.Directory.FullName.Equals(dir.FullName)))
            {
                Saveable.LuaSources.Add(new LuaSource(dir));
                RefreshLuaScripts();
            }
        }
        catch (Exception ex)
        {
            ExceptionHandler(ex);
        }
    }

    public void RefreshLuaScripts()
    {
        try
        {
            RequiringParameters?.TrySetCanceled();

            Saveable.SelectedScripts.Clear();
            Saveable.LuaScripts.Clear();
            Saveable.LuaSources.ForEach(src => Saveable.LuaScripts.AddRange(src.Directory.GetFiles()
                .Where(file => file.Extension.Equals(".lua")).Select(file => new LuaScript(file))));
        }
        catch (Exception ex)
        {
            ExceptionHandler(ex);
        }
    }

    public void DeleteSelectedLuaSources()
    {
        Saveable.LuaSources.RemoveAll(src => src.selected);
        RefreshLuaScripts();
    }

    public void AddParameter(IParameter param)
    {
        if (_parameterQueue.TryDequeue(out object? value))
        {
            if (value is DefaultParameter)
            {
                _lua[param.Name] = param.Value;
            }
            else
            {
                _lua[param.Name] = value;
            }
        }
        else
        {
            Parameters.Add(param);
        }
    }

    public void ResetLuaState()
    {
        _lua = new();
        _lua.State.Encoding = Encoding.Default;

        _lua.LoadCLRPackage();
        _lua.DoString("luanet.load_assembly('Scallion')");

        _lua.DoString("mish = {}");
        _lua["mish.log"] = (object obj) => Log.AppendLine(obj.ToString());
        _lua["mish.beginParams"] = () =>
        {
            Parameters.Clear();
            RequiringParameters = new();
        };
        _lua["mish.requireInt"] = (string name, int defaultValue = default) => AddParameter(new Parameter<int>(name, defaultValue));
        _lua["mish.requireFloat"] = (string name, float defaultValue = default) => AddParameter(new Parameter<float>(name, defaultValue));
        _lua["mish.requireVec2"] = (string name, Vector2 defaultValue = default) => AddParameter(new Parameter<Vector2>(name, defaultValue));
        _lua["mish.requireVec3"] = (string name, Vector3 defaultValue = default) => AddParameter(new Parameter<Vector3>(name, defaultValue));
        _lua["mish.requireDouble"] = (string name, double defaultValue = default) => AddParameter(new Parameter<double>(name, defaultValue));
        _lua["mish.requireString"] = (string name, string defaultValue = "") => AddParameter(new Parameter<string>(name, defaultValue));
        _lua["mish.setNextParam"] = (object? value) => _parameterQueue.Enqueue(value);
        _lua["mish.setNextParamDefault"] = () => _parameterQueue.Enqueue(new DefaultParameter());
        _lua["mish.endParams"] = () =>
        {
            RequiringParameters!.Task.Wait();
            if (RequiringParameters.Task.Status == TaskStatus.Canceled)
            {
                _lua.DoString("do return end");
            }
            else
            {
                Parameters.ForEach(param =>
                {
                    _lua[param.Name] = param.Value;
                });
            }
        };
        _lua["mish.suppressOutputForCurrent"] = () => OutputEnabled[CurrentScriptIndex] = false;
    }

    public async Task RunScripts()
    {
        try
        {
            if (CurrentScriptIndex != -1)
            {
                return;
            }
            CurrentScriptIndex = 0;

            var pmmFile = new FileInfo(PmmFilePath);
            var previousWriteTime = pmmFile.Exists ? pmmFile.LastWriteTime : DateTime.MinValue;

            _saveProjectFinishedEvent.Reset();
            RequestSave();
            _saveProjectFinishedEvent.WaitOne();

            pmmFile = new FileInfo(PmmFilePath);
            var currentWriteTime = pmmFile.Exists ? pmmFile.LastWriteTime : DateTime.MinValue;
            if (previousWriteTime >= currentWriteTime)
            {
                CurrentScriptIndex = -1;
                return;
            }

            var project = new Project();
            if (File.Exists(PmmFilePath))
            {
                project = project.Load(PmmFilePath);
            }
            _lua["project"] = project;

            var motion = project.Panel.IsModelSelected ? new Motion
            {
                ModelName = project.Models[project.Panel.SelectedModelIndex].Name,
                Bones = [.. project.Models[project.Panel.SelectedModelIndex].Bones.Select(bone => new Bone { Name = bone.Name })],
                Morphs = [.. project.Models[project.Panel.SelectedModelIndex].Morphs.Select(morph => new Morph { Name = morph.Name })]
            }
            : new Motion
            {
                // Model name must be set, or null value will cause exception
                ModelName = string.Empty
            };
            _lua["motion"] = motion;

            _parameterQueue.Clear();
            OutputEnabled = [.. Enumerable.Repeat(true, Saveable.SelectedScripts.Count)];
            for (; CurrentScriptIndex < Saveable.SelectedScripts.Count; CurrentScriptIndex++)
            {
                _lua.DoFile(Saveable.SelectedScripts[CurrentScriptIndex].File.FullName);
                if (RequiringParameters is not null && RequiringParameters.Task.Status == TaskStatus.Canceled)
                {
                    CurrentScriptIndex = -1;
                    return;
                }
            }

            if (OutputEnabled.Exists(en => en))
            {
                motion.Save(VmdFilePath);
                RequestDropFile();
                ExceptionHandler("VMD file generated and dropped");
            }
        }
        catch (Exception ex)
        {
            ExceptionHandler(ex);
        }
        finally
        {
            CurrentScriptIndex = -1;
        }
    }

    public static string StripBasePath(string path)
    {
        var relative = Path.GetRelativePath(AppContext.BaseDirectory, path);
        if (relative.StartsWith(@"..\..\.."))
        {
            return path;
        }
        return relative;
    }

    public void SaveAsLua()
    {
        var dialogResult = Dialog.FileSave("lua", AppContext.BaseDirectory);
        if (dialogResult.IsOk)
        {
            var fileName = Path.GetExtension(dialogResult.Path).Equals(".lua") ? dialogResult.Path : dialogResult.Path + ".lua";
            var text = string.Join(Environment.NewLine, Saveable.SelectedScripts.Select(scr => $"dofile [[{StripBasePath(scr.File.FullName)}]]"));
            File.WriteAllText(fileName, text);
        }
    }
}
