# custom login (PowerShell)
$loginUrl = $env:LOGIN_URL
$user = $env:LOGIN_USERNAME
$pass = $env:LOGIN_PASSWORD
$userKey = $env:LOGIN_USER_KEY -or 'username'
$passKey = $env:LOGIN_PASS_KEY -or 'password'
$bodyType = $env:LOGIN_BODY_TYPE -or 'json'
if ($bodyType -eq 'form') {
    $form = @{ $userKey = $user; $passKey = $pass }
    $resp = Invoke-RestMethod -Method Post -Uri $loginUrl -Body $form -ContentType 'application/x-www-form-urlencoded'
} else {
    $json = @{ $userKey = $user; $passKey = $pass } | ConvertTo-Json
    $resp = Invoke-RestMethod -Method Post -Uri $loginUrl -Body $json -ContentType 'application/json'
}
$token = $resp.access_token
if (-not $token) { Write-Error 'Failed to parse token from login response'; exit 1 }
$auth = "Authorization: Bearer $token"
Write-Output $auth
