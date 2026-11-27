#!/usr/bin/env bash
# Gate G1 end-to-end check against the running Docker Compose stack (docs/ROADMAP.md, Week 2):
#   docker compose up -d → login → asset list.
# Usage (repository root, stack running):  scripts/g1-check.sh
# Needs bash, curl and python3 (Linux, macOS, WSL or Git Bash). CI runs the same script.
# The browser half of the gate (a real PKCE login in Chromium) is scripts/g1-browser-check.cjs.
set -euo pipefail

API="${API_URL:-http://localhost:5080}"
KEYCLOAK="${KEYCLOAK_URL:-http://localhost:8180}"
WEB="${WEB_URL:-http://localhost:4200}"
REALM="smartfactory"
TENANT="tnt-alsafa"
SEEDED_CODES="CHL-003 CMP-007 CNC-018 CVR-012 FAN-034 MTR-042 PMP-211 PRS-105"

# Why not "source .env": that would execute whatever the file contains. Read the one value needed.
if [ -z "${DEV_USER_PASSWORD:-}" ] && [ -f .env ]; then
  DEV_USER_PASSWORD="$(grep -E '^DEV_USER_PASSWORD=' .env | head -n1 | cut -d= -f2-)"
fi
: "${DEV_USER_PASSWORD:?set DEV_USER_PASSWORD or run from the repository root with a .env file}"

pass() { printf '  \033[32m✔\033[0m %s\n' "$1"; }
fail() { printf '  \033[31m✘\033[0m %s\n' "$1" >&2; exit 1; }
json() { python3 -c "import json,sys; d=json.load(sys.stdin); $1"; }

wait_for() { # name url seconds
  local i
  for i in $(seq 1 "$3"); do
    if curl -fsS -o /dev/null "$2" 2>/dev/null; then pass "$1 is up ($2)"; return 0; fi
    sleep 1
  done
  fail "$1 did not answer at $2 within $3 s"
}

echo "G1 check: Compose stack → login → asset list (synthetic data)"

wait_for "Platform API" "$API/health/ready" 180
wait_for "Keycloak realm" "$KEYCLOAK/realms/$REALM/.well-known/openid-configuration" 180
wait_for "Web app" "$WEB/" 60

status=$(curl -s -o /dev/null -w '%{http_code}' "$API/api/assets/assets")
[ "$status" = "401" ] && pass "anonymous asset list is refused (401)" || fail "anonymous asset list returned $status, expected 401"

# Password grant on the test-only client (direct access grants enabled; the web client uses PKCE only).
token=$(curl -fsS "$KEYCLOAK/realms/$REALM/protocol/openid-connect/token" \
  -d grant_type=password -d client_id=smartfactory-authz-tests \
  -d username=planner --data-urlencode "password=$DEV_USER_PASSWORD" -d scope=openid \
  | json 'print(d["access_token"])') || fail "planner could not sign in (is keycloak-seed done?)"
pass "planner signed in through Keycloak"
auth=(-H "Authorization: Bearer $token")

curl -fsS "${auth[@]}" "$API/api/me" | json "
assert d['tenantId'] == '$TENANT', d
assert 'maintenance-planner' in d['roles'], d" || fail "/api/me does not show tenant $TENANT and the planner role"
pass "the API sees tenant $TENANT and role maintenance-planner"

curl -fsS "${auth[@]}" "$API/api/assets/assets" | json "
codes = sorted(a['code'] for a in d)
missing = set('$SEEDED_CODES'.split()) - set(codes)
assert not missing, f'missing {missing}; got {codes}'
assert sum(a['sensorCount'] for a in d if a['code'] in '$SEEDED_CODES'.split()) == 32
print('    ' + ' '.join(codes))" || fail "asset list does not hold the 8 seeded machines with 32 sensors"
pass "asset list holds the 8 seeded machines and their 32 sensors"

curl -fsS "${auth[@]}" "$API/api/assets/plants" | json "
assert [l['code'] for p in d for l in p['lines']] == ['line-a', 'line-b'], d" || fail "plant does not have line-a and line-b"
pass "plant-01 has line-a and line-b"

allow=$(curl -s -o /dev/null -D - -X OPTIONS "$API/api/assets/assets" \
  -H "Origin: $WEB" -H "Access-Control-Request-Method: GET" -H "Access-Control-Request-Headers: authorization" \
  | tr -d '\r' | awk -F': ' 'tolower($1)=="access-control-allow-origin"{print $2}')
[ "$allow" = "$WEB" ] && pass "CORS allows the web origin $WEB" || fail "CORS preflight did not allow $WEB (got '$allow')"

# Why not "curl | grep -q": grep stops at the first match, curl then fails writing the rest and pipefail reports it.
strings=$(curl -fsS "$WEB/i18n/en.json")
grep -q "Synthetic data" <<<"$strings" && pass "web app serves the synthetic-data label" || fail "synthetic-data label missing"
status=$(curl -s -o /dev/null -w '%{http_code}' "$WEB/assets")
[ "$status" = "200" ] && pass "deep link $WEB/assets is served by the SPA" || fail "$WEB/assets returned $status"

echo "G1 check passed."
