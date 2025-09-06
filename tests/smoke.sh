#!/usr/bin/env bash
# ShipMvp - Projects API smoke tests (curl)
# Usage:
#   chmod +x smoke.sh && ./smoke.sh

set -euo pipefail

BASE_URL="${BASE_URL:-{{BASE_URL}}}"
CLIENT_ID="${CLIENT_ID:-{{CLIENT_ID}}}"
CLIENT_SECRET="${CLIENT_SECRET:-{{CLIENT_SECRET}}}"
USERNAME="${USERNAME:-{{USERNAME}}}"
PASSWORD="${PASSWORD:-{{PASSWORD}}}"
TOKEN="${TOKEN:-}"

# Ensure BODY_FILE is always defined to avoid unbound variable errors
BODY_FILE=""

# If BASE_URL looks templated (contains {{ or }}), default to localhost for local runs
if [[ "$BASE_URL" == *"{{"* || "$BASE_URL" == *"}}"* || -z "$BASE_URL" ]]; then
  echo "WARN: BASE_URL appears templated or empty ('$BASE_URL') - defaulting to http://localhost:5066"
  BASE_URL="http://localhost:5066"
fi

_tmp() { mktemp -t smk.XXXXXX; }
has_jq() { command -v jq >/dev/null 2>&1; }
now() { date -u +"%Y-%m-%dT%H:%M:%SZ"; }

PASS=0
FAIL=0
STEP=0
log() { echo "[$(now)] $*"; }
pass() { echo "✅ $*"; PASS=$((PASS+1)); }
fail() { echo "❌ $*"; FAIL=$((FAIL+1)); }

expect_code() {
  local got="$1" expected="$2" name="$3"
  if [[ "$got" == "$expected" ]]; then
    pass "$name -> $expected"
  else
    fail "$name -> expected $expected, got $got"
  fi
}

auth_header() {
  if [[ -n "${TOKEN:-}" ]]; then
    printf "Authorization: Bearer %s" "$TOKEN"
  fi
}

# do_request METHOD PATH [DATA_JSON]
# Writes body to $BODY_FILE, returns status code on stdout
do_request() {
  local method="$1"; shift
  local path="$1"; shift
  local data="${1:-}"
  local url="${BASE_URL%/}${path}"
  local body_file
  body_file=$(_tmp)

  local curl_args=( -sS -o "$body_file" -w "%{http_code}" -X "$method" "$url" -H "Accept: application/json" )
  if [[ -n "$data" ]]; then
    curl_args+=( -H "Content-Type: application/json" --data "$data" )
  fi
  if [[ -n "${TOKEN:-}" ]]; then
    curl_args+=( -H "$(auth_header)" )
  fi

  local code
  code="$(curl "${curl_args[@]}")" || true
  BODY_FILE="$body_file"
  echo -n "$code"
}

extract_json() {
  local field="$1" file="$2"
  # Defensive: if file is empty or not provided, return empty
  if [[ -z "${file:-}" || ! -s "$file" ]]; then
    echo ""
    return 0
  fi
  if has_jq; then
    jq -r --arg f "$field" '.[$f] // empty' -- "$file" 2>/dev/null || echo ""
  else
    sed -n "s/.*\"$field\":\"\([^\"]*\)\".*/\1/p" "$file" | head -n1 || echo ""
  fi
}

get_token() {
  [[ -n "${TOKEN:-}" ]] && { log "Using existing TOKEN"; return 0; }

  local url="${BASE_URL%/}/connect/token"
  local code body_file
  body_file=$(_tmp)

  # Default scopes for this app
  local SCOPE
  SCOPE="${SCOPE:-openid email profile roles}"

  if [[ -n "$USERNAME" && -n "$PASSWORD" ]]; then
    log "Fetching token via password grant (scope=$SCOPE)…"
    code="$(curl -sS -o "$body_file" -w "%{http_code}" -X POST "$url" \
      -H "Content-Type: application/x-www-form-urlencoded" \
      --data-urlencode "grant_type=password" \
      --data-urlencode "client_id=spa-client" \
      --data-urlencode "username=admin@shipmvp.com" \
      --data-urlencode "password=Admin123!" \
      --data-urlencode "scope=${SCOPE}" )" || true
  elif [[ -n "$CLIENT_ID" && -n "$CLIENT_SECRET" ]]; then
    log "Fetching token via client_credentials grant (scope=$SCOPE)…"
    code="$(curl -sS -o "$body_file" -w "%{http_code}" -X POST "$url" \
      -H "Content-Type: application/x-www-form-urlencoded" \
      --data-urlencode "grant_type=client_credentials" \
      --data-urlencode "client_id=${CLIENT_ID}" \
      --data-urlencode "client_secret=${CLIENT_SECRET}" \
      --data-urlencode "scope=${SCOPE}" )" || true
  else
    log "No TOKEN/credentials provided; will only run unauthorized checks."
    return 0
  fi

  if [[ -z "$code" ]]; then
    fail "Token request produced no HTTP code (network error). Body:"; [[ -s "$body_file" ]] && sed -e 's/^/    /' "$body_file" || echo "    <no-body>"
    return 1
  fi

  if [[ "$code" != "200" ]]; then
    fail "Token request failed ($code). Body:"; [[ -s "$body_file" ]] && sed -e 's/^/    /' "$body_file" || echo "    <no-body>"
    return 1
  fi

  # Defensive: don't call jq/sed on an empty body file
  if [[ ! -s "$body_file" ]]; then
    TOKEN=""
  else
    if has_jq; then
      TOKEN="$(jq -r '.access_token // empty' -- "$body_file" 2>/dev/null || echo "")"
    else
      TOKEN="$(sed -n 's/.*"access_token":"\([^\"]*\)".*/\1/p' "$body_file" | head -n1 || echo "")"
    fi
  fi

  if [[ -n "$TOKEN" ]]; then
    pass "Obtained access token"
  else
    fail "Could not parse access_token from token response"; [[ -s "$body_file" ]] && sed -e 's/^/    /' "$body_file" || echo "    <no-body>"
    return 1
  fi
}

section() { STEP=$((STEP+1)); echo; echo "===== [$STEP] $* ====="; }

# 1) Unauthorized list should be 401
section "Projects: unauthorized GET should fail"
TOKEN="" # force unauth
CODE="$(do_request GET "/api/projects")"
expect_code "$CODE" "401" "GET /api/projects (no auth)"
rm -f "${BODY_FILE:-}"

# 2) Acquire token (if creds provided)
section "Auth: acquire token (optional)"
get_token || true

# 3) Create a project
section "Projects: create"
NAME="Smoke Project $(date +%s)"
DATA_CREATE=$(cat <<JSON
{"name":"$NAME","description":"Created by smoke.sh","tasks":[{"title":"Kickoff","description":"initial task"}]}
JSON
)
CODE="$(do_request POST "/api/projects" "$DATA_CREATE")"
expect_code "$CODE" "201" "POST /api/projects"
CREATE_BODY="${BODY_FILE:-}"

PROJECT_ID="$(extract_json "id" "$CREATE_BODY")"
if [[ -z "$PROJECT_ID" ]]; then
  fail "Could not extract project id from create response"
else
  pass "Created project id: $PROJECT_ID"
fi

# 4) Get list (authorized)
section "Projects: list"
CODE="$(do_request GET "/api/projects")"
expect_code "$CODE" "200" "GET /api/projects"
rm -f "${BODY_FILE:-}"

# 5) Get by id
section "Projects: get by id"
CODE="$(do_request GET "/api/projects/$PROJECT_ID")"
expect_code "$CODE" "200" "GET /api/projects/{id}"
rm -f "${BODY_FILE:-}"

# 6) Update
section "Projects: update"
NEW_NAME="${NAME} (updated)"
DATA_UPDATE=$(cat <<JSON
{"name":"$NEW_NAME","description":"Updated by smoke.sh","tasks":[{"title":"Kickoff","description":"initial task"}]}
JSON
)
CODE="$(do_request PUT "/api/projects/$PROJECT_ID" "$DATA_UPDATE")"
expect_code "$CODE" "200" "PUT /api/projects/{id}"
rm -f "${BODY_FILE:-}"

# 7) Activate
section "Projects: activate"
CODE="$(do_request POST "/api/projects/$PROJECT_ID/activate")"
expect_code "$CODE" "200" "POST /api/projects/{id}/activate"
rm -f "${BODY_FILE:-}"

# 8) Put on hold
section "Projects: hold"
CODE="$(do_request POST "/api/projects/$PROJECT_ID/hold")"
expect_code "$CODE" "200" "POST /api/projects/{id}/hold"
rm -f "${BODY_FILE:-}"

# 9) Archive
section "Projects: archive"
CODE="$(do_request POST "/api/projects/$PROJECT_ID/archive")"
expect_code "$CODE" "200" "POST /api/projects/{id}/archive"
rm -f "${BODY_FILE:-}"

# 10) Delete
section "Projects: delete"
CODE="$(do_request DELETE "/api/projects/$PROJECT_ID")"
expect_code "$CODE" "200" "DELETE /api/projects/{id}"
rm -f "${BODY_FILE:-}"

# 11) Verify 404 after delete
section "Projects: get after delete -> 404"
CODE="$(do_request GET "/api/projects/$PROJECT_ID")"
expect_code "$CODE" "404" "GET /api/projects/{id} after delete"
rm -f "${BODY_FILE:-}"

# 12) Pagination sanity
section "Projects: pagination"
CODE="$(do_request GET "/api/projects?Page=1&PageSize=5")"
expect_code "$CODE" "200" "GET /api/projects?Page=1&PageSize=5"
rm -f "${BODY_FILE:-}"

# ------------- summary -------------
echo; echo "================ Summary ================"; echo "Passed:  $PASS"; echo "Failed:  $FAIL"; echo "========================================="

[[ "$FAIL" -eq 0 ]] || exit 1
