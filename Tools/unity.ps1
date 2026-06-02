# MidProject — Unity CLI 래퍼 (Unity.exe 경로 고정)
# 사용: .\Tools\unity.ps1 -batchmode -quit -logFile Logs\cli.log
# 주의: 같은 프로젝트를 Unity 에디터에서 열어 두면 batchmode가 실패합니다.

param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$UnityArgs
)

$EditorVersion = "6000.4.8f1"
$UnityExe = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$EditorVersion\Editor\Unity.exe"
$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

if (-not (Test-Path $UnityExe)) {
    Write-Error "Unity $EditorVersion 을 찾을 수 없습니다. Unity Hub에서 해당 버전을 설치하세요."
    Write-Host "경로 예: $UnityExe"
    exit 1
}

$allArgs = @("-projectPath", $ProjectRoot) + $UnityArgs
Write-Host "[unity.ps1] $UnityExe"
Write-Host "[unity.ps1] project: $ProjectRoot"
& $UnityExe @allArgs
exit $LASTEXITCODE
