
$originFolder =  Get-Location

cd ..
cd ".\tests\FiapCloudGames.Tests"
$reporttFolder = ".\..\coverage-report"

dotnet test --collect:"XPlat Code Coverage" 
cd ".\TestResults"


$firstFolder = Get-ChildItem | Where-Object { $_.PSIsContainer } | Select-Object -First 1
reportgenerator -reports:"$($firstFolder.FullName)\coverage.cobertura.xml" -targetdir:"$reporttFolder" -classfilters:"-*Migration*;-*Migrations*"

cd ..
Invoke-Item "coverage-report\index.html"
cd $originFolder
