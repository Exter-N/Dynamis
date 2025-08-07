#if WITH_SMA
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility.Raii;
using Dynamis.UI.Components;
using Dynamis.Utility;

namespace Dynamis.UI.PsHost.Input;

public sealed class FormPrompt(
    string? caption,
    string? message,
    Collection<FieldDescription> descriptions) : BaseFormPrompt(caption, message), IPrompt<Dictionary<string, PSObject>>
{
    private readonly ImmutableArray<FormItem> _items = [..descriptions.Select(Compile),];

    private Dictionary<string, PSObject>? _finishedValue;

    protected override int FrameRows
        => _items.Length + 1;

    public Dictionary<string, PSObject> Result
        => _finishedValue ?? throw new InvalidOperationException("Value not yet submitted");

    protected override bool DrawForm(ref bool focus)
    {
        if (_finishedValue is not null) {
            foreach (var item in _items) {
                item.Input.Draw(ImGuiInputTextFlags.ReadOnly);
                DrawHelpMessage(item.HelpMessage);
            }

            using (ImRaii.Disabled()) {
                ImGui.Button("Submit");
            }

            return true;
        }

        if (focus) {
            ImGui.SetKeyboardFocusHere();
            focus = false;
        }

        var enter = false;
        foreach (var item in _items) {
            if (enter) {
                ImGui.SetKeyboardFocusHere();
            }

            enter = item.Input.Draw(ImGuiInputTextFlags.EnterReturnsTrue);
            DrawHelpMessage(item.HelpMessage);
        }

        enter |= ImGui.Button("Submit");

        if (enter) {
            _finishedValue ??= GetValue();
        }

        return enter;
    }

    private static void DrawHelpMessage(string helpMessage)
    {
        if (!string.IsNullOrEmpty(helpMessage)) {
            ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
            ImGuiComponents.NormalizedIcon(
                FontAwesomeIcon.InfoCircle, StyleModel.GetFromCurrent()!.BuiltInColors!.ParsedBlue!.Value.ToUInt32()
            );

            if (ImGui.IsItemHovered()) {
                using var tt = ImRaii.Tooltip();
                ImGui.TextUnformatted(helpMessage);
            }
        }
    }

    public override void Cancel()
        => _finishedValue ??= GetValue();

    private Dictionary<string, PSObject> GetValue()
    {
        var result = new Dictionary<string, PSObject>();
        foreach (var item in _items) {
            result[item.Key] = item.GetResult();
        }

        return result;
    }

    private static FormItem Compile(FieldDescription description)
    {
        if (description.ParameterTypeFullName.ToFieldType() is not null) {
            var type = Type.GetType(description.ParameterTypeFullName);
            return (FormItem)typeof(FormPrompt)
                            .GetMethod(
                                 nameof(CompileScalar),
                                 BindingFlags.Static | BindingFlags.NonPublic
                             )!
                            .MakeGenericMethod(type!)
                            .Invoke(null, [description,])!;
        }

        var label = $"{description.Label.ParseAccelerator().Label}###{description.Name}";
        if (string.Equals(
                description.ParameterTypeFullName, "System.Security.SecureString", StringComparison.Ordinal
            )) {
            return EndCompile(description, new SecurePasswordInput(label, 2048));
        }

        return EndCompile(
            description, new TextInput(label, 2048, description.DefaultValue?.ToString() ?? string.Empty)
        );
    }

    private static FormItem CompileScalar<T>(FieldDescription description)
        where T : unmanaged, IBinaryNumber<T>
    {
        var label = $"{description.Label.ParseAccelerator().Label}###{description.Name}";
        T initialValue;
        if (description.DefaultValue?.BaseObject is
            {
            } baseObject) {
            initialValue = (T)Convert.ChangeType(baseObject, typeof(T));
        } else {
            initialValue = default;
        }

        var input = new ScalarInput<T>(label, initialValue);
        return EndCompile(description, input);
    }

    private static FormItem EndCompile<T>(FieldDescription description,
        IInput<T> input)
        => new(description.Name, () => new(input.GetValue()), input, description.HelpMessage ?? string.Empty);

    private readonly record struct FormItem(string Key, Func<PSObject> GetResult, IInput Input, string HelpMessage);
}
#endif
