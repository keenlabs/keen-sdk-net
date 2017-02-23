<#
NewSemVer.ps1

This utility helps to update the .NET assembly versions, rebuild the solution and create a new
NuGet package with that same version.
#>

Param(
    [parameter()]
    [alias('v')]
    $version,
     
    [parameter()]
    [alias('h', '?')]
    [switch]
    $help
    )


$semVerPattern = '\d+.\d+.\d+(-[a-zA-Z0-9-]+)?'


<#
Print usage info.
#>
function Usage {
	"Updates the AssemblyVersion, AssemblyInformationalVersion and AssemblyFileVersion in "
	"the SharedVersionInfo.cs file. Then, tries to create a new NuGet package, which will fail if "
    "nuget.exe isn't in the PATH or the script's directory.`n"
    ".\NewSemVer.ps1 <VersionNumber>`n"
	"   <VersionNumber>     The version number to set, for example: 1.2.3"
    "                       If prelease/metadata info (e.g. 1.2.3-beta) is provided, only "
    "                        AssemblyInformationalVersion will include the extra info.`n"
}


function Get-MajMinPatchVersion([string] $version) {
    return $version.Split('-'.ToCharArray(), 2)[0]
}
 
<#
Effect version updates in SharedVersionInfo.cs.
#>
function Update-AssemblyVersionAttributes ([string] $version) {
    $majMinPatchPattern = '[0-9]+(\.([0-9]+|\*)){1,3}'
	$assemblyVersionPattern = "AssemblyVersion\(`"$majMinPatchPattern`"\)"
	$assemblyFileVersionPattern = "AssemblyFileVersion\(`"$majMinPatchPattern`"\)"
    $assemblyInformationalVersionPattern = "AssemblyInformationalVersion\(`"$semVerPattern`"\)"

    $majMinPatchVersion = Get-MajMinPatchVersion($version)
	$newAssemblyVersion = "AssemblyVersion(`"$majMinPatchVersion`")";
	$newAssemblyfileVersion = "AssemblyFileVersion(`"$majMinPatchVersion`")";
    $newAssemblyInformationalVersion = "AssemblyInformationalVersion(`"$version`")";
	
	Get-ChildItem -r -filter SharedVersionInfo.cs | ForEach-Object {
		$filename = $_.Directory.ToString() + [IO.Path]::DirectorySeparatorChar + $_.Name
		"Setting version to $version in $filename"
	
		(Get-Content $filename) | ForEach-Object {
			% {$_ -replace $assemblyVersionPattern, $newAssemblyVersion } |
			% {$_ -replace $assemblyFileVersionPattern, $newAssemblyfileVersion } |
            % {$_ -replace $assemblyInformationalVersionPattern, $newAssemblyInformationalVersion }
		} | Set-Content $filename
	}
} 


# Handle args.
if ($help -or ($version -notmatch "^$semVerPattern$")) {
    Usage
	return;
}


# Update the version for the .NET assemblies.
Update-AssemblyVersionAttributes $version


# Rebuild the solution to bake the new version into the assemblies. This is using the default
# VS2015 location for MSBuild, so change it as appropriate.
$msBuildExe = (${env:ProgramFiles(x86)}, 'MSBuild', '14.0', 'bin', 'msbuild.exe') `
    -join [IO.Path]::DirectorySeparatorChar
'MSBuild EXE: ' + $msBuildExe

& $msBuildExe ('Keen.sln','/verbosity:q','/p:configuration=Release','/t:Clean,Build')

if (-not $?) {
    Write-Debug 'Failed to build solution!'
    exit
}


# Execute the nuget CLI either from the script's location or the PATH.
$scriptPath = Split-Path -Path $MyInvocation.MyCommand.Path
$env:Path += ";$scriptPath"


# Create the .nupkg and pass the version to override the .nuspec token(s).
& 'nuget.exe' pack KeenClient.nuspec -properties "version=$version"
