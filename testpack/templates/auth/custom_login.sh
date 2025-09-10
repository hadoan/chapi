# custom login
LOGIN_URL="${LOGIN_URL}"
LOGIN_USERNAME="${LOGIN_USERNAME}"
LOGIN_PASSWORD="${LOGIN_PASSWORD}"
LOGIN_USER_KEY="${LOGIN_USER_KEY:-username}"
LOGIN_PASS_KEY="${LOGIN_PASS_KEY:-password}"
LOGIN_BODY_TYPE="${LOGIN_BODY_TYPE:-json}"

if command -v jq >/dev/null 2>&1; then
  if [ "$LOGIN_BODY_TYPE" = "json" ]; then
    CL_BODY_JSON=$(jq -n --arg u "$LOGIN_USERNAME" --arg p "$LOGIN_PASSWORD" '{($LOGIN_USER_KEY):$u, ($LOGIN_PASS_KEY):$p}' 2>/dev/null)
  fi
fi
if [ "$LOGIN_BODY_TYPE" = "form" ] || [ -z "$CL_BODY_JSON" ]; then
  CONTENT_TYPE="application/x-www-form-urlencoded"
  BODY="username=$LOGIN_USERNAME&password=$LOGIN_PASSWORD"
else
  CONTENT_TYPE="application/json"
  BODY="$CL_BODY_JSON"
fi
RESP=$(curl -s -X POST "$LOGIN_URL" -H "Content-Type: $CONTENT_TYPE" -d "$BODY")
TOKEN=$(echo "$RESP" | sed -n 's/.*"access_token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')
[ -z "$TOKEN" ] && echo "Failed to parse token from login response" && exit 1
AUTH_HEADER="Authorization: Bearer $TOKEN"

echo "$AUTH_HEADER"
