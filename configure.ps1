$rimworldSourceFolder = "C:\RWSource"
$rimworldSearchDir = "C:\Rimworld"

if(Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 294100") {
    $rimworldSearchDir = Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 294100" -Name InstallLocation
}
if (!($rimworldDir = Read-Host "Enter RimWorld directory [$rimworldSearchDir]")) { $rimworldDir = $rimworldSearchDir }
$rimworldDataDir = Join-Path -Path $rimworldDir -ChildPath "RimWorldWin64_Data"
$rimworldManagedDir = Join-Path -Path $rimworldDataDir -ChildPath "Managed"
$assemblyPath = Join-Path -Path $rimworldManagedDir -ChildPath "Assembly-CSharp.dll"

if((Test-Path $assemblyPath) -eq $false) {
    Write-Error ("Assembly file not found in: " + $assemblyPath)
    exit
}
Write-Host ("Assembly file found at: " + $assemblyPath)

$productVersion = Get-ChildItem -Path $assemblyPath | Select-Object -ExpandProperty VersionInfo | Select-Object -ExpandProperty ProductVersion
$majorMinorSplit = $productVersion -split "\."
$majorMinorVersion = $majorMinorSplit[0..1] -join "."
Write-Host ("Detected Version: " + $majorMinorVersion)

$rimworldSourceVersionFolder = Join-Path -Path $rimworldSourceFolder -ChildPath $majorMinorVersion
if((Test-Path -Path $rimworldSourceVersionFolder) -eq $false) {
    $null = New-Item -Path $rimworldSourceVersionFolder -ItemType Directory
}

$dependencyVersionDir = Join-Path ".\Dependencies" -ChildPath $majorMinorVersion
$null = New-Item -Path $dependencyVersionDir -ItemType Directory
Write-Host ("Copying: " + $assemblyPath + " to: " + $dependencyVersionDir)
Copy-Item -Path $assemblyPath -Destination $dependencyVersionDir

$ICSharpCodeDecompilerZip = Join-Path -Path $rimworldSourceFolder -ChildPath "ICSharpCode.Decompiler.zip"
Invoke-WebRequest -Uri https://github.com/cseelhoff/ILSpy/releases/download/7.1b/ICSharpCode.Decompiler.zip -OutFile $ICSharpCodeDecompilerZip
Expand-Archive -Path $ICSharpCodeDecompilerZip -DestinationPath $rimworldSourceFolder -Force
$null = [Reflection.Assembly]::LoadFile((Join-Path -Path $rimworldSourceFolder -ChildPath "System.Reflection.Metadata.dll"))
$null = [Reflection.Assembly]::LoadFile((Join-Path -Path $rimworldSourceFolder -ChildPath "System.Collections.Immutable.dll"))
$null = [Reflection.Assembly]::LoadFile((Join-Path -Path $rimworldSourceFolder -ChildPath "ICSharpCode.Decompiler.dll"))
$pdbDestinationFile = Join-Path -Path $rimworldManagedDir -ChildPath "Assembly-CSharp.pdb"
Write-Host ("Decompiling source to: " + $rimworldSourceVersionFolder)
Write-Host ("Writing PDB to: " + $pdbDestinationFile)
Write-Host ("This process could take several minutes. Please wait...")
[ICSharpCode.Decompiler.DebugInfo.PortablePdbWriter]::WritePdb($assemblyPath, $pdbDestinationFile, $rimworldSourceVersionFolder)

