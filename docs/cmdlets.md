# Dynamis Hosted PowerShell Cmdlets

## Native Memory

### `Get-ClientStruct`

Alias: `gs`.

```powershell
Get-ClientStruct [-Address] <Object> [-Access <BoxAccess>]
```

Obtains a view of the native object at the given address (usually an `IntPtr` but it can be passed in other forms). `-Access Constant` or `-Access ShallowConstant` can be used to make the returned view read-only.

```powershell
Get-ClientStruct -Type <Type> [[-Name] <String>] [-Access <BoxAccess>]
```

Obtains a view of the well-known native object of the given type. If there are several well-known objects of the same type, the `-Name` parameter can be used to specify which one is desired.

## Threading

### `Invoke-OnFramework`

Alias: `ifx`.

```powershell
Invoke-OnFramework [-ScriptBlock] <ScriptBlock>
```

Runs the given block on the framework thread.

Using this cmdlet while already on the framework thread will result in an error.

## Dynamis Tools

### `Get-PluginService`

Alias: `gsv`.

```powershell
Get-PluginService [-Type] <Type>
```

Gets the Dynamis-provided or Dalamud-provided service of the given type.

### `Show-Object`

Aliases: `shobj`, `sho`.

```powershell
Show-Object [-Address] <Object> [[-Name] <String>]
```

Opens an object inspector on the object at the given address, or, if there is already one, brings it to the front.

### `Show-ObjectTable`

Alias: `shot`.

```powershell
Show-ObjectTable
```

Opens the object table viewer, or brings it to the front if it is already open.

## Lumina

### `Get-GameFile`

Alias: `ggf`.

```powershell
Get-GameFile [-Path] <String> [[-AsType] <Type>]
```

Obtains a view of the given game file.

If no type is specified, the view will be a base [`FileResource`](https://github.com/NotAdam/Lumina/blob/master/src/Lumina/Data/FileResource.cs).

## Inter-Plugin Services

### `Get-IpcFunction`

Alias: `gipc`.

```powershell
Get-IpcFunction [-Name] <String> [-Type] <Type>
```

Obtains a delegate of the given type for the IPC function of the given name.

### `Invoke-ChatCommand`

Alias: `icc`.

```powershell
Invoke-ChatCommand [-Command] <String>
```

Invokes the given Dalamud-provided or plugin-provided chat command, on the framework thread.

See also [`ICommandManager.ProcessCommand(string)`](https://dalamud.dev/api/Dalamud.Plugin.Services/Interfaces/ICommandManager/#processcommandstring).

## Output

### `Show-Log`

Aliases: `shlog`, `shl`.

```powershell
Show-Log
```

Opens the Dalamud log console.

### `Write-Chat`

Alias: `wrc`.

```powershell
Write-Chat [[-Object] <Object>]
```

Writes the given object to the chat log. Can be used at the end of a pipeline.

### `Write-Log`

Aliases: `wrlog`, `wrl`.

```powershell
Write-Log [[-Object] <Object>]
```

Writes the given object to the Dalamud log. Can be used at the end of a pipeline.
