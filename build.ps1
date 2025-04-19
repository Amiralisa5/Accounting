param (
    
    [switch]$PreventFinalBuild=$false,
    [switch]$Release=$false
    
    )
function checkCommandResult {
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
dotnet restore
checkCommandResult "restore nuget packages"

bigbang migrate
checkCommandResult "database migrate"

bigbang domain
checkCommandResult "domain generation"

#build solution
if(-Not $PreventFinalBuild){
    if($Release){
        dotnet build --no-restore --configuration Release    
    }else{
        dotnet build --no-restore --configuration Debug
    }
    checkCommandResult 'build solution'
}