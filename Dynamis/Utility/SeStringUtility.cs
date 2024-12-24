using System.Runtime.CompilerServices;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Dynamis.Utility;

public static class SeStringUtility
{
    public static UIForegroundPayload UiForegroundOff
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => UIForegroundPayload.UIForegroundOff;
    }

    public static UIGlowPayload UiGlowOff
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => UIGlowPayload.UIGlowOff;
    }

    public static EmphasisItalicPayload ItalicsOn
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => EmphasisItalicPayload.ItalicsOn;
    }

    public static EmphasisItalicPayload ItalicsOff
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => EmphasisItalicPayload.ItalicsOff;
    }

    public static IEnumerable<Payload> Italics(string text)
    {
        yield return EmphasisItalicPayload.ItalicsOn;
        yield return new TextPayload(text);
        yield return EmphasisItalicPayload.ItalicsOff;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UIForegroundPayload UiForeground(ushort colorKey)
        => new(colorKey);

    public static IEnumerable<Payload> UiForeground(string text, ushort colorKey)
    {
        yield return new UIForegroundPayload(colorKey);
        yield return new TextPayload(text);
        yield return UIForegroundPayload.UIForegroundOff;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UIGlowPayload UiGlow(ushort colorKey)
        => new(colorKey);

    public static IEnumerable<Payload> UiGlow(string text, ushort colorKey)
    {
        yield return new UIGlowPayload(colorKey);
        yield return new TextPayload(text);
        yield return UIGlowPayload.UIGlowOff;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IconPayload Icon(BitmapFontIcon icon)
        => new(icon);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeString ItemLink(uint itemId, bool isHq, string? displayNameOverride = null)
        => SeString.CreateItemLink(itemId, isHq, displayNameOverride);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeString ItemLink(uint itemId, ItemPayload.ItemKind kind = ItemPayload.ItemKind.Normal,
        string? displayNameOverride = null)
        => SeString.CreateItemLink(itemId, kind, displayNameOverride);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemPayload ItemLinkRaw(uint rawItemId)
        => ItemPayload.FromRaw(rawItemId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MapLinkPayload MapLink(uint territoryTypeId, uint mapId, float niceXCoord, float niceYCoord,
        float fudgeFactor = 0.05f)
        => new(territoryTypeId, mapId, niceXCoord, niceYCoord, fudgeFactor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static QuestPayload QuestLink(uint questId)
        => new(questId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatusPayload StatusLink(uint statusId)
        => new(statusId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PartyFinderPayload PartyFinderSearchConditionsLink()
        => new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PartyFinderPayload PartyFinderLink(uint id, bool isCrossWorld = false)
        => new(
            id,
            isCrossWorld
                ? PartyFinderPayload.PartyFinderLinkType.NotSpecified
                : PartyFinderPayload.PartyFinderLinkType.LimitedToHomeWorld
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeString BuildSeString(ref SeStringInterpolatedStringHandler handler)
    {
        handler.Flush();
        return handler.StringBuilder.Build();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeString BuildSeString(IFormatProvider? provider,
        [InterpolatedStringHandlerArgument(nameof(provider))]
        ref SeStringInterpolatedStringHandler handler)
    {
        handler.Flush();
        return handler.StringBuilder.Build();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeStringBuilder Add(this SeStringBuilder sb,
        [InterpolatedStringHandlerArgument(nameof(sb))]
        ref SeStringInterpolatedStringHandler handler)
    {
        handler.Flush();
        return sb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeStringBuilder Add(this SeStringBuilder sb, IFormatProvider? provider,
        [InterpolatedStringHandlerArgument(nameof(sb), nameof(provider))]
        ref SeStringInterpolatedStringHandler handler)
    {
        handler.Flush();
        return sb;
    }
}
