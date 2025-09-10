# oauth2 password (PowerShell)
$tokenUrl = $env:TOKEN_URL
$body = @{
    grant_type = 'password'
    client_id = $env:CLIENT_ID
    client_secret = $env:CLIENT_SECRET
    username = $env:ROPC_USERNAME
    password = $env:ROPC_PASSWORD
    scope = $env:SCOPES
    audience = $env:AUDIENCE
}
$resp = Invoke-RestMethod -Method Post -Uri $tokenUrl -Body $body -ContentType 'application/x-www-form-urlencoded' -ErrorAction Stop
$token = $resp.access_token
if (-not $token) { Write-Error 'Failed to obtain token'; exit 1 }
$auth = "Authorization: Bearer $token"
Write-Output $auth
