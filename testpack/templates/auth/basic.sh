# basic auth
if command -v base64 >/dev/null 2>&1; then
  BASIC=$(printf '%s:%s' "$BASIC_USERNAME" "$BASIC_PASSWORD" | base64)
elif command -v openssl >/dev/null 2>&1; then
  BASIC=$(printf '%s:%s' "$BASIC_USERNAME" "$BASIC_PASSWORD" | openssl base64)
else
  echo "Need base64/openssl for Basic Auth"; exit 1
fi
AUTH_HEADER="Authorization: Basic $BASIC"

echo "$AUTH_HEADER"
