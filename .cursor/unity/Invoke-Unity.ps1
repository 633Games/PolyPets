<#
.SYNOPSIS
  Send a compact command to the live Unity editor via the PolyPets file bridge.

.DESCRIPTION
  Writes .cursor/unity/cmd.json and waits for matching out.json.
  Requires Unity Editor open with the project (CursorUnityBridge auto-loads).

.EXAMPLE
  .\Invoke-Unity.ps1 ping
  .\Invoke-Unity.ps1 hierarchy -Depth 2 -Limit 40
  .\Invoke-Unity.ps1 find -Name Care -Type CareHudController
  .\Invoke-Unity.ps1 log -Limit 30 -Filter CareHud
  .\Invoke-Unity.ps1 exec -Menu "PolyPets/UI/Apply Cozy Companion HUD"
  .\Invoke-Unity.ps1 get-component -Name CareHud -Type CareHudController
  .\Invoke-Unity.ps1 screenshot -View game
  .\Invoke-Unity.ps1 screenshot -View scene -Path house_scene
  .\Invoke-Unity.ps1 click -X 240 -Y 360
  .\Invoke-Unity.ps1 click -X 0.5 -Y 0.5 -Normalized
  .\Invoke-Unity.ps1 focus -View game
  .\Invoke-Unity.ps1 select -Name CareHud
  .\Invoke-Unity.ps1 gameview
  .\Invoke-Unity.ps1 menus
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0, Mandatory = $true)]
    [ValidateSet(
        "ping", "status", "hierarchy", "find", "log", "exec",
        "get-component", "component", "menus",
        "screenshot", "capture", "click", "tap", "press", "invoke", "focus", "select", "gameview", "game-view",
        "play", "set-gameview", "gameview-set", "rebuild-hud", "rebuildhud",
        "refresh", "reimport"
    )]
    [string]$Command,

    [string]$Name = "",
    [string]$Query = "",
    [string]$Type = "",
    [string]$Component = "",
    [string]$Filter = "",
    [string]$Menu = "",
    [ValidateSet("", "game", "scene", "both", "ui", "world", "auto", "window", "camera", "desktop")]
    [string]$View = "",
    [string]$Path = "",
    [double]$X = 0,
    [double]$Y = 0,
    [switch]$Normalized,
    [ValidateSet("", "topleft", "bottomleft")]
    [string]$Origin = "",
    [int]$Depth = 0,
    [int]$Limit = 0,
    [int]$TimeoutSec = 20,
    [switch]$Raw
)

$ErrorActionPreference = "Stop"

$BridgeDir = $PSScriptRoot
$CmdPath = Join-Path $BridgeDir "cmd.json"
$OutPath = Join-Path $BridgeDir "out.json"
$StatusPath = Join-Path $BridgeDir "status.json"

function Write-BridgeError([string]$Message, [int]$Code = 1) {
    Write-Host $Message -ForegroundColor Red
    exit $Code
}

if (-not (Test-Path $StatusPath)) {
    Write-BridgeError @"
Unity bridge status not found: $StatusPath

Open this project in Unity Editor (scripts compile → CursorUnityBridge starts).
Then retry. Menu: PolyPets → Cursor Bridge → Ping (write status)
"@
}

try {
    $status = Get-Content -Raw -Path $StatusPath | ConvertFrom-Json
    if ($status.state -eq "quitting") {
        Write-BridgeError "Unity bridge reports state=quitting. Re-open the Unity Editor on this project, then retry."
    }
    $age = [DateTime]::UtcNow - [DateTime]::Parse($status.t).ToUniversalTime()
    if ($age.TotalSeconds -gt 45) {
        Write-Host "Warning: status.json is $([int]$age.TotalSeconds)s old (state=$($status.state)). Unity may be closed or stuck." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Warning: could not parse status.json" -ForegroundColor Yellow
}

$id = [guid]::NewGuid().ToString("N").Substring(0, 12)
$cmdName = $Command
switch ($cmdName) {
    "status" { $cmdName = "ping" }
    "component" { $cmdName = "get-component" }
    "capture" { $cmdName = "screenshot" }
    "tap" { $cmdName = "click" }
    "invoke" { $cmdName = "press" }
    "game-view" { $cmdName = "gameview" }
    "gameview-set" { $cmdName = "set-gameview" }
    "rebuildhud" { $cmdName = "rebuild-hud" }
    "reimport" { $cmdName = "refresh" }
}

# Screenshots (esp. play-mode Game View) may need an extra frame
if ($cmdName -eq "screenshot" -and $TimeoutSec -lt 45) {
    $TimeoutSec = 45
}

$filterValue = $Filter
if ($Origin -eq "topleft" -and -not $filterValue) {
    $filterValue = "topleft"
}

$viewValue = $View
if (-not $viewValue -and $cmdName -eq "screenshot") {
    $viewValue = "game"
}
if (-not $viewValue -and $cmdName -eq "focus") {
    $viewValue = "game"
}

# Flat envelope for Unity JsonUtility
$payload = [ordered]@{
    id         = $id
    cmd        = $cmdName
    name       = $(if ($Name) { $Name } elseif ($Query) { $Query } else { "" })
    query      = $Query
    type       = $(if ($Type) { $Type } elseif ($Component) { $Component } else { "" })
    component  = $Component
    filter     = $filterValue
    menu       = $Menu
    view       = $viewValue
    path       = $Path
    depth      = $Depth
    limit      = $Limit
    x          = [float]$X
    y          = [float]$Y
    normalized = $(if ($Normalized) { 1 } else { 0 })
}

$json = ($payload | ConvertTo-Json -Compress)

# Clear stale out if it matches nothing useful
if (Test-Path $OutPath) {
    Remove-Item -Force $OutPath -ErrorAction SilentlyContinue
}
if (Test-Path $CmdPath) {
    Remove-Item -Force $CmdPath -ErrorAction SilentlyContinue
}

[System.IO.File]::WriteAllText($CmdPath, $json, [System.Text.UTF8Encoding]::new($false))

$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSec)
$outText = $null
while ([DateTime]::UtcNow -lt $deadline) {
    Start-Sleep -Milliseconds 200
    if (-not (Test-Path $OutPath)) { continue }
    try {
        $outText = [System.IO.File]::ReadAllText($OutPath)
        $parsed = $outText | ConvertFrom-Json
        if ($parsed.id -eq $id) { break }
        $outText = $null
    }
    catch {
        $outText = $null
    }
}

if (-not $outText) {
    Write-BridgeError "Timed out after ${TimeoutSec}s waiting for Unity. Is the editor open and not stuck compiling?"
}

if ($Raw) {
    Write-Output $outText
    exit 0
}

$parsed = $outText | ConvertFrom-Json
if (-not $parsed.ok) {
    Write-Host "FAIL [$($parsed.cmd)] $($parsed.error)" -ForegroundColor Red
    if ($parsed.data) {
        $parsed.data | ConvertTo-Json -Depth 6 -Compress | Write-Host
    }
    exit 2
}

# Compact pretty print of data (token-friendly)
Write-Host "OK $($parsed.cmd) ($($parsed.ms)ms)" -ForegroundColor Green
if ($null -ne $parsed.data) {
    $parsed.data | ConvertTo-Json -Depth 8 | Write-Output
}
else {
    Write-Output $outText
}
