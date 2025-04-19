
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

dotnet test test/cloud.erp.accounting.tests/cloud.erp.accounting.tests.csproj

checkCommandResult 'execute tests'
