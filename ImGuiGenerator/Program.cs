using System.Text;
using System.Text.RegularExpressions;

StringBuilder builder = new();

var lines = File.ReadAllLines(args[0]);
foreach (var line in lines[args.Length >= 3 ? (Convert.ToInt32(args[1]) - 1)..(Convert.ToInt32(args[2])) : 0..])
{
    var match = FieldRegex().Match(line);
    if (match.Success)
    {
        if (match.Groups[1].Value.Equals("int"))
        {
            builder.AppendLine($"""ImGui.InputInt("{match.Groups[3].Value}", ref XXXXXX.{match.Groups[3].Value});""");
        }
        else if (match.Groups[1].Value.Equals("float"))
        {
            builder.AppendLine($"""ImGui.InputFloat("{match.Groups[3].Value}", ref XXXXXX.{match.Groups[3].Value});""");
        }
        else if (match.Groups[1].Value.Equals("byte"))
        {
            builder.AppendLine($"""fixed (void* ptr = &XXXXXX.{match.Groups[3].Value}) ImGui.InputScalar("{match.Groups[3].Value}", ImGuiDataType.S8, ptr, "%d");""");
        }
        else if (match.Groups[1].Value.Equals("string"))
        {
            builder.AppendLine($$"""ImGui.Text($"{{match.Groups[3].Value}}: {XXXXXX.{{match.Groups[3].Value}}}");""");
        }
        else if (match.Groups[1].Value.Equals("Vector2D"))
        {
            if (match.Groups[2].Value.Equals("<int>"))
            {
                builder.AppendLine($"""ImGui.InputInt2("{match.Groups[3].Value}", ref XXXXXX.{match.Groups[3].Value}.X);""");
            }
            else if (match.Groups[2].Value.Equals("<float>"))
            {
                builder.AppendLine($"""ImGui.InputFloat2("{match.Groups[3].Value}", ref XXXXXX.{match.Groups[3].Value}.X);""");
            }
        }
        else if (match.Groups[1].Value.Equals("Vector3D"))
        {
            if (match.Groups[2].Value.Equals("<float>"))
            {
                builder.AppendLine($"""ImGui.InputFloat3("{match.Groups[3].Value}", ref XXXXXX.{match.Groups[3].Value}.X);""");
            }
        }
        else if (match.Groups[1].Value.Equals("Vector4D"))
        {
            if (match.Groups[2].Value.Equals("<float>"))
            {
                builder.AppendLine($"""ImGui.InputFloat4("{match.Groups[3].Value}", ref XXXXXX.{match.Groups[3].Value}.X);""");
            }
        }
        else if (match.Groups[1].Value.Equals("int[]"))
        {
            var subMatch = ElementCountRegex().Match(match.Groups[4].Value);
            var count = subMatch.Success ? Convert.ToInt32(subMatch.Groups[1].Value) : int.MaxValue;

            if (count <= 6)
            {
                builder.AppendLine($"""fixed (void* ptr = XXXXXX.{match.Groups[3].Value}) ImGui.InputScalarN("{match.Groups[3].Value}", ImGuiDataType.S32, ptr, {count},"%d");""");
            }
            else
            {
                builder.AppendLine($$"""
ShowList("{{match.Groups[3].Value}}", XXXXXX.{{match.Groups[3].Value}}.Length, i =>
{
    ImGui.InputScalar(i.ToString(), ImGuiDataType.S32, ref XXXXXX.{{match.Groups[3].Value}}[i], "%d");
});
""");
            }
        }
        else if (match.Groups[1].Value.Equals("float[]"))
        {
            var subMatch = ElementCountRegex().Match(match.Groups[4].Value);
            var count = subMatch.Success ? Convert.ToInt32(subMatch.Groups[1].Value) : int.MaxValue;

            if (count <= 6)
            {
                builder.AppendLine($"""fixed (void* ptr = XXXXXX.{match.Groups[3].Value}) ImGui.InputScalarN("{match.Groups[3].Value}", ImGuiDataType.Float, ptr, {count},"%d");""");
            }
            else
            {
                builder.AppendLine($$"""
ShowList("{{match.Groups[3].Value}}", XXXXXX.{{match.Groups[3].Value}}.Length, i =>
{
    ImGui.InputScalar(i.ToString(), ImGuiDataType.Float, ref XXXXXX.{{match.Groups[3].Value}}[i], "%d");
});
""");
            }
        }
        else if (match.Groups[1].Value.Equals("byte[]"))
        {
            var subMatch = ElementCountRegex().Match(match.Groups[4].Value);
            var count = subMatch.Success ? Convert.ToInt32(subMatch.Groups[1].Value) : int.MaxValue;

            if (count <= 6)
            {
                builder.AppendLine($"""fixed (void* ptr = XXXXXX.{match.Groups[3].Value}) ImGui.InputScalarN("{match.Groups[3].Value}", ImGuiDataType.S8, ptr, {count},"%d");""");
            }
            else
            {
                builder.AppendLine($$"""
ShowList("{{match.Groups[3].Value}}", XXXXXX.{{match.Groups[3].Value}}.Length, i =>
{
    fixed (void* ptr = &XXXXXX.{{match.Groups[3].Value}}[i]) ImGui.InputScalar(i.ToString(), ImGuiDataType.S8, ptr, "%d");
});
""");
            }
        }
        else
        {
            builder.AppendLine($"""// {match.Groups[0].Value}""");
        }
    }
    else
    {
        builder.AppendLine();
    }
}

Console.WriteLine(builder.ToString());

partial class Program
{
    [GeneratedRegex(@"public (.+?)((?:<.+?>)?) (.+?)((?: *=.+?)?);")]
    private static partial Regex FieldRegex();

    [GeneratedRegex(@"\[(\d+)\]")]
    private static partial Regex ElementCountRegex();
}