param(
    [Parameter(Mandatory=$false)]
    [string]$GamePath = "",

    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

function Find-GamePath {
    param([string]$RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $resolved = Resolve-Path -LiteralPath $RequestedPath -ErrorAction SilentlyContinue
        if ($resolved) { return $resolved.Path }
        return $RequestedPath
    }

    $candidate = $PSScriptRoot
    for ($i = 0; $i -lt 7 -and -not [string]::IsNullOrWhiteSpace($candidate); $i++) {
        $assembly = Join-Path $candidate "7DaysToDie_Data\Managed\Assembly-CSharp.dll"
        if (Test-Path -LiteralPath $assembly) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) { break }
        $candidate = $parent
    }

    return ""
}

function Safe-TypeName {
    param($Type)
    if ($null -eq $Type) { return "<null>" }
    try {
        if (-not [string]::IsNullOrWhiteSpace($Type.FullName)) { return $Type.FullName }
    }
    catch { }
    try { return $Type.ToString() } catch { return "<unknown>" }
}

function Safe-Parameters {
    param($Method)
    try { return @($Method.GetParameters()) } catch { return @() }
}

function Format-Method {
    param($Method)

    $name = try { $Method.Name } catch { "<unknown>" }
    try {
        $parameters = @($Method.GetParameters() | ForEach-Object {
            "$(Safe-TypeName $_.ParameterType) $($_.Name)"
        })
        $returnName = Safe-TypeName $Method.ReturnType
        return "$returnName $name($($parameters -join ', '))"
    }
    catch {
        return "[METHOD PARAMETER ERROR] ${name}: $($_.Exception.GetType().Name): $($_.Exception.Message)"
    }
}

function Format-Constructor {
    param($Constructor)

    try {
        $parameters = @($Constructor.GetParameters() | ForEach-Object {
            "$(Safe-TypeName $_.ParameterType) $($_.Name)"
        })
        return "$($Constructor.DeclaringType.FullName)($($parameters -join ', '))"
    }
    catch {
        return "[CONSTRUCTOR PARAMETER ERROR] $($_.Exception.GetType().Name): $($_.Exception.Message)"
    }
}

function Get-AllLoadableTypes {
    param($Assembly, $Lines)

    try {
        return @($Assembly.GetTypes())
    }
    catch [System.Reflection.ReflectionTypeLoadException] {
        $types = @($_.Exception.Types | Where-Object { $_ -ne $null })
        $Lines.Add("[NOTE] GetTypes partially failed; recovered $($types.Count) loadable types.")
        foreach ($loaderException in $_.Exception.LoaderExceptions | Select-Object -First 20) {
            if ($loaderException) {
                $Lines.Add("[LOADER] $($loaderException.GetType().Name): $($loaderException.Message)")
            }
        }
        return $types
    }
}

function New-OpcodeMaps {
    $oneByte = @{}
    $twoByte = @{}
    $fields = [System.Reflection.Emit.OpCodes].GetFields(
        [System.Reflection.BindingFlags] "Public,Static")

    foreach ($field in $fields) {
        try {
            $opcode = [System.Reflection.Emit.OpCode]$field.GetValue($null)
            $value = ([int]$opcode.Value) -band 0xffff
            if ($value -le 0xff) {
                $oneByte[$value] = $opcode
            }
            elseif (($value -band 0xff00) -eq 0xfe00) {
                $twoByte[$value -band 0xff] = $opcode
            }
        }
        catch { }
    }

    return @($oneByte, $twoByte)
}

$opcodeMaps = New-OpcodeMaps
$oneByteOpcodes = $opcodeMaps[0]
$twoByteOpcodes = $opcodeMaps[1]

function Get-CalledMethods {
    param($Method)

    $results = New-Object System.Collections.Generic.List[string]
    $body = $null
    try { $body = $Method.GetMethodBody() } catch { }
    if ($null -eq $body) { return @() }

    $bytes = $null
    try { $bytes = $body.GetILAsByteArray() } catch { }
    if ($null -eq $bytes -or $bytes.Length -eq 0) { return @() }

    $module = $Method.Module
    $typeArgs = @()
    $methodArgs = @()
    try {
        if ($Method.DeclaringType -and $Method.DeclaringType.IsGenericType) {
            $typeArgs = @($Method.DeclaringType.GetGenericArguments())
        }
    }
    catch { }
    try {
        if ($Method.IsGenericMethod) {
            $methodArgs = @($Method.GetGenericArguments())
        }
    }
    catch { }

    $offset = 0
    while ($offset -lt $bytes.Length) {
        $first = [int]$bytes[$offset]
        $offset++

        $opcode = $null
        if ($first -eq 0xfe) {
            if ($offset -ge $bytes.Length) { break }
            $second = [int]$bytes[$offset]
            $offset++
            if ($twoByteOpcodes.ContainsKey($second)) { $opcode = $twoByteOpcodes[$second] }
        }
        elseif ($oneByteOpcodes.ContainsKey($first)) {
            $opcode = $oneByteOpcodes[$first]
        }

        if ($null -eq $opcode) { break }

        $operandSize = 0
        $inlineMethodToken = $null
        switch ($opcode.OperandType.ToString()) {
            "InlineNone" { $operandSize = 0 }
            "ShortInlineBrTarget" { $operandSize = 1 }
            "ShortInlineI" { $operandSize = 1 }
            "ShortInlineVar" { $operandSize = 1 }
            "InlineVar" { $operandSize = 2 }
            "InlineI" { $operandSize = 4 }
            "InlineBrTarget" { $operandSize = 4 }
            "ShortInlineR" { $operandSize = 4 }
            "InlineField" { $operandSize = 4 }
            "InlineSig" { $operandSize = 4 }
            "InlineString" { $operandSize = 4 }
            "InlineTok" { $operandSize = 4 }
            "InlineType" { $operandSize = 4 }
            "InlineMethod" {
                $operandSize = 4
                if ($offset + 4 -le $bytes.Length) {
                    $inlineMethodToken = [BitConverter]::ToInt32($bytes, $offset)
                }
            }
            "InlineI8" { $operandSize = 8 }
            "InlineR" { $operandSize = 8 }
            "InlineSwitch" {
                if ($offset + 4 -gt $bytes.Length) { return @($results) }
                $count = [BitConverter]::ToInt32($bytes, $offset)
                $operandSize = 4 + (4 * $count)
            }
            default { $operandSize = 0 }
        }

        if ($null -ne $inlineMethodToken) {
            try {
                $resolved = $module.ResolveMethod($inlineMethodToken, $typeArgs, $methodArgs)
                if ($resolved) {
                    $declaring = Safe-TypeName $resolved.DeclaringType
                    $results.Add("$($opcode.Name) $declaring.$(Format-Method $resolved)")
                }
            }
            catch {
                $results.Add("$($opcode.Name) <unresolved token 0x$($inlineMethodToken.ToString('X8'))>: $($_.Exception.Message)")
            }
        }

        $offset += $operandSize
        if ($offset -gt $bytes.Length) { break }
    }

    return @($results | Select-Object -Unique)
}

function Add-TypeReport {
    param($Type, $Lines, [bool]$IncludeCalls = $false)

    if ($null -eq $Type) { return }

    $flags = [System.Reflection.BindingFlags] "Instance,Static,Public,NonPublic,DeclaredOnly"
    $Lines.Add("============================================================")
    $Lines.Add("TYPE: $($Type.FullName)")
    try { $Lines.Add("BaseType: $(Safe-TypeName $Type.BaseType)") } catch { }
    try { $Lines.Add("Assembly: $($Type.Assembly.FullName)") } catch { }

    $Lines.Add("")
    $Lines.Add("CONSTRUCTORS:")
    try {
        foreach ($constructor in $Type.GetConstructors($flags) | Sort-Object { (Safe-Parameters $_).Count }) {
            $Lines.Add("  $(Format-Constructor $constructor)")
        }
    }
    catch { $Lines.Add("  [ERROR] $($_.Exception.Message)") }

    $Lines.Add("")
    $Lines.Add("FIELDS:")
    try {
        foreach ($field in $Type.GetFields($flags) | Sort-Object Name) {
            $Lines.Add("  $(Safe-TypeName $field.FieldType) $($field.Name)")
        }
    }
    catch { $Lines.Add("  [ERROR] $($_.Exception.Message)") }

    $Lines.Add("")
    $Lines.Add("PROPERTIES:")
    try {
        foreach ($property in $Type.GetProperties($flags) | Sort-Object Name) {
            $Lines.Add("  $(Safe-TypeName $property.PropertyType) $($property.Name)")
        }
    }
    catch { $Lines.Add("  [ERROR] $($_.Exception.Message)") }

    $Lines.Add("")
    $Lines.Add("METHODS:")
    $methods = @()
    try { $methods = @($Type.GetMethods($flags) | Sort-Object Name) } catch { }
    foreach ($method in $methods) {
        $Lines.Add("  $(Format-Method $method)")

        if ($IncludeCalls -and $method.Name -match "OnBlockActivated|SetBlockRPC|SetBlocksRPC|Send|Register|GetPackage|ProcessPackage") {
            $called = @(Get-CalledMethods $method)
            if ($called.Count -gt 0) {
                $Lines.Add("    CALLED METHODS:")
                foreach ($call in $called) { $Lines.Add("      $call") }
            }
        }
    }

    $Lines.Add("")
}

$GamePath = Find-GamePath -RequestedPath $GamePath
if ([string]::IsNullOrWhiteSpace($GamePath)) {
    throw "Could not locate a 7 Days To Die installation above the probe script. Pass -GamePath 'C:\path\to\7 Days To Die'."
}

$managed = Join-Path $GamePath "7DaysToDie_Data\Managed"
$assemblyPath = Join-Path $managed "Assembly-CSharp.dll"
if (-not (Test-Path -LiteralPath $assemblyPath)) {
    throw "Assembly-CSharp.dll not found at: $assemblyPath"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot "LeezGrowLights_MultiplayerLightSyncProbe_V3.1.txt"
}

Push-Location $managed
try {
    $asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $lines = New-Object System.Collections.Generic.List[string]

    $file = Get-Item -LiteralPath $assemblyPath
    $hash = Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256
    $fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($assemblyPath)

    $lines.Add("LeezGrowLights Multiplayer Light Sync API probe")
    $lines.Add("Generated: $(Get-Date -Format o)")
    $lines.Add("GamePath: $GamePath")
    $lines.Add("Assembly: $assemblyPath")
    $lines.Add("AssemblyFullName: $($asm.FullName)")
    $lines.Add("AssemblyMVID: $($asm.ManifestModule.ModuleVersionId)")
    $lines.Add("AssemblyLength: $($file.Length)")
    $lines.Add("AssemblySHA256: $($hash.Hash)")
    $lines.Add("FileVersion: $($fileVersion.FileVersion)")
    $lines.Add("ProductVersion: $($fileVersion.ProductVersion)")
    $lines.Add("PowerShell: $($PSVersionTable.PSVersion)")
    $lines.Add("CLR: $([System.Environment]::Version)")
    $lines.Add("")

    $allTypes = @(Get-AllLoadableTypes -Assembly $asm -Lines $lines)

    $lines.Add("============================================================")
    $lines.Add("DISCOVERED NETWORK/PACKAGE TYPES")
    foreach ($type in $allTypes | Where-Object {
        try {
            $_.Name -match "NetPackage|Connection|Network|ClientInfo|PooledBinaryReader|PooledBinaryWriter"
        }
        catch { $false }
    } | Sort-Object FullName) {
        try { $lines.Add("  $($type.FullName) : $(Safe-TypeName $type.BaseType)") } catch { }
    }
    $lines.Add("")

    $lines.Add("============================================================")
    $lines.Add("DISCOVERED RPC/SEND/REGISTRATION METHODS")
    $methodPattern = "SetBlockRPC|SetBlocksRPC|SendPackage|SendToServer|SendToClient|Broadcast|RegisterPackage|GetPackage|ProcessPackage"
    foreach ($type in $allTypes | Sort-Object FullName) {
        $matches = @()
        try {
            $flags = [System.Reflection.BindingFlags] "Instance,Static,Public,NonPublic,DeclaredOnly"
            $matches = @($type.GetMethods($flags) | Where-Object { $_.Name -match $methodPattern })
        }
        catch { }

        if ($matches.Count -eq 0) { continue }
        $lines.Add("TYPE: $($type.FullName)")
        foreach ($method in $matches | Sort-Object Name) {
            $lines.Add("  $(Format-Method $method)")
            foreach ($call in @(Get-CalledMethods $method)) {
                $lines.Add("    CALLS: $call")
            }
        }
    }
    $lines.Add("")

    $detailNames = New-Object System.Collections.Generic.HashSet[string]
    foreach ($name in @(
        "ConnectionManager",
        "GameManager",
        "WorldBase",
        "World",
        "BlockPoweredLight",
        "BlockChangeInfo",
        "BlockValueRef"
    )) {
        [void]$detailNames.Add($name)
    }

    foreach ($type in $allTypes) {
        try {
            if ($type.Name -match "^NetPackage$|NetPackageManager|PooledBinaryReader|PooledBinaryWriter") {
                [void]$detailNames.Add($type.FullName)
            }
            elseif ($type.Name -match "NetPackage" -and $type.Name -match "Block|Tile|Light|Prop|Command|Action|Activ|Custom") {
                [void]$detailNames.Add($type.FullName)
            }
        }
        catch { }
    }

    $lines.Add("============================================================")
    $lines.Add("DETAILED RELEVANT TYPE REPORTS")
    $lines.Add("")

    foreach ($name in $detailNames | Sort-Object) {
        $type = $null
        try { $type = $asm.GetType($name, $false, $false) } catch { }
        if (-not $type) {
            $type = $allTypes | Where-Object { $_.Name -eq $name } | Select-Object -First 1
        }
        if ($type) {
            Add-TypeReport -Type $type -Lines $lines -IncludeCalls $true
        }
        else {
            $lines.Add("TYPE NOT FOUND: $name")
            $lines.Add("")
        }
    }

    $lines.Add("============================================================")
    $lines.Add("PROBE COMPLETED")

    [System.IO.File]::WriteAllLines($OutputPath, $lines)
    Write-Host "Multiplayer Light Sync API report written to: $OutputPath" -ForegroundColor Green
}
finally {
    Pop-Location
}
