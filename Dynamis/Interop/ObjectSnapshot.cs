namespace Dynamis.Interop;

public class ObjectSnapshot(byte[] data)
{
    public nint?   Address { get; set; }
    public string? Name    { get; set; }

    public bool Live { get; set; } = true;

    public byte[] Data { get; set; } = data;

    public ClassInfo? Class           { get; set; }
    public byte[]?    HighlightColors { get; set; }

    public ObjectSnapshot? AssociatedSnapshot { get; set; }
}
