# basic auth (PowerShell)
if (Get-Command -Name ConvertTo-Base64 -ErrorAction SilentlyContinue) {
    # hypothetical helper
}
$pair = "$env:BASIC_USERNAME`:$env:BASIC_PASSWORD"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($pair)
$basic = [Convert]::ToBase64String($bytes)
$auth = "Authorization: Basic $basic"
Write-Output $auth
