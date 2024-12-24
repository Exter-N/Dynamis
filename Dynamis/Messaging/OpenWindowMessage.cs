using Dalamud.Interface.Windowing;

namespace Dynamis.Messaging;

public record OpenWindowMessage<T> where T : Window;
