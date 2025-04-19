
function Check-Command-Result {
    param (
        $message
    )
    if($LASTEXITCODE -ne 0){
        $exitCode = $LASTEXITCODE
        $finalMessage = 'Error in ' + $message
        Write-Error $finalMessage
        Exit $exitCode
    }
}

$GitVersion = dotnet-gitversion | ConvertFrom-Json
$version = $GitVersion.SemVer
if([string]::IsNullOrEmpty($version)){
    Write-Warning "No version is detected"
    $version="0.0.0.0"
}

Write-Information "Version:${$version}"

$branch = $null
$remoteBranch = $null
git status 2>$null
if($LASTEXITCODE -eq 0){
    $branch = git rev-parse --abbrev-ref HEAD
    $remoteBranch = "origin/"+$branch
}else{
    $LASTEXITCODE = 0
}
function getIsThereAnyRemoteBranch(){
    git rev-parse --verify $remoteBranch 2>$null
    if($LASTEXITCODE -ne 0){
        $LASTEXITCODE=0
        return $false
    }else{
        return $true
    }
}
$isThereAnyRemoteBranch = getIsThereAnyRemoteBranch
function Create-Tag {
    if(-Not $branch){
        return
    }
    git tag $version
}
function Push-Tag {
    if(-Not $branch){
        return
    }
    if($isThereAnyRemoteBranch){
        git push origin $version    
    }
}

function Check-If-Any-Uncommited-File {
    if(-Not $branch){
        return;
    }

    if(git status --porcelain |Where-Object {$_ -match '^\?\?'}){
        # untracked files exist
        Write-Error "Uncommited file exists"
        Exit 1
    } 
    elseif(git status --porcelain |Where-Object {$_ -notmatch '^\?\?'}) {
        # uncommitted changes
        Write-Error "Uncommited file exists"
        Exit 1
    }
    else {
        # tree is clean
    }
}
function Check-If-Any-Unpushed-Commits {
    if(-Not $branch){
        return;
    }
    if(-Not $isThereAnyRemoteBranch){
        return
    }
    $localBranchSha = git rev-parse --verify $branch
    $remoteBranchSha = git rev-parse --verify $remoteBranch
    if($localBranchSha -ne $remoteBranchSha){
        Write-Error "Local Branch ${branch} is not as same as remote branch ${remoteBranch}"
        Exit 1
    }
}
function Check-Git{
    Check-If-Any-Uncommited-File
    Check-If-Any-Unpushed-Commits
}



function Discover-Version{

    return $version
}

Export-ModuleMember -Function Check-Command-Result
Export-ModuleMember -Function Check-Git
Export-ModuleMember -Function Create-Tag
Export-ModuleMember -Function Push-Tag
Export-ModuleMember -Function Discover-Version
