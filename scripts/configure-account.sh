#!/usr/bin/env bash
set -euo pipefail

ACCOUNT_ID="${1:-}"
if [ -z "$ACCOUNT_ID" ]; then
  ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
fi

if ! [[ "$ACCOUNT_ID" =~ ^[0-9]{12}$ ]]; then
  echo "Usage: $0 [12-digit AWS account id]" >&2
  exit 1
fi

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_DIR="$ROOT/backend/restaurant-backend-app"
SDCT_DIR="$BACKEND_DIR/.syndicate-config-restaurant-backend-app"
OLD_ID="747482682250"

for file in \
  "$ROOT/infra/terraform/backend.tf" \
  "$ROOT/infra/terraform/variables.tf" \
  "$SDCT_DIR/syndicate.yml" \
  "$SDCT_DIR/syndicate_aliases.yml"; do
  sed -i.bak "s/$OLD_ID/$ACCOUNT_ID/g" "$file" && rm -f "$file.bak"
done

sed -i.bak "s|^deploy_target_bucket:.*|deploy_target_bucket: \"marathon2026-restaurant-artifacts-$ACCOUNT_ID\"|" "$SDCT_DIR/syndicate.yml"
sed -i.bak "s|^project_path:.*|project_path: \"$BACKEND_DIR\"|" "$SDCT_DIR/syndicate.yml"
rm -f "$SDCT_DIR/syndicate.yml.bak"

echo "Account id set to $ACCOUNT_ID"
echo "Syndicate project_path set to $BACKEND_DIR"
grep -n "account_id\|deploy_target_bucket\|project_path" "$SDCT_DIR/syndicate.yml"
grep -n "bucket" "$ROOT/infra/terraform/backend.tf"
