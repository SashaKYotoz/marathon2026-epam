# Green & Tasty | EPAM IT Marathon 2026

Green & Tasty is a restaurant web application. Customers book tables, pre-order dishes and leave feedback. Waiters manage the bookings at their location, and managers get reports.

## Features

- **Locations and menus:** browse restaurants, their dishes and the available tables.
- **Reservations:** book a time slot for a number of guests, then edit, postpone or cancel it.
- **Pre-orders:** choose dishes in advance so the kitchen is ready when guests arrive.
- **Group pre-order:** for a booking with 2 or more guests, the owner shares an invite link. Guests join without an account and choose and confirm their own dishes. The owner sees every participant with their dishes and totals, can remove participants, and confirms the group. The confirmed carts are combined into the booking's pre-order, which the waiter sees.
- **Waiter dashboard:** the bookings at the waiter's location, service status, and pre-orders.
- **Feedback and reports:** guests rate service and food; reports are generated and e-mailed on a schedule.

## Architecture

| Part | Technology | Folder |
|---|---|---|
| Frontend | Angular 22 (standalone components, signals), served from S3 through CloudFront | `frontend/restaurant-frontend-app` |
| Backend | .NET 10 AWS Lambda functions behind API Gateway, DynamoDB, Cognito sign-in, deployed with AWS Syndicate | `backend/restaurant-backend-app` |
| Shared infrastructure | Terraform: Cognito, the frontend S3 bucket and CloudFront, SQS, EventBridge, monitoring and alerts | `infra/terraform` |
| End-to-end tests | Reqnroll + TUnit + Playwright for .NET, Screenplay pattern | `aqa/restaurant-aqa-framework` |
| CI/CD | GitHub Actions | `.github/workflows` |

The backend has three Lambda functions:

- `api-handler` serves the REST API.
- `reports-handler` builds reports.
- `report-sender` e-mails them.

The API routes are listed in `backend/restaurant-backend-app/docs/ApiRoutes.md`. Everything is deployed to `us-west-2`.

## Requirements

- AWS CLI v2, configured for your account (`aws configure`, region `us-west-2`)
- Terraform 1.10 or newer
- Python 3.10 or 3.11, for AWS Syndicate
- .NET 10 SDK
- Node.js 22.x with npm
- Git

## Run locally

The frontend runs on your machine against a deployed backend.

1. Set `backendUrl` in `frontend/restaurant-frontend-app/src/environments/environment.development.ts` to your API URL, for example `https://<api-id>.execute-api.us-west-2.amazonaws.com/develop`.
2. Start the frontend:

```bash
cd frontend/restaurant-frontend-app
npm ci
npm start
```

The app opens at http://localhost:4200.

To run the checks:

```bash
cd backend/restaurant-backend-app
dotnet build restaurant-backend-app.sln
dotnet test Tests/APIHandler.Tests.csproj

cd ../../frontend/restaurant-frontend-app
npx tsc -p tsconfig.app.json --noEmit
npm test -- --watch=false --browsers=ChromeHeadless
```

## Deploy to AWS

Deploy in this order: infrastructure, then backend, then frontend. The backend finds the Cognito user pool by name, so Terraform has to run first.

The commands below are for Git Bash, Linux or macOS. On Windows PowerShell, notes follow each step.

### 1. Point the configuration at your account

```bash
./scripts/configure-account.sh
```

On PowerShell, run `.\scripts\configure-account.ps1`.

The script reads your account id from the AWS CLI. It writes the id into:

- `infra/terraform/backend.tf` and `infra/terraform/variables.tf`
- the Syndicate files in `backend/restaurant-backend-app/.syndicate-config-restaurant-backend-app/`, where it also sets `project_path`

Then set your e-mail for alerts in `alert_emails` in `infra/terraform/variables.tf`.

### 2. Deploy the infrastructure (Terraform)

Create the bucket that stores the Terraform state, once:

```bash
export AWS_REGION=us-west-2
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)

aws s3api create-bucket --bucket restaurant-terraform-state-$ACCOUNT_ID \
  --create-bucket-configuration LocationConstraint=us-west-2
aws s3api put-bucket-versioning --bucket restaurant-terraform-state-$ACCOUNT_ID \
  --versioning-configuration Status=Enabled
aws s3api put-public-access-block --bucket restaurant-terraform-state-$ACCOUNT_ID \
  --public-access-block-configuration "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
```

Then apply:

```bash
cd infra/terraform
terraform init
terraform validate
terraform plan
terraform apply -auto-approve
terraform output cloudfront_distribution_domain_name
cd ../..
```

The last command prints your frontend address. A new AWS account may need to be verified by AWS Support before it can create a CloudFront distribution.

### 3. Deploy the backend (AWS Syndicate)

```bash
cd backend/restaurant-backend-app
python3 -m venv syndicate_venv
source syndicate_venv/bin/activate
pip install aws-syndicate
export SDCT_CONF="$(pwd)/.syndicate-config-restaurant-backend-app"
syndicate create-deploy-target-bucket
syndicate build
syndicate deploy --deploy_name dev
deactivate
cd ../..
```

On Windows:

- Create the environment with `py -3.11 -m venv syndicate_venv`, because `python3` may be the Microsoft Store shortcut.
- In PowerShell, activate it with `.\syndicate_venv\Scripts\Activate.ps1` and set `$env:SDCT_CONF = (Resolve-Path ".\.syndicate-config-restaurant-backend-app").Path`.
- In Git Bash, use `source syndicate_venv/Scripts/activate` and `$(pwd -W)`.
- Keep the repository in a short path such as `C:\m26`, because Windows limits paths to 260 characters.

After a code change, run `syndicate build`, then `syndicate update`.

Get the API URL:

```bash
API_ID=$(aws apigateway get-rest-apis --region us-west-2 --query "items[?name=='restaurant-backend-api'].id | [0]" --output text)
export BACKEND_URL="https://$API_ID.execute-api.us-west-2.amazonaws.com/develop"
echo $BACKEND_URL
```

### 4. Deploy the frontend

```bash
cd frontend/restaurant-frontend-app
npm ci
npm run build
aws s3 sync dist/restaurant-frontend-app/browser s3://restaurant-frontend-$ACCOUNT_ID/ --delete
DIST_ID=$(cd ../../infra/terraform && terraform output -raw cloudfront_distribution_id)
aws cloudfront create-invalidation --distribution-id $DIST_ID --paths "/*"
cd ../..
```

`npm run build` reads `BACKEND_URL` from step 3 and fails if it isn't set. On PowerShell, set it with `$env:BACKEND_URL = "https://..."`.

### 5. Add the first data

A new deployment has no locations, tables or waiters. Waiters can't sign up, so register one and hire them at a location:

```bash
curl -s -X POST "$BACKEND_URL/locations" -H "Content-Type: application/json" \
  -d '{"address":"Main Street 1","description":"Demo location","totalCapacity":40,"averageOccupancy":"0"}'

LOCATION_ID=paste-the-id-from-the-previous-response

curl -s -X POST "$BACKEND_URL/register" -H "Content-Type: application/json" \
  -d '{"firstName":"Standing","lastName":"Waiter","email":"waiter.aqa@example.com","password":"Waiter-Pass1!","confirmPassword":"Waiter-Pass1!"}'

curl -s -X POST "$BACKEND_URL/waiters" -H "Content-Type: application/json" \
  -d "{\"email\":\"waiter.aqa@example.com\",\"locationId\":\"$LOCATION_ID\"}"

curl -s -X POST "$BACKEND_URL/tables" -H "Content-Type: application/json" \
  -d "{\"locationId\":\"$LOCATION_ID\",\"capacity\":6,\"tableNumber\":1}"
```

Create the location only once. A location without a waiter can't take bookings, and a location without tables shows every time slot as unavailable. Dishes are added with `POST /dishes`; the request body is in `docs/ApiRoutes.md`.

## Continuous integration

| Workflow | What it does |
|---|---|
| `terraform.yml` | plans and applies `infra/terraform` |
| `deploy.yml` | runs the backend unit tests, then builds and updates the backend with Syndicate |
| `pr.yml` | type-checks, builds and tests the frontend; on `main` it deploys to S3 and invalidates CloudFront |
| `aqa-quality.yml` | format, build and Gherkin lint for the test framework |
| `aqa-tests.yml` | runs the end-to-end scenarios, nightly and on demand |

The workflows sign in to AWS through GitHub's OIDC provider. In IAM:

1. Add the identity provider `https://token.actions.githubusercontent.com` with audience `sts.amazonaws.com`.
2. Create a role that trusts it, limited to `repo:<owner>/<repo>:*`.

Then add these under **Settings → Secrets and variables → Actions**:

| Kind | Name | Value |
|---|---|---|
| Secret | `AWS_DEPLOY_ROLE`, `AWS_TERRAFORM_ROLE` | the role ARN |
| Variable | `ALERT_EMAILS` | `["you@example.com"]` |
| Variable | `BACKEND_URL` | optional, the API URL |
| Variable | `AQA_UI_URL`, `AQA_API_URL` | the frontend and API URLs for the tests |
| Secret | `AQA_WAITER_USERNAME`, `AQA_WAITER_PASSWORD` | the waiter from step 5 |

## End-to-end tests

The test framework, its setup and the way to run it are described in [aqa/restaurant-aqa-framework/README.md](aqa/restaurant-aqa-framework/README.md). In short:

```bash
cd aqa/restaurant-aqa-framework
cp .env.example .env
dotnet build
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=group-preorder]"
```

Before running, set `UI_URL`, `API_URL` and the waiter account in `.env`.

## Clean up

Remove everything in the reverse order of deployment, from the repository root:

```bash
export AWS_REGION=us-west-2
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
STATE_BUCKET=restaurant-terraform-state-$ACCOUNT_ID
ARTIFACTS_BUCKET=$(sed -n 's/^deploy_target_bucket: *"\([^"]*\)".*/\1/p' backend/restaurant-backend-app/.syndicate-config-restaurant-backend-app/syndicate.yml)

aws s3 rm "s3://restaurant-frontend-$ACCOUNT_ID" --recursive

cd backend/restaurant-backend-app
source syndicate_venv/bin/activate
export SDCT_CONF="$(pwd)/.syndicate-config-restaurant-backend-app"
syndicate clean
deactivate
cd ../../infra/terraform
terraform destroy -auto-approve
cd ../..

aws s3 rm "s3://$ARTIFACTS_BUCKET" --recursive
aws s3api delete-bucket --bucket "$ARTIFACTS_BUCKET"

aws s3api delete-objects --bucket "$STATE_BUCKET" --delete \
  "$(aws s3api list-object-versions --bucket "$STATE_BUCKET" --query '{Objects: Versions[].{Key:Key,VersionId:VersionId}}' --output json)"
aws s3api delete-objects --bucket "$STATE_BUCKET" --delete \
  "$(aws s3api list-object-versions --bucket "$STATE_BUCKET" --query '{Objects: DeleteMarkers[].{Key:Key,VersionId:VersionId}}' --output json)"
aws s3api delete-bucket --bucket "$STATE_BUCKET"
```

On Windows Git Bash, activate with `source syndicate_venv/Scripts/activate` and use `$(pwd -W)`.

If a `delete-objects` call reports that `Objects` is `null`, there was nothing of that kind to delete.

Deleting the state bucket is permanent: without it, Terraform no longer knows what it created. Keep it if you plan to deploy again.