param(
    [Parameter(Mandatory=$false)]
    [string]$GamePath = "",

    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

function Add-SafeProperties {
    param($Type, $Lines, $Flags)
    try {
        $props = $Type.GetProperties($Flags) | Sort-Object Name
        foreach ($p in $props) {
            try {
                $propertyTypeName = if ($p.PropertyType) { $p.PropertyType.FullName } else { "<unknown>" }
                $Lines.Add("  $propertyTypeName $($p.Name)")
            }
            catch {
                $Lines.Add("  [PROPERTY ERROR] $($p.Name): $($_.Exception.GetType().Name): $($_.Exception.Message)")
            }
        }
    }
    catch {
        $Lines.Add("  [PROPERTIES ENUMERATION ERROR] $($_.Exception.GetType().Name): $($_.Exception.Message)")
    }
}

function Add-SafeFields {
    param($Type, $Lines, $Flags)
    try {
        $fields = $Type.GetFields($Flags) | Sort-Object Name
        foreach ($f in $fields) {
            try {
                $fieldTypeName = if ($f.FieldType) { $f.FieldType.FullName } else { "<unknown>" }
                $Lines.Add("  $fieldTypeName $($f.Name)")
            }
            catch {
                $Lines.Add("  [FIELD ERROR] $($f.Name): $($_.Exception.GetType().Name): $($_.Exception.Message)")
            }
        }
    }
    catch {
        $Lines.Add("  [FIELDS ENUMERATION ERROR] $($_.Exception.GetType().Name): $($_.Exception.Message)")
    }
}

function Add-SafeMethods {
    param($Type, $Lines, $Flags)
    try {
        $methods = $Type.GetMethods($Flags) | Sort-Object Name
        foreach ($m in $methods) {
            try {
                $returnTypeName = if ($m.ReturnType) { $m.ReturnType.FullName } else { "System.Void" }
                $parameters = @()
                try {
                    $parameters = $m.GetParameters() | ForEach-Object {
                        $parameterTypeName = if ($_.ParameterType) { $_.ParameterType.FullName } else { "<unknown>" }
                        "$parameterTypeName $($_.Name)"
                    }
                }
                catch {
                    # V3.x can expose interface/default-interface members that older PowerShell/.NET
                    # reflection cannot fully materialize. Keep the method in the report and move on.
                    $parameters = @("<parameters unavailable: $($_.Exception.GetType().Name): $($_.Exception.Message)>")
                }
                $Lines.Add("  $returnTypeName $($m.Name)($($parameters -join ', '))")
            }
            catch {
                $methodName = try { $m.Name } catch { "<unknown>" }
                $Lines.Add("  [METHOD ERROR] ${methodName}: $($_.Exception.GetType().Name): $($_.Exception.Message)")
            }
        }
    }
    catch {
        $Lines.Add("  [METHODS ENUMERATION ERROR] $($_.Exception.GetType().Name): $($_.Exception.Message)")
    }
}

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
    $OutputPath = Join-Path $PSScriptRoot "LeezGrowLights_ApiProbe.txt"
}

Push-Location $managed
try {
    $asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $flags = [System.Reflection.BindingFlags] "Instance,Static,Public,NonPublic"

    $typeNames = @(
        "BlockPlantGrowing",
        "BlockPowered",
        "WorldBase",
        "TileEntity",
        "TileEntityPowered",
        "TileEntityPoweredBlock",
        "TileEntityElectricityLightBlock",
        "PowerConsumerToggle"
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("LeezGrowLights V3.1 API probe - resilient reflection build")
    $lines.Add("Generated: $(Get-Date -Format o)")
    $lines.Add("PowerShell: $($PSVersionTable.PSVersion)")
    $lines.Add("CLR: $([System.Environment]::Version)")
    $lines.Add("Assembly: $assemblyPath")
    $lines.Add("")

    foreach ($typeName in $typeNames) {
        $lines.Add("============================================================")
        $lines.Add("TYPE: $typeName")

        try {
            $type = $asm.GetType($typeName, $false, $false)
        }
        catch {
            $type = $null
            $lines.Add("TYPE LOOKUP ERROR: $($_.Exception.GetType().Name): $($_.Exception.Message)")
        }

        if (-not $type) {
            $lines.Add("NOT FOUND")
            $lines.Add("")
            continue
        }

        try { $lines.Add("FullName: $($type.FullName)") } catch { $lines.Add("FullName: <unavailable>") }
        try { $lines.Add("BaseType: $($type.BaseType)") } catch { $lines.Add("BaseType: <unavailable: $($_.Exception.Message)>") }
        try { $lines.Add("IsInterface: $($type.IsInterface)") } catch { }
        $lines.Add("")
        $lines.Add("PROPERTIES:")
        Add-SafeProperties -Type $type -Lines $lines -Flags $flags
        $lines.Add("")
        $lines.Add("FIELDS:")
        Add-SafeFields -Type $type -Lines $lines -Flags $flags
        $lines.Add("")
        $lines.Add("METHODS:")
        Add-SafeMethods -Type $type -Lines $lines -Flags $flags
        $lines.Add("")
    }

    $lines.Add("============================================================")
    $lines.Add("POWER/COMPOSITE TYPES CONTAINING USEFUL NAMES")

    $allTypes = @()
    try {
        $allTypes = $asm.GetTypes()
    }
    catch [System.Reflection.ReflectionTypeLoadException] {
        $allTypes = $_.Exception.Types | Where-Object { $_ -ne $null }
        $lines.Add("  [NOTE] GetTypes partially failed; recovered $($allTypes.Count) loadable types.")
        foreach ($loaderException in $_.Exception.LoaderExceptions | Select-Object -First 25) {
            if ($loaderException) {
                $lines.Add("  [LOADER] $($loaderException.GetType().Name): $($loaderException.Message)")
            }
        }
    }
    catch {
        $lines.Add("  [TYPE ENUMERATION ERROR] $($_.Exception.GetType().Name): $($_.Exception.Message)")
    }

    foreach ($t in $allTypes | Where-Object {
        try {
            $_.Name -match "Power|Electric|Composite|Feature" -and
            $_.Name -match "Tile|Block|Consumer|Feature"
        }
        catch { $false }
    } | Sort-Object FullName | Select-Object -First 250) {
        try { $lines.Add("  $($t.FullName)") } catch { }
    }

    $lines.Add("")
    $lines.Add("============================================================")
    $lines.Add("PROBE COMPLETED")

    [System.IO.File]::WriteAllLines($OutputPath, $lines)
    Write-Host "API report written to: $OutputPath" -ForegroundColor Green
}
finally {
    Pop-Location
}
