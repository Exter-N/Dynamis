namespace Dynamis.UI;

public static class EnumExtensions
{
    public static bool IsPointer(this HexViewerColor color)
        => color switch
        {
            HexViewerColor.Pointer              => true,
            HexViewerColor.ObjectPointer        => true,
            HexViewerColor.CodePointer          => true,
            HexViewerColor.VirtualTablePointer  => true,
            HexViewerColor.LibraryObjectPointer => true,
            _                                   => false,
        };
}
