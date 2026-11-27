#!/usr/bin/env bash
# Seeds the five persona users for local development into the "smartfactory" realm.
# Runs inside the Keycloak image (kcadm.sh available). Idempotent: re-running
# updates roles and resets the password but never duplicates users.
#
# Why a script and not users in the realm export: a realm export with credentials
# is a password in the repository, which CLAUDE.md forbids. Passwords come from
# .env through the environment.
set -euo pipefail

: "${KEYCLOAK_URL:?KEYCLOAK_URL is required}"
: "${KC_BOOTSTRAP_ADMIN_USERNAME:?KC_BOOTSTRAP_ADMIN_USERNAME is required}"
: "${KC_BOOTSTRAP_ADMIN_PASSWORD:?KC_BOOTSTRAP_ADMIN_PASSWORD is required}"
: "${DEV_USER_PASSWORD:?DEV_USER_PASSWORD is required}"
DEV_TENANT_ID="${DEV_TENANT_ID:-tnt-alsafa}"
REALM="smartfactory"
KCADM=/opt/keycloak/bin/kcadm.sh

echo "[seed] waiting for Keycloak at ${KEYCLOAK_URL} ..."
for i in $(seq 1 30); do
  if "$KCADM" config credentials --server "$KEYCLOAK_URL" --realm master \
       --user "$KC_BOOTSTRAP_ADMIN_USERNAME" --password "$KC_BOOTSTRAP_ADMIN_PASSWORD" >/dev/null 2>&1; then
    break
  fi
  sleep 2
  if [ "$i" -eq 30 ]; then echo "[seed] Keycloak not reachable, giving up"; exit 1; fi
done
echo "[seed] authenticated as ${KC_BOOTSTRAP_ADMIN_USERNAME}"

# username | first | last | realm role
PERSONAS=(
  "manager|Reem|Al Mansoori|reliability-manager"
  "planner|Omar|Haddad|maintenance-planner"
  "technician|Yusuf|Karim|technician"
  "mlengineer|Lina|Farah|ml-engineer"
  "admin-sf|Site|Administrator|administrator"
)

for entry in "${PERSONAS[@]}"; do
  IFS='|' read -r username first last role <<<"$entry"
  email="${username}@alsafa.example"

  existing_id=$("$KCADM" get users -r "$REALM" -q "username=${username}" -q exact=true --fields id --format csv --noquotes 2>/dev/null | head -n1 || true)
  if [ -z "$existing_id" ]; then
    user_id=$("$KCADM" create users -r "$REALM" \
      -s "username=${username}" -s "email=${email}" -s emailVerified=true \
      -s "firstName=${first}" -s "lastName=${last}" -s enabled=true \
      -s "attributes.tenant_id=${DEV_TENANT_ID}" -i)
    echo "[seed] created ${username} (${role})"
  else
    user_id="$existing_id"
    "$KCADM" update "users/${user_id}" -r "$REALM" -s "attributes.tenant_id=${DEV_TENANT_ID}" -s enabled=true
    echo "[seed] updated ${username} (${role})"
  fi

  "$KCADM" set-password -r "$REALM" --userid "$user_id" --new-password "$DEV_USER_PASSWORD"
  "$KCADM" add-roles -r "$REALM" --uid "$user_id" --rolename "$role" >/dev/null 2>&1 || true
done

echo "[seed] done. Users: manager, planner, technician, mlengineer, admin-sf (tenant ${DEV_TENANT_ID})."
echo "[seed] All data in this realm is synthetic. Passwords are local-only."
