# Dynamis Inter-Plugin API

All the function and event names are prefixed with `Dynamis.`, and most are suffixed with their major version number, for example, `Dynamis.InspectObject.V1`.

For the sake of brevity, IPC signatures will be shown as C#-like pseudocode, with the IPC name string instead of a function name, and `event` instead of the return type for events.

Once a function/event is published, its signature will never change until its removal ; if the needs change, a new function/event will be published with a bumped version number and/or a different name.

See also: [IpcProvider.cs](../Dynamis/Messaging/IpcProvider.cs).

For an example of a consumer implementation, see [DynamisIpc.cs from Ottermandias/OtterGui](https://github.com/Ottermandias/OtterGui/blob/main/Services/DynamisIpc.cs).

## Version and Lifecycle

### `GetApiVersion` function

```csharp
(uint MajorVersion, uint MinorVersion, ulong FeatureFlags) "Dynamis.GetApiVersion"();
```

Returns the current API version. Dynamis' API versioning scheme is as follows:
- `MajorVersion` will be incremented (and `MinorVersion` will return to 0) on any backwards-incompatible changes (for example removal of functions, addition of restrictions on functions/parameters) ;
- `MinorVersion` will be incremented on backwards-compatible baseline changes (for example addition of functions, removal of restrictions on functions/parameters) ;
- `FeatureFlags` is a bit field that indicates which optional features are available (compiled in and/or enabled by user).

Compatibility can be checked using the following function:
```csharp
bool IsCompatible((uint MajorVersion, uint MinorVersion, ulong FeatureFlags) actual, (uint MajorVersion, uint MinorVersion, ulong FeatureFlags) required)
    => actual.MajorVersion == required.MajorVersion && actual.MinorVersion >= required.MinorVersion && (actual.FeatureFlags & required.FeatureFlags) == required.FeatureFlags;
```

Minimum API version: `(1, 3, 0)`.

### `ApiInitialized` event

```csharp
event "Dynamis.ApiInitialized"(uint apiMajorVersion, uint apiMinorVersion, ulong apiFeatureFlags, Version pluginVersion);
```

This event will be dispatched when Dynamis' API is ready to use.

- `apiMajorVersion`, `apiMinorVersion` and `apiFeatureFlags` have the same semantics as in `GetApiVersion` ;
- `pluginVersion` is the plugin's version, that can otherwise be retrieved by inspecting Dalamud's plugin list.

Minimum API version: `(1, 3, 0)`.

### `ApiDisposing` event

```csharp
event "Dynamis.ApiDisposing"();
```

This event will be dispatched just before Dynamis' API stops being available.

Minimum API version: `(1, 3, 0)`.

## ImGui Components

### `ImGuiDrawPointer` function

```csharp
void "Dynamis.ImGuiDrawPointer.V1"(nint pointer);
```

Draws a pointer in hexadecimal in a monospace font (or "nullptr").

On hover, displays a tooltip with various info about the object behind that pointer, if possible.

On click, offers several actions, such as copying the address to the clipboard, or inspecting the object.

**MUST** be called from a valid ImGui context.

If Dynamis' API is unavailable, a minimal fallback implementation **SHOULD** be used by the caller instead.

Minimum API version: `(1, 0, 0)`.

```csharp
Action<nint> "Dynamis.GetImGuiDrawPointerDelegate.V1"();
```

Obtains a delegate for the `ImGuiDrawPointer` function, to avoid the IPC overhead if you want to draw a lot of pointers (in a list/table for example).

The obtained delegate either **MUST NOT** be cached across frames, or **MUST** be discarded in response to the `ApiDisposing` event, in order not to cause plugin unloading issues.

Minimum API version: `(1, 3, 0)`.

### `ImGuiDrawPointerTooltipDetails` function

```csharp
void "Dynamis.ImGuiDrawPointerTooltipDetails.V1"(nint pointer);
```

Draws various info about the object behind the given pointer, if possible.

No interactivity is provided, as this is designed to be displayed in a tooltip.

**MUST** be called from a valid ImGui context.

Minimum API version: `(1, 0, 0)`.

## Windows

### `InspectObject` function

```csharp
void "Dynamis.InspectObject.V1"(nint address);
```

Opens an object inspector on the object at the given address, or, if there is already one, brings it to the front.

Minimum API version: `(1, 0, 0)`.

### `InspectRegion` function

```csharp
void "Dynamis.InspectRegion.V1"(nint address, uint size, string typeName, uint typeTemplateId, uint classKindId);
```

Opens an object inspector on the given region.

- `address` and `size` describe the region to inspect ;
- `typeName` is a type name to display (it doesn't have to match any known type name) ;
- `typeTemplateId` is a type template to use to auto-generate fields - pass 0 not to use this feature (see the `Template` nested enum in [PseudoClasses.cs](../Dynamis/Interop/PseudoClasses.cs)) ;
- `classKindId` is a class kind that defines which additional inspectors are applicable - pass 0 not to use this feature (see [ClassKind.cs](../Dynamis/Interop/ClassKind.cs)).

Minimum API version: `(1, 0, 0)`.

## Native Reflection

### `GetClass` function

```csharp
(string Name, Type? Type, uint Size, uint Displacement) "Dynamis.GetClass.V1"(nint pointer);
```

Determines the class of the object at the given pointer.

- `Name` is the class name, if it could be successfully determined, otherwise it will be auto-generated using the object's vtbl pointer, as displayed in the object inspector ;
- `Type` is the FFXIVClientStructs type that best represents the object's class, if one could be successfully determined, otherwise `null` ;
- `Size` is an estimate of the object's size, as displayed in the object inspector ;
- `Displacement` is the offset from the object's start to the given pointer (for example, it can be non-zero when given a pointer to a second subclass of an object with multiple inheritance).

Minimum API version: `(1, 1, 0)`.

### `IsInstanceOf` function

```csharp
(bool IsInstance, uint Displacement) "Dynamis.IsInstanceOf.V1"(nint pointer, string? className, Type? type);
```

Determines whether the object at the given pointer is of the given class.

Exactly one of `className` or `type` **MUST NOT** be `null`, the other **MUST** be.

Minimum API version: `(1, 1, 0)`.

## Miscellaneous

### `PreloadDataYaml` function

```csharp
void "Dynamis.PreloadDataYaml.V1"();
```

Preloads the `data.yml` file, in order to avoid a UI hitch due to it being loaded when first needed.

If it was already loaded, this function is a no-op.

Minimum API version: `(1, 2, 0)`.
