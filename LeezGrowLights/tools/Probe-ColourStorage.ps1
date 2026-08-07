param(
    [Parameter(Mandatory=$false)]
    [string]$GamePath = "",

    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($GamePath)) {
    $candidate = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..") -ErrorAction SilentlyContinue
    if ($candidate) { $GamePath = $candidate.Path }
}

$managed = Join-Path $GamePath "7DaysToDie_Data\Managed"
$assemblyPath = Join-Path $managed "Assembly-CSharp.dll"
$blocksPath = Join-Path $GamePath "Data\Config\blocks.xml"

if (-not (Test-Path $assemblyPath)) {
    throw "Assembly-CSharp.dll not found at: $assemblyPath"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot "LeezGrowLights_ColourStorageProbe.txt"
}

function Safe-TypeName($t) {
    if ($null -eq $t) { return "<null>" }
    try { return $t.FullName } catch { return $t.ToString() }
}

function Format-Method {
    param($Method)
    try {
        $parameters = @($Method.GetParameters() | ForEach-Object {
            "$(Safe-TypeName $_.ParameterType) $($_.Name)"
        })
        $returnName = Safe-TypeName $Method.ReturnType
        return "$returnName $($Method.Name)($($parameters -join ', '))"
    }
    catch {
        return "[METHOD ERROR] $($Method.Name): $($_.Exception.GetType().Name): $($_.Exception.Message)"
    }
}

function Add-DeclaredTypeReport {
    param($Asm, [string]$TypeName, $Lines)

    $Lines.Add("============================================================")
    $Lines.Add("TYPE DECLARED-ONLY: $TypeName")

    $type = $null
    try { $type = $Asm.GetType($TypeName, $false, $false) } catch { }
    if (-not $type) {
        $Lines.Add("NOT FOUND")
        $Lines.Add("")
        return
    }

    $flags = [System.Reflection.BindingFlags] "Instance,Static,Public,NonPublic,DeclaredOnly"
    $Lines.Add("FullName: $($type.FullName)")
    try { $Lines.Add("BaseType: $($type.BaseType)") } catch { }

    $Lines.Add("")
    $Lines.Add("FIELDS:")
    try {
        foreach ($f in $type.GetFields($flags) | Sort-Object Name) {
            $Lines.Add("  $(Safe-TypeName $f.FieldType) $($f.Name)")
        }
    } catch { $Lines.Add("  [ERROR] $($_.Exception.Message)") }

    $Lines.Add("")
    $Lines.Add("PROPERTIES:")
    try {
        foreach ($p in $type.GetProperties($flags) | Sort-Object Name) {
            $Lines.Add("  $(Safe-TypeName $p.PropertyType) $($p.Name)")
        }
    } catch { $Lines.Add("  [ERROR] $($_.Exception.Message)") }

    $Lines.Add("")
    $Lines.Add("METHODS:")
    try {
        foreach ($m in $type.GetMethods($flags) | Sort-Object Name) {
            $Lines.Add("  $(Format-Method $m)")
        }
    } catch { $Lines.Add("  [ERROR] $($_.Exception.Message)") }

    $Lines.Add("")
}

Push-Location $managed
try {
    $asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $lines = New-Object System.Collections.Generic.List[string]

    $lines.Add("LeezGrowLights V3.1 colour storage/activation probe")
    $lines.Add("Generated: $(Get-Date -Format o)")
    $lines.Add("Assembly: $assemblyPath")
    $lines.Add("")

    foreach ($typeName in @(
        "BlockValue",
        "BlockPoweredLight",
        "BlockPowered",
        "PropChangeInfo",
        "NetPackageSetProp",
        "TileEntityPoweredBlock",
        "TileEntityLight",
        "NetPackageTileEntity",
        "DynamicProperties"
    )) {
        Add-DeclaredTypeReport -Asm $asm -TypeName $typeName -Lines $lines
    }

    $lines.Add("============================================================")
    $lines.Add("VANILLA BLOCK: ceilingLight01_player")

    if (Test-Path $blocksPath) {
        try {
            [xml]$blocksXml = Get-Content -LiteralPath $blocksPath -Raw
            $node = $blocksXml.blocks.block | Where-Object { $_.name -eq "ceilingLight01_player" } | Select-Object -First 1
            if ($node) {
                $lines.Add($node.OuterXml)
            }
            else {
                $lines.Add("NOT FOUND IN $blocksPath")
            }
        }
        catch {
            $lines.Add("XML READ ERROR: $($_.Exception.GetType().Name): $($_.Exception.Message)")
        }
    }
    else {
        $lines.Add("blocks.xml not found at: $blocksPath")
    }

    $lines.Add("")
    $lines.Add("============================================================")
    $lines.Add("PROBE COMPLETED")

    [System.IO.File]::WriteAllLines($OutputPath, $lines)
    Write-Host "Colour storage report written to: $OutputPath" -ForegroundColor Green
}
finally {
    Pop-Location
}
