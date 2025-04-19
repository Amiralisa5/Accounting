param(
    [string]$ArchiveUri,
    [string]$ArchiveUsername,
    [string]$ArchivePassword,
    [switch]$IgnoreGit=$false
)
Import-Module -Name ./publish-global.psm1

#Discover publish version 
$version = Discover-Version

#Check Git commit status
if(-Not $IgnoreGit){
    Check-Git
}

$tempDirectory = Join-Path $pwd  "`$temp"

$applicationName = 'Cloud.ERP.Accounting'

#build
dotnet restore
Check-Command-Result "restore nuget packages"

bigbang migrate
Check-Command-Result "database migrate"

bigbang domain
Check-Command-Result "domain generation"

dotnet publish

#create zip file
$archiveSource = Join-Path $pwd "bin/Release/net8.0/publish/*"
$archiveDestination = Join-Path $tempDirectory ($applicationName +"_"+ $version + ".zip")
if(Test-Path -Path $archiveDestination){
    Remove-Item -Path $archiveDestination -Force
}
Compress-Archive -Path $archiveSource -CompressionLevel Optimal -DestinationPath $archiveDestination

#upload zip file
if(-Not [string]::IsNullOrEmpty($ArchiveUri)){
    $uri = $ArchiveUri
    if(-Not $uri.EndsWith("/")){
        $uri = $uri + "/"
    }

    $uri = $uri + "BigBang.Cloud.ERP.Accounting"+"/"+ $applicationName + "_" + $version + ".zip"
    
    Write-Host "Push zip file to server"+$uri
    if(-Not [string]::IsNullOrEmpty($ArchiveUsername)){
        $User = $ArchiveUsername
        $PWord = ConvertTo-SecureString -String $ArchivePassword -AsPlainText -Force
        $Credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $User, $PWord
        
        $Params = @{
	        Uri = $uri
	        Authentication = "Basic"
	        Credential = $Credential
            Method = "PUT"
            InFile = $archiveDestination
        }
        Invoke-RestMethod @Params
    }else{
        $Params = @{
	        Uri = $uri
            Method = "PUT"
            InFile = $archiveDestination
        }
        Invoke-RestMethod @Params
    }

}
#Create git tag and push it to remote repository
if(-Not $IgnoreGit){
    Create-Tag
    Push-Tag
}