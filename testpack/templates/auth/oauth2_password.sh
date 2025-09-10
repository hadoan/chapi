# oauth2 password
TOKEN_URL="${TOKEN_URL}"
TOKEN=$(curl -s -X POST "$TOKEN_URL" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "grant_type=password" \
  --data-urlencode "client_id=$CLIENT_ID" \
  --data-urlencode "client_secret=$CLIENT_SECRET" \
  --data-urlencode "username=$ROPC_USERNAME" \
  --data-urlencode "password=$ROPC_PASSWORD" \
  --data-urlencode "scope=$SCOPES" \
  --data-urlencode "audience=$AUDIENCE" \
  | sed -n 's/.*"access_token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')
[ -z "$TOKEN" ] && echo "Failed to obtain token" && exit 1
AUTH_HEADER="Authorization: Bearer $TOKEN"

echo "$AUTH_HEADER"
