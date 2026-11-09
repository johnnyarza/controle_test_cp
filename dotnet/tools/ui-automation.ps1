# UI Automation helpers for checking ProbetaControl.exe screens (used for the screenshot check of each
# phase-4 group). Dot-source it:  . .\dotnet\tools\ui-automation.ps1
#
# Typical use: copy the built exe (+ a ProbetaControl.config for a test database) to a scratch folder,
# Start-Process it, then Get-AppWindow / Wait-Text / Set-Edit / Click / Select-Item / Snap.
# Keep one check inside one PowerShell command with try/finally Stop-Process: an app left running
# between commands may be closed by the host.
# Notes: text inside control templates is not visible to UI Automation (wait on rows instead);
# dialogs are children of their owner window (Wait-Window $owner "Title").
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class W32 {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
}
"@
[W32]::SetProcessDPIAware() | Out-Null
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]

function Get-AppWindow([int]$ProcessId, [int]$TimeoutSec = 20) {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $cond = New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $ProcessId)
        $w = $AE::RootElement.FindFirst($TS::Children, $cond)
        if ($w) { return $w }
        Start-Sleep -Milliseconds 250
    }
    throw "window not found"
}

function Wait-Text($win, [string]$text, [int]$TimeoutSec = 20) {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $cond = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $text)
        $e = $win.FindFirst($TS::Descendants, $cond)
        if ($e) { return $e }
        Start-Sleep -Milliseconds 250
    }
    throw "text '$text' not found"
}

function Get-Edits($win) {
    $cond = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
    return @($win.FindAll($TS::Descendants, $cond))
}

function Set-Edit($element, [string]$value) {
    $p = $element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $p.SetValue($value)
}

function Click($win, [string]$name) {
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)))
    $deadline = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $deadline) {
        $b = $win.FindFirst($TS::Descendants, $cond)
        if ($b -and $b.Current.IsEnabled) {
            $b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
            return
        }
        Start-Sleep -Milliseconds 250
    }
    throw "button '$name' not found or disabled"
}

function Snap($win, [string]$path) {
    [W32]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 400
    $r = $win.Current.BoundingRectangle
    $bmp = New-Object System.Drawing.Bitmap ([int]$r.Width), ([int]$r.Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    # PW_RENDERFULLCONTENT (2): captures the window's own pixels even when covered.
    [W32]::PrintWindow([IntPtr]$win.Current.NativeWindowHandle, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}

function Resize-Window($win, [double]$width, [double]$height) {
    $wp = $win.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
    if ($wp.Current.WindowVisualState -ne [System.Windows.Automation.WindowVisualState]::Normal) {
        $wp.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Normal)
        Start-Sleep -Milliseconds 400
    }
    $tp = $win.GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern)
    $tp.Move(40, 40)
    $tp.Resize($width, $height)
    Start-Sleep -Milliseconds 600
}

function Select-Item($win, [string]$name) {
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)))
    $item = $win.FindFirst($TS::Descendants, $cond)
    if (-not $item) { throw "list item '$name' not found" }
    $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
}

function Get-DataRows($win) {
    $cond = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::DataItem)
    return @($win.FindAll($TS::Descendants, $cond))
}

# Dialogs are owned windows: UIA lists them as children of their owner window.
function Wait-Window($owner, [string]$title, [int]$TimeoutSec = 15) {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $title)),
        (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::Window)))
    while ((Get-Date) -lt $deadline) {
        $w = $owner.FindFirst($TS::Children, $cond)
        if ($w) { return $w }
        Start-Sleep -Milliseconds 250
    }
    throw "window '$title' not found"
}

function Read-TestEnv {
    $values = @{}
    Get-Content (Join-Path $PSScriptRoot "..\tests\test.env") |
        Where-Object { $_ -match '^\s*[A-Z_]+=' } |
        ForEach-Object { $k, $v = $_ -split '=', 2; $values[$k.Trim()] = $v.Trim() }
    return $values
}
