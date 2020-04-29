Set-Location $PSScriptRoot\CS526-LobbyServer
git reset --hard HEAD
git pull
.\build.ps1

Set-Location $PSScriptRoot\CS526_SneakRobber
git reset --hard HEAD
git pull
.\build.ps1