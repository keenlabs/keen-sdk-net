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
    "Updates the Version in the .csproj file. Then, tries to create a new NuGet package, which will"
    "fail if dotnet core tools aren't installed.`n"
    ".\NewSemVer.ps1 <VersionNumber>`n"
    "   <VersionNumber>     The version number to set, for example: 1.2.3"
    "                       If prelease/metadata info (e.g. 1.2.3-beta) is provided, only "
    "                        AssemblyInformationalVersion will include the extra info.`n"
}


function Get-MajMinPatchVersion([string] $version) {
    return $version.Split('-'.ToCharArray(), 2)[0]
}


<#
Effect version updates in .csproj file.
#>
function Update-AssemblyVersionAttributes ([string] $version) {
    $majMinPatchPattern = '[0-9]+(\.([0-9]+|\*)){1,3}'
    $netStandardVersionPattern = "<Version>$majMinPatchPattern</Version>"

    $majMinPatchVersion = Get-MajMinPatchVersion($version)
    $newNetStandardVersion = "<Version>$majMinPatchVersion</Version>"
 
    Get-ChildItem -r -filter .\* -Include Keen.csproj | ForEach-Object {
        $filename = $_.Directory.ToString() + [IO.Path]::DirectorySeparatorChar + $_.Name
        "Setting version to $version in $filename"

        (Get-Content $filename) | ForEach-Object {
            % {$_ -replace $netStandardVersionPattern, $newNetStandardVersion }
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


# Create another .nupkg for the .NET Standard stuff based on the .csproj, which will be the only .nupkg going forward
& pushd .\Keen
& dotnet clean -c Release
& dotnet pack -c Release
& popd
