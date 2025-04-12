using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;

namespace Dynamis.UI.PsHost.Output;

public sealed class TextParagraph : IParagraph
{
    private SeStringBuilder? _builder = new();
    private byte[]?          _value;

    public void Append(string value)
    {
        if (_builder is null) {
            throw new InvalidOperationException("Paragraph has been ended");
        }

        lock (this) {
            _value = null;
            _builder.Append(value);
        }
    }

    public void Append(IEnumerable<Payload> value)
    {
        if (_builder is null) {
            throw new InvalidOperationException("Paragraph has been ended");
        }

        lock (this) {
            _value = null;
            _builder.Append(value);
        }
    }

    public void End()
    {
        if (_builder is null) {
            return;
        }

        lock (this) {
            Update();
            _builder = null;
        }
    }

    public static TextParagraph Create(string value)
    {
        var paragraph = new TextParagraph();
        paragraph.Append(value);
        paragraph.End();
        return paragraph;
    }

    public static TextParagraph Create(IEnumerable<Payload> value)
    {
        var paragraph = new TextParagraph();
        paragraph.Append(value);
        paragraph.End();
        return paragraph;
    }

    private void Update()
        => _value ??= _builder!.Build().EncodeWithNullTerminator();

    public void Draw()
    {
        lock (this) {
            Update();
            ImGuiHelpers.SeStringWrapped(_value!.AsSpan(..^1));
        }
    }
}
