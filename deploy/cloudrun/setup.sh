#!/usr/bin/env bash
# One-time Google Cloud setup for the CourseCore API on Cloud Run.
# Run it in Cloud Shell (or any shell with `gcloud auth login` done by an Owner):
#
#   PROJECT_ID=my-project REGION=us-east1 GITHUB_REPO=Leo-Slv/CourseCore bash deploy/cloudrun/setup.sh
#
# Safe to run again: every step checks before creating. It never prints secret values.
set -euo pipefail

: "${PROJECT_ID:?set PROJECT_ID}"
: "${REGION:?set REGION, e.g. us-east1}"
: "${GITHUB_REPO:?set GITHUB_REPO as owner/name, e.g. Leo-Slv/CourseCore}"

REPO_NAME="${REPO_NAME:-coursecore}"
RUNTIME_SA="coursecore-run"
DEPLOYER_SA="github-deployer"
POOL="github"
PROVIDER="github-actions"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

gcloud config set project "$PROJECT_ID" >/dev/null
PROJECT_NUMBER="$(gcloud projects describe "$PROJECT_ID" --format='value(projectNumber)')"

echo "==> Enabling APIs"
gcloud services enable \
  run.googleapis.com \
  artifactregistry.googleapis.com \
  secretmanager.googleapis.com \
  iamcredentials.googleapis.com \
  sts.googleapis.com

echo "==> Artifact Registry repository ($REPO_NAME in $REGION)"
if ! gcloud artifacts repositories describe "$REPO_NAME" --location "$REGION" >/dev/null 2>&1; then
  gcloud artifacts repositories create "$REPO_NAME" \
    --repository-format=docker --location "$REGION" \
    --description="CourseCore API images"
fi
# Keep storage inside the free allowance: only the 5 most recent images are kept.
POLICY_FILE="$(mktemp)"
cat > "$POLICY_FILE" <<'JSON'
[{"name":"keep-recent","action":{"type":"Keep"},"mostRecentVersions":{"keepCount":5}},
 {"name":"delete-the-rest","action":{"type":"Delete"},"condition":{"tagState":"any"}}]
JSON
gcloud artifacts repositories set-cleanup-policies "$REPO_NAME" \
  --location "$REGION" --policy="$POLICY_FILE" --no-dry-run >/dev/null
rm -f "$POLICY_FILE"

echo "==> Runtime service account ($RUNTIME_SA)"
if ! gcloud iam service-accounts describe "$RUNTIME_SA@$PROJECT_ID.iam.gserviceaccount.com" >/dev/null 2>&1; then
  gcloud iam service-accounts create "$RUNTIME_SA" --display-name="CourseCore API runtime"
fi

echo "==> Secrets (values for DB, Turnstile, Resend and Stripe are added by you afterwards)"
while IFS='=' read -r _ secret; do
  [[ -z "${secret:-}" || "$secret" == \#* ]] && continue
  if ! gcloud secrets describe "$secret" >/dev/null 2>&1; then
    gcloud secrets create "$secret" --replication-policy=automatic >/dev/null
    echo "created secret $secret"
  fi
  gcloud secrets add-iam-policy-binding "$secret" \
    --member="serviceAccount:$RUNTIME_SA@$PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/secretmanager.secretAccessor" >/dev/null
done < <(grep -v '^#' "$SCRIPT_DIR/secrets.txt")

# These two are random: generate them once, and only if there is no version yet.
for secret in coursecore-jwt coursecore-media-signing; do
  if [ -z "$(gcloud secrets versions list "$secret" --limit=1 --format='value(name)')" ]; then
    openssl rand -base64 48 | tr -d '\n' | gcloud secrets versions add "$secret" --data-file=- >/dev/null
    echo "generated a random value for $secret (the two are different)"
  fi
done

echo "==> Deployer service account ($DEPLOYER_SA) used by GitHub Actions"
DEPLOYER_EMAIL="$DEPLOYER_SA@$PROJECT_ID.iam.gserviceaccount.com"
if ! gcloud iam service-accounts describe "$DEPLOYER_EMAIL" >/dev/null 2>&1; then
  gcloud iam service-accounts create "$DEPLOYER_SA" --display-name="GitHub Actions deployer"
fi
for role in roles/run.admin roles/artifactregistry.writer; do
  gcloud projects add-iam-policy-binding "$PROJECT_ID" \
    --member="serviceAccount:$DEPLOYER_EMAIL" --role="$role" --condition=None >/dev/null
done
# Allowed to deploy revisions that run AS the runtime account, nothing broader.
gcloud iam service-accounts add-iam-policy-binding \
  "$RUNTIME_SA@$PROJECT_ID.iam.gserviceaccount.com" \
  --member="serviceAccount:$DEPLOYER_EMAIL" --role="roles/iam.serviceAccountUser" >/dev/null

echo "==> Workload Identity Federation (GitHub -> Google, no key files)"
if ! gcloud iam workload-identity-pools describe "$POOL" --location=global >/dev/null 2>&1; then
  gcloud iam workload-identity-pools create "$POOL" --location=global --display-name="GitHub"
fi
if ! gcloud iam workload-identity-pools providers describe "$PROVIDER" --location=global --workload-identity-pool="$POOL" >/dev/null 2>&1; then
  gcloud iam workload-identity-pools providers create-oidc "$PROVIDER" \
    --location=global --workload-identity-pool="$POOL" \
    --issuer-uri="https://token.actions.githubusercontent.com" \
    --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository" \
    --attribute-condition="assertion.repository == '$GITHUB_REPO'"
fi
gcloud iam service-accounts add-iam-policy-binding "$DEPLOYER_EMAIL" \
  --role="roles/iam.workloadIdentityUser" \
  --member="principalSet://iam.googleapis.com/projects/$PROJECT_NUMBER/locations/global/workloadIdentityPools/$POOL/attribute.repository/$GITHUB_REPO" >/dev/null

PROVIDER_NAME="projects/$PROJECT_NUMBER/locations/global/workloadIdentityPools/$POOL/providers/$PROVIDER"

cat <<EOF

================ DONE ================
1) Add the remaining secret values (paste, then Ctrl+D). Examples:
   printf %s '<Npgsql connection string, pooled host>' | gcloud secrets versions add coursecore-db --data-file=-
   printf %s '<Turnstile secret key>'                  | gcloud secrets versions add coursecore-turnstile --data-file=-
   printf %s '<Resend API key>'                        | gcloud secrets versions add coursecore-resend --data-file=-
   printf %s '<sk_test_... / rk_test_...>'             | gcloud secrets versions add coursecore-stripe-key --data-file=-
   printf %s '<whsec_...>'                             | gcloud secrets versions add coursecore-stripe-webhook --data-file=-

2) GitHub repo -> Settings -> Secrets and variables -> Actions -> *Variables*:
   GCP_PROJECT_ID                 = $PROJECT_ID
   GCP_REGION                     = $REGION
   GCP_WORKLOAD_IDENTITY_PROVIDER = $PROVIDER_NAME
   GCP_SERVICE_ACCOUNT            = $DEPLOYER_EMAIL

3) Run the "CourseCore CD" workflow. The API URL it prints (and
   https://coursecore-api-$PROJECT_NUMBER.$REGION.run.app) is the one for the Vercel rewrite
   and for the Stripe webhook.
EOF
