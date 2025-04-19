param(
    [string]$DockerRegistry,
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

$applicationName = 'cloud.erp.accounting'
$applicationName = $applicationName.ToLower()
$imageName = "bigbang/"+$applicationName+':'+$version
$latestImageName = "bigbang/"+$applicationName+':latest'
docker build --label version=$version -t $imageName -f ./.docker/Dockerfile ./
Check-Command-Result 'docker build'
docker tag $imageName $latestImageName
Check-Command-Result 'docker tag'

if(-Not [string]::IsNullOrEmpty($DockerRegistry)){
    $registryImageName = $DockerRegistry+'/'+$imageName
    $registryLatestImageName = $DockerRegistry+'/'+$latestImageName
    docker tag $imageName $registryImageName
    Check-Command-Result 'docker tag'
    docker tag $latestImageName $registryLatestImageName
    Check-Command-Result 'docker tag'
    docker push $registryImageName
    Check-Command-Result 'docker push'
    docker push $registryLatestImageName
    Check-Command-Result 'docker push'

}
if(-Not [string]::IsNullOrEmpty($ArchiveUri)){
    $uri = $ArchiveUri
    if(-Not $uri.EndsWith("/")){
        $uri = $uri + "/"
    }
    $tarName = "BigBang.Cloud.ERP.Accounting_"+$version+".tar"
    $tarFullPath = Join-Path $tempDirectory  $tarName
    Write-Host "save docker tar file"
    docker save $imageName -o $tarFullPath

    $uri = $uri + "BigBang.Cloud.ERP.Accounting"+"/"+$tarName
    
    Write-Host "Push tar file to server"+$uri
    if(-Not [string]::IsNullOrEmpty($ArchiveUsername)){
        $User = $ArchiveUsername
        $PWord = ConvertTo-SecureString -String $ArchivePassword -AsPlainText -Force
        $Credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $User, $PWord
        
        $Params = @{
	        Uri = $uri
	        Authentication = "Basic"
	        Credential = $Credential
            Method = "PUT"
            InFile = $tarFullPath
        }
        Invoke-RestMethod @Params
    }else{
        $Params = @{
	        Uri = $uri
            Method = "PUT"
            InFile = $tarFullPath
        }
        Invoke-RestMethod @Params
    }

}

#Create git tag and push it to remote repository
if(-Not $IgnoreGit){
    Create-Tag
    Push-Tag
}