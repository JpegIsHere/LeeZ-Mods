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

if (-not (Test-Path $assemblyPath)) {
    throw "Assembly-CSharp.dll not found at: $assemblyPath`nPass -GamePath 'C:\path\to\7 Days To Die'."
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot "LeezGrowLights_ColourApiProbe.txt"
}

function Format-Method {
    param($Method)
    try {
        $parameters = @($Method.GetParameters() | ForEach-Object {
            $typeName = if ($_.ParameterType) { $_.ParameterType.FullName } else { "<unknown>" }
            "$typeName $($_.Name)"
        })
        $returnName = if ($Method.ReturnType) { $Method.ReturnType.FullName } else { "System.Void" }
        return "$returnName $($Method.Name)($($parameters -join ', '))"
    }
    catch {
        return "[METHOD ERROR] $($Method.Name): $($_.Exception.GetType().Name): $($_.Exception.Message)"
    }
}

function Add-TypeReport {
    param($Asm, [string]$TypeName, $Lines, $Flags)

    $Lines.Add("============================================================")
    $Lines.Add("TYPE: $TypeName")

    $type = $null
    try { $type = $Asm.GetType($TypeName, $false, $false) } catch { }
    if (-not $type) {
        $Lines.Add("NOT FOUND")
        $Lines.Add("")
        return
    }

    try { $Lines.Add("FullName: $($type.FullName)") } catch { }
    try { $Lines.Add("BaseType: $($type.BaseType)") } catch { }

    $memberPattern = "Activ|Command|Text|Light|Color|Colour|Write|Read|Save|Load|SetBlock|RPC|TileEntity|Prefab|Transform|GameObject|Custom|Data|Sync|Net|Value|Window|Open|Close|Apply|Set|Get"

    $Lines.Add("")
    $Lines.Add("MATCHING PROPERTIES:")
    try {
        foreach ($p in $type.GetProperties($Flags) | Where-Object { $_.Name -match $memberPattern } | Sort-Object Name) {
            try { $Lines.Add("  $($p.PropertyType.FullName) $($p.Name)") } catch { }
        }
    }
    catch { $Lines.Add("  [ERROR] $($_.Exception.Message)") }

    $Lines.Add("")
    $Lines.Add("MATCHING FIELDS:")
    try {
        foreach ($f in $type.GetFields($Flags) | Where-Object { $_.Name -match $memberPattern } | Sort-Object Name) {
            try { $Lines.Add("  $($f.FieldType.FullName) $($f.Name)") } catch { }
        }
    }
    catch { $Lines.Add("  [ERROR] $($_.Exception.Message)") }

    $Lines.Add("")
    $Lines.Add("MATCHING METHODS:")
    try {
        foreach ($m in $type.GetMethods($Flags) | Where-Object { $_.Name -match $memberPattern } | Sort-Object Name) {
            $Lines.Add("  $(Format-Method $m)")
        }
    }
    catch { $Lines.Add("  [ERROR] $($_.Exception.Message)") }

    $Lines.Add("")
}

Push-Location $managed
try {
    $asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $flags = [System.Reflection.BindingFlags] "Instance,Static,Public,NonPublic"
    $lines = New-Object System.Collections.Generic.List[string]

    $lines.Add("LeezGrowLights V3.1 colour-system API probe")
    $lines.Add("Generated: $(Get-Date -Format o)")
    $lines.Add("Assembly: $assemblyPath")
    $lines.Add("")

    $typeNames = @(
        "Block",
        "BlockPowered",
        "BlockPoweredLight",
        "BlockLight",
        "BlockEntityData",
        "GameManager",
        "TileEntity",
        "TileEntityPowered",
        "TileEntityPoweredBlock",
        "TileEntityLight",
        "PowerItem",
        "PowerConsumerToggle",
        "BlockActivationCommand",
        "LightManager",
        "LightManager+NetPackageLight",
        "LightState",
        "LightStateType",
        "UpdateLight",
        "XUiC_LightEditor",
        "XUiC_LightEditor+LightValues",
        "XUiC_PoweredGenericWindowGroup",
        "XUiC_PoweredSpotlightWindowGroup",
        "NetPackageTileEntity",
        "ConnectionManager"
    )

    foreach ($typeName in $typeNames) {
        Add-TypeReport -Asm $asm -TypeName $typeName -Lines $lines -Flags $flags
    }

    $lines.Add("============================================================")
    $lines.Add("DISCOVERED TYPES WITH COLOUR/LIGHT/ACTIVATION/NETWORK NAMES")

    $allTypes = @()
    try {
        $allTypes = $asm.GetTypes()
    }
    catch [System.Reflection.ReflectionTypeLoadException] {
        $allTypes = $_.Exception.Types | Where-Object { $_ -ne $null }
        $lines.Add("  [NOTE] Partial type load; recovered $($allTypes.Count) types.")
    }

    foreach ($t in $allTypes | Where-Object {
        try { $_.Name -match "Color|Colour|Light|Activ|NetPackage|Powered|Electric" } catch { $false }
    } | Sort-Object FullName | Select-Object -First 400) {
        try { $lines.Add("  $($t.FullName)") } catch { }
    }

    $lines.Add("")
    $lines.Add("============================================================")
    $lines.Add("PROBE COMPLETED")

    [System.IO.File]::WriteAllLines($OutputPath, $lines)
    Write-Host "Colour API report written to: $OutputPath" -ForegroundColor Green
}
finally {
    Pop-Location
}
