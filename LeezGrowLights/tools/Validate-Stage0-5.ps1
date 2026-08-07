param(
    [Parameter(Mandatory=$false)]
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$modRoot = Join-Path $RepoRoot "LeezGrowLights"
$failures = New-Object System.Collections.Generic.List[string]
$passes = New-Object System.Collections.Generic.List[string]

function Pass([string]$message) {
    $script:passes.Add($message)
    Write-Host "PASS  $message" -ForegroundColor Green
}

function Fail([string]$message) {
    $script:failures.Add($message)
    Write-Host "FAIL  $message" -ForegroundColor Red
}

function Assert-True([bool]$condition, [string]$message) {
    if ($condition) { Pass $message } else { Fail $message }
}

function Assert-Equal($actual, $expected, [string]$message) {
    if ($actual -eq $expected) {
        Pass "$message = $expected"
    }
    else {
        Fail "$message (expected '$expected', got '$actual')"
    }
}

function Read-Text([string]$relativePath) {
    $path = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path $path)) {
        Fail "Missing file: $relativePath"
        return ""
    }
    return [System.IO.File]::ReadAllText($path)
}

Write-Host "LeezGrowLights Stage 0-5 static validator" -ForegroundColor Cyan
Write-Host "Repository: $RepoRoot"
Write-Host "This script validates repository invariants only; it does not replace live V3.1.0 b14 tests."
Write-Host ""

# Stage 0: version/build identity.
[xml]$modInfo = Read-Text "LeezGrowLights\ModInfo.xml"
Assert-Equal $modInfo.xml.Version.value "0.5.3.0" "Stage 0 package version"

$initSource = Read-Text "LeezGrowLights\Source\Harmony\Init.cs"
Assert-True ($initSource.Contains("v0.5.3-dev4")) "Stage 0 runtime banner is dev4"

# Stage 1: explicit old-style project compile manifest and hook wiring.
[xml]$project = Read-Text "LeezGrowLights\Source\LeezGrowLights.csproj"
$ns = New-Object System.Xml.XmlNamespaceManager($project.NameTable)
$ns.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")
$compileIncludes = @($project.SelectNodes("//msb:Compile", $ns) | ForEach-Object { $_.Include })

$requiredCompileFiles = @(
    "Harmony\BlockRemovalInstaller.cs",
    "Harmony\BlockRemovalPatches.cs",
    "Harmony\Init.cs",
    "Harmony\PatchInstaller.cs",
    "Harmony\PlantGrowthPatches.cs",
    "Harmony\PowerTransitionPatches.cs",
    "Harmony\SunlightSubstitutionPatches.cs",
    "Runtime\GrowthScheduleContext.cs",
    "Runtime\GrowLightScanner.cs",
    "Runtime\GrowLightTransitionRescheduler.cs",
    "Runtime\PowerStateResolver.cs",
    "Runtime\TickerScheduleAccessor.cs",
    "Runtime\LeezLog.cs"
)

foreach ($file in $requiredCompileFiles) {
    Assert-True ($compileIncludes -contains $file) "Stage 1 project compiles $file"
}

Assert-True ($initSource.Contains("PatchInstaller.Install(harmony)")) "Stage 1 core patch installer is invoked"
Assert-True ($initSource.Contains("BlockRemovalInstaller.Install(harmony)")) "Stage 1 removal patch installer is invoked"

$patchInstaller = Read-Text "LeezGrowLights\Source\Harmony\PatchInstaller.cs"
foreach ($hook in @("set_IsToggled", "HandlePowerReceived", "HandlePowerUpdate")) {
    Assert-True ($patchInstaller.Contains("`"$hook`"")) "Stage 1 electrical hook declared: $hook"
}
Assert-True ($patchInstaller.Contains("typeof(PowerItem)" ) -and $patchInstaller.Contains("HandleDisconnect")) "Stage 1 declaring PowerItem.HandleDisconnect hook is present"

$removalInstaller = Read-Text "LeezGrowLights\Source\Harmony\BlockRemovalInstaller.cs"
Assert-True ($removalInstaller.Contains("BlockPowered") -and $removalInstaller.Contains("OnBlockRemoved")) "Stage 1/5 physical removal hook target is present"

# Stage 2: six tiers, content, power request and progression.
[xml]$blocksXml = Read-Text "LeezGrowLights\Config\blocks.xml"
$blocks = @($blocksXml.configs.append.block)
Assert-Equal $blocks.Count 6 "Stage 2 grow-light block count"

$expectedMultipliers = @{
    "leezGrowLightT1" = "1.2"
    "leezGrowLightT2" = "1.3"
    "leezGrowLightT3" = "1.4"
    "leezGrowLightT4" = "1.5"
    "leezGrowLightT5" = "1.6"
    "leezGrowLightT6" = "4.0"
}

foreach ($tier in 1..6) {
    $name = "leezGrowLightT$tier"
    $block = $blocks | Where-Object { $_.name -eq $name } | Select-Object -First 1
    Assert-True ($null -ne $block) "Stage 2 block exists: $name"
    if ($null -eq $block) { continue }

    $properties = @{}
    foreach ($property in @($block.property)) {
        $properties[$property.name] = $property.value
    }

    Assert-Equal $properties["Extends"] "ceilingLight01_player" "Stage 2 $name electrical base"
    Assert-Equal $properties["RequiredPower"] "10" "Stage 2 $name power request"
    Assert-Equal $properties["LeezGrowTier"] "$tier" "Stage 2 $name tier metadata"
    Assert-Equal $properties["LeezGrowMultiplier"] $expectedMultipliers[$name] "Stage 4 $name multiplier"
    Assert-Equal $properties["LeezGrowRadius"] "2" "Stage 3 $name horizontal radius"
    Assert-Equal $properties["LeezGrowMinFarmBlockVerticalOffset"] "1" "Stage 3 $name minimum vertical offset"
    Assert-Equal $properties["LeezGrowMaxFarmBlockVerticalOffset"] "10" "Stage 3 $name maximum vertical offset"
}

[xml]$recipesXml = Read-Text "LeezGrowLights\Config\recipes.xml"
$recipeNames = @($recipesXml.configs.append.recipe | ForEach-Object { $_.name })
foreach ($tier in 1..6) {
    Assert-True ($recipeNames -contains "leezGrowLightT$tier") "Stage 2 recipe exists: T$tier"
}

[xml]$progressionXml = Read-Text "LeezGrowLights\Config\progression.xml"
$progressionText = $progressionXml.OuterXml
foreach ($tier in 1..6) {
    Assert-True ($progressionText.Contains("leezGrowLightT$tier")) "Stage 2 progression references T$tier"
}

$localizationPath = Join-Path $modRoot "Config\Localization.csv"
$localization = @(Import-Csv $localizationPath)
foreach ($tier in 1..6) {
    foreach ($key in @("leezGrowLightT$tier", "leezGrowLightT${tier}Desc", "leezGrowLightUnlockT$tier")) {
        Assert-True (($localization | Where-Object { $_.Key -eq $key }).Count -eq 1) "Stage 2 localization key exists: $key"
    }
}

# Stages 3-4: scanner geometry and highest-tier-wins rule.
$scanner = Read-Text "LeezGrowLights\Source\Runtime\GrowLightScanner.cs"
Assert-True ($scanner.Contains("DefaultRadius = 2")) "Stage 3 scanner radius is 2"
Assert-True ($scanner.Contains("DefaultMinVerticalOffsetFromFarm = 1")) "Stage 3 scanner minimum vertical offset is 1"
Assert-True ($scanner.Contains("DefaultMaxVerticalOffsetFromFarm = 10")) "Stage 3 scanner maximum vertical offset is 10"
Assert-True ($scanner.Contains("if (multiplier > best)")) "Stage 4 scanner uses highest active multiplier"
Assert-True ($scanner.Contains("HasActiveSunlightReplacement") -and $scanner.Contains("ScanFarmFootprint")) "Stage 3 sunlight replacement delegates to farm-footprint scanning"

# Stage 5: shared transition/equality guard and remaining-work conversion.
$transition = Read-Text "LeezGrowLights\Source\Runtime\GrowLightTransitionRescheduler.cs"
Assert-True ($transition.Contains("Math.Abs(newMultiplier - plant.OldMultiplier) <= 0.0001f")) "Stage 5 unchanged effective multiplier skips reschedule"
Assert-True ($transition.Contains("TickerScheduleAccessor.RescheduleRemainingWork")) "Stage 5 shared transition calls ticker rescheduler"

$ticker = Read-Text "LeezGrowLights\Source\Runtime\TickerScheduleAccessor.cs"
Assert-True ($ticker.Contains("oldRemainingTicks * (double)oldMultiplier")) "Stage 5 converts remaining ticks back to vanilla work"
Assert-True ($ticker.Contains("remainingVanillaWork / newMultiplier")) "Stage 5 applies the new multiplier to remaining work"

$removalPatch = Read-Text "LeezGrowLights\Source\Harmony\BlockRemovalPatches.cs"
Assert-True ($removalPatch.Contains("GrowLightTransitionRescheduler.Capture")) "Stage 5 removal captures old effective state"
Assert-True ($removalPatch.Contains("GrowLightTransitionRescheduler.Apply")) "Stage 5 removal applies new effective state"

Write-Host ""
Write-Host ("Static checks passed: {0}" -f $passes.Count) -ForegroundColor Cyan
Write-Host ("Static checks failed: {0}" -f $failures.Count) -ForegroundColor Cyan

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Failures:" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host " - $failure" -ForegroundColor Red
    }
    exit 1
}

Write-Host "All Stage 0-5 static repository checks passed. Live b14 gameplay tests are still required for runtime acceptance." -ForegroundColor Green
exit 0
