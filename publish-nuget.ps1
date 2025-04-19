param(
    [string]$NugetSource,
    [string]$NugetApiKey,
    [switch]$IgnoreGit=$false
)
Import-Module -Name ./publish-global.psm1

#Discover publish version 
$version = Discover-Version

#Check Git commit status
if(-Not $IgnoreGit){
    Check-Git
}


#build
dotnet restore
Check-Command-Result "restore nuget packages"
bigbang migrate
Check-Command-Result "database migrate"
bigbang domain
Check-Command-Result "domain generation"

#pack
    #top level package is a package with the name of BigBang.Cloud.ERP.Accounting and is dependent to all packable solution Project and host dependent packages
$projectName = "BigBang.Cloud.ERP.Accounting"
$tempDirectory = Join-Path $pwd  "`$temp"
$nugetDirectory = Join-Path $tempDirectory  ".nuget"
function createProjectFile {
    param(
        $projectFileName
    )
    $projectData = @'
    <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFramework>net8.0</TargetFramework>
      <NoBuild>true</NoBuild>
      <IncludeBuildOutput>false</IncludeBuildOutput>
      <IncludeSource>false</IncludeSource>
      <IncludeSymbols>false</IncludeSymbols>
      <NoDefaultExcludes>true</NoDefaultExcludes>
      <NuspecFile>$(MSBuildThisFileDirectory)$(MSBuildProjectName).nuspec</NuspecFile>
      <NuspecBasePath>./</NuspecBasePath>
    </PropertyGroup>
  </Project>
'@
    $xcsProject =[System.Xml.Linq.XDocument]::Parse($projectData)
    $xcsProject.Save($projectFileName)
}
function getHostTopLevelPackages(){
   $json = dotnet list .\src\Host\ package --format json | ConvertFrom-Json
   return $json.projects[0].frameworks[0].topLevelPackages
}
function createNuspecFile {
    param (
        $nuspecFileName,
        $nupackagesDirectory
    )
    $nuspecHead = @'
    <package>
    <metadata>
      <id>BigBang.Cloud.ERP.Accounting</id>
      <title>BigBang Cloud.ERP.Accounting</title>
      <authors>BigBang Team</authors>
      <owners>BigBang Team</owners>
      <description>Cloud.ERP.Accounting</description>
      <tags>BigBang Cloud.ERP.Accounting</tags>
      <version>
'@
$nuspecHead = $nuspecHead + $version
$nuspecHead = $nuspecHead+ @'
</version>
      <copyright>Copyright ©2023</copyright>
      <dependencies>
'@

        $nuspecTail = @'
        </dependencies>
        </metadata>
        <files>
        <dependency src="*.csproj" exclude="*.csproj" />
        </files>
        </package>
'@
    $dependecies = ''
    $excludedPackages = [System.Collections.Generic.HashSet[String]] @()
    $filter = Join-Path $nupackagesDirectory '*.nupkg'
    $nupackageFiles = Get-ChildItem -Path $filter -Name
    foreach ($nupackageFile in $nupackageFiles) {
        $packageId = [System.IO.Path]::GetFileNameWithoutExtension($nupackageFile)
        $packageId = $packageId.Replace(".${version}",'')
        if(-Not $excludedPackages.Contains($packageId)){
            $dependecies += '<dependency id="'+$packageId+'" version="'+$version+'"/>'
        }
    }
    $hostTopLevelPackages = getHostTopLevelPackages
    foreach ($hostTopLevelPackage in $hostTopLevelPackages) {
        $packageId = $hostTopLevelPackage.id
        if(-Not $excludedPackages.Contains($packageId)){
            $requestedVersion = $hostTopLevelPackage.requestedVersion
            $dependecies += '<dependency id="'+$packageId+'" version="'+$requestedVersion+'"/>'
        }
    }

    $xnuspec =[System.Xml.Linq.XDocument]::Parse($nuspecHead+$dependecies+$nuspecTail)
    $xnuspec.Save($nuspecFileName)
}
function createTopLevelPackage {

    $directory = Join-Path $nugetDirectory ".topLevelPackage"
    $projectFileName = Join-Path $directory "${projectName}.csproj"
    $nuspecFileName = Join-Path $directory "${projectName}.nuspec"
    if(Test-Path -Path $directory){
        Remove-Item -Path $directory -Force -Recurse
    }
    New-Item -Path $directory -ItemType Directory

    createNuspecFile -nuspecFileName $nuspecFileName -nupackagesDirectory $nugetDirectory
    createProjectFile -projectFileName $projectFileName
    dotnet pack $projectFileName /p:Version=$version --output $nugetDirectory
}
function createPackages {
    if(Test-Path -Path $nugetDirectory){
        Remove-Item -Path $nugetDirectory -Force -Recurse
    }
    New-Item -Path $nugetDirectory -ItemType Directory
    
    dotnet pack /p:Version=$version --configuration Release
    Check-Command-Result 'packing nuget packages' 
    createTopLevelPackage    
}
function pushPackage {
    $filter = Join-Path $nugetDirectory '*.nupkg'
    $nupackageFiles = Get-ChildItem -Path $filter -Name
    foreach ($nupackageFile in $nupackageFiles) {
        $nugetPackageFullPath = Join-Path $nugetDirectory $nupackageFile
        $packageId = [System.IO.Path]::GetFileNameWithoutExtension($nupackageFile)
        $packageId = $packageId.Replace(".${version}",'')
        if([string]::IsNullOrEmpty($NugetApiKey)){
            dotnet nuget push $nugetPackageFullPath --source $NugetSource
        }else{
            dotnet nuget push $nugetPackageFullPath --source $NugetSource --api-key $NugetApiKey
        }

        Check-Command-Result "push package with id ${packageId} and version ${version}"
    }
}
createPackages

#push package
if(-Not [string]::IsNullOrEmpty($NugetSource)){
    pushPackage
}

#Create git tag and push it to remote repository
if(-Not $IgnoreGit){
    Create-Tag
    Push-Tag
}
