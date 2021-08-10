$rimworldSteamDir = "C:\Rimworld"
if(Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 294100") {
    $rimworldSteamDir = Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 294100" -Name InstallLocation
}
if (!($rimworldDir = Read-Host "Enter RimWorld directory [$rimworldSteamDir]")) { $rimworldDir = $rimworldSteamDir }
$rimworldDataDir = Join-Path -Path $rimworldDir -ChildPath "RimWorldWin64_Data"
$rimworldManagedDir = Join-Path -Path $rimworldDataDir -ChildPath "Managed"
$assemblyPath = Join-Path -Path $rimworldManagedDir -ChildPath "Assembly-CSharp.dll"

if((Test-Path $assemblyPath) -eq $false) {
    Write-Error ("Assembly file not found in: " + $assemblyPath)
    exit
}

Write-Host ("Assembly file found at: " + $assemblyPath)
$devFolder = "C:\Dev"
$rimworldFolder = Join-Path -Path $devFolder -ChildPath "RimWorld"
$rimworldTempFolder = Join-Path -Path $rimworldFolder -ChildPath "Temp"
if((Test-Path -Path $rimworldTempFolder) -eq $false) {
    New-Item -Path $rimworldTempFolder -ItemType Directory
}
dotnet tool install --global ilspycmd --version 7.1.0.6543
#Start-Process "ilspycmd" -ArgumentList @($assemblyPath, "-o", $rimworldTempFolder, "-p")
#Start-Process "ilspycmd" -ArgumentList @($assemblyPath, "-o", $rimworldTempFolder, "-genpdb") -Wait

$pdbSourceFile = Join-Path -Path $rimworldTempFolder -ChildPath "Assembly-CSharp.pdb"
$pdbDestinationFile = Join-Path -Path $rimworldManagedDir -ChildPath "Assembly-CSharp.pdb"
Copy-Item -Path $pdbSourceFile -Destination $pdbDestinationFile

$productVersion = Get-ChildItem -Path $assemblyPath | Select-Object -ExpandProperty VersionInfo | Select-Object -ExpandProperty ProductVersion
$majorMinorSplit = $productVersion -split "\."
$majorMinorVersion = $majorMinorSplit[0..1] -join "."

Write-Host ("Detected Version: " + $majorMinorVersion)
$dependenciesDir = Convert-Path -Path ".\Dependencies"
$dependencyVersionDir = Join-Path $dependenciesDir -ChildPath $majorMinorVersion
Copy-Item -Path $assemblyPath -Destination $dependencyVersionDir
