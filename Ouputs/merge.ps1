#cat ..\OceanOfCode\*.cs ..\OceanOfCode\*\*.cs | sc merge.cs

Write-Host 'start merging'

$fromPath = "C:\Users\Q z L\Documents\Visual Studio 2019\Projects\CodeInGame\OceanOfCode\OceanOfCode\*.cs"
$toFile = "C:\Users\Q z L\Documents\Visual Studio 2019\Projects\CodeInGame\OceanOfCode\Ouputs\merge.cs"

Get-ChildItem -Recurse $fromPath | Where {$_.Name -notlike "*.AssemblyInfo.cs" } | ForEach-Object { Get-Content $_ } | Out-File $toFile


Write-Host 'done merging'


# Get-ChildItem -Recurse "C:\Users\Q z L\Documents\Visual Studio 2019\Projects\CodeInGame\OceanOfCode\OceanOfCode\*.cs"