param(
    [string]$ArchiveUri,
    [string]$ArchiveUsername,
    [string]$ArchivePassword,
    [string]$DockerRegistry,
    [string]$NugetSource,
    [string]$NugetApiKey,
    [switch]$IgnoreGit=$false
)
Import-Module -Name ./publish-global.psm1

#Check Git commit status
if(-Not $IgnoreGit){
    Check-Git
}

./publish-artifact.ps1 -ArchiveUri $ArchiveUri -ArchiveUsername $ArchiveUsername -ArchivePassword $ArchivePassword -IgnoreGit
Check-Command-Result "publish artifact"

./publish-docker.ps1 -ArchiveUri $ArchiveUri -ArchiveUsername $ArchiveUsername -ArchivePassword $ArchivePassword -DockerRegistry $DockerRegistry -IgnoreGit
Check-Command-Result "publish docker"

./publish-nuget.ps1 -NugetSource $NugetSource -NugetApiKey $NugetApiKey -IgnoreGit
Check-Command-Result "publish nuget"

#Create git tag and push it to remote repository
if(-Not $IgnoreGit){
    Create-Tag
    Push-Tag
}