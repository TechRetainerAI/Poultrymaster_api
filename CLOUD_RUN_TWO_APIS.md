# Deploy **two** APIs on Google Cloud Run

## GitHub → Cloud Run “Deploy from repository”

Cloud Build uses the **repository root** and expects **`Dockerfile`** there.

This repo includes a **root `Dockerfile`** that builds **LoginAPI** only.  
If you only wired one Cloud Run service from GitHub, it should be the **Login / User Management** API.

For **PoultryFarmAPI**, add a **second Cloud Run service** and either:

- Build from the same repo using **`cloudbuild.yaml`** (see below), or  
- Build/push the image manually from `PoultryFarmAPI/` (see sections 2–3).

### Optional: `cloudbuild.yaml` for Farm API (same repo)

Create another trigger or use a second `cloudbuild.yaml` path — example build step:

```yaml
steps:
  - name: gcr.io/cloud-builders/docker
    args:
      - build
      - -t
      - $_IMAGE
      - -f
      - PoultryFarmAPI/Dockerfile
      - PoultryFarmAPI
images:
  - $_IMAGE
```

(`$_IMAGE` / substitutions depend on how you configure the trigger; use the Console “substitution variables” your project expects.)

---

Cloud Run runs **one container per service**. You deploy:

| API | Folder | Cloud Run service name (example) |
|-----|--------|-----------------------------------|
| **Login / User Management** | `LoginAPI/` | `poultrymaster-login-api` |
| **Farm / production / flocks** | `PoultryFarmAPI/` | `poultrymaster-farm-api` |

Each service gets its **own HTTPS URL**. Your Next.js app (or mobile) calls **both** base URLs.

---

## 1. One-time GCP setup

```bash
gcloud config set project YOUR_PROJECT_ID
gcloud services enable run.googleapis.com artifactregistry.googleapis.com
gcloud artifacts repositories create poultrycore-apis \
  --repository-format=docker \
  --location=us-central1
gcloud auth configure-docker us-central1-docker.pkg.dev
```

Replace `us-central1` if you use another region.

---

## 2. Build & push **LoginAPI**

```bash
cd LoginAPI
docker build -t us-central1-docker.pkg.dev/YOUR_PROJECT_ID/poultrycore-apis/login-api:latest .
docker push us-central1-docker.pkg.dev/YOUR_PROJECT_ID/poultrycore-apis/login-api:latest
```

---

## 3. Build & push **PoultryFarmAPI**

```bash
cd ../PoultryFarmAPI
docker build -t us-central1-docker.pkg.dev/YOUR_PROJECT_ID/poultrycore-apis/farm-api:latest .
docker push us-central1-docker.pkg.dev/YOUR_PROJECT_ID/poultrycore-apis/farm-api:latest
```

---

## 4. Deploy **Login** service

```bash
gcloud run deploy poultrymaster-login-api \
  --image us-central1-docker.pkg.dev/YOUR_PROJECT_ID/poultrycore-apis/login-api:latest \
  --region us-central1 \
  --platform managed \
  --port 8080 \
  --allow-unauthenticated
```

Add secrets via **environment variables** (Console or CLI), e.g.:

- `ConnectionStrings__ConnStr` — SQL connection string  
- `JWT__Secret`, `JWT__ValidAudience`, `JWT__ValidIssuer`  
- `StripeSettings__PrivateKey`, `StripeSettings__PublicKey`, `StripeSettings__WHSecret`  
- Email settings if used  

Use **Secret Manager** for production instead of plain `--set-env-vars` when possible.

---

## 5. Deploy **Farm** service

```bash
gcloud run deploy poultrymaster-farm-api \
  --image us-central1-docker.pkg.dev/YOUR_PROJECT_ID/poultrycore-apis/farm-api:latest \
  --region us-central1 \
  --platform managed \
  --port 8080 \
  --allow-unauthenticated
```

Set at least:

- `ConnectionStrings__PoultryConn` — SQL connection string  
- `JWT__Secret` — **must match** what LoginAPI uses to sign tokens (same secret both APIs trust)  
- `JWT__ValidAudience`, `JWT__ValidIssuer` — must match your token validation config  

`ASPNETCORE_ENVIRONMENT=Production` is usually set automatically or add `--set-env-vars ASPNETCORE_ENVIRONMENT=Production`.

---

## 6. Wire the **frontend**

After deploy, Cloud Run shows URLs like:

- `https://poultrymaster-login-api-xxxxx-uc.a.run.app`
- `https://poultrymaster-farm-api-xxxxx-uc.a.run.app`

Point your Next.js env (e.g. `NEXT_PUBLIC_*` or server-side API base URLs) to:

- **Auth / employees / admin** → Login API URL  
- **Flocks, production, sales, health, etc.** → Farm API URL  

CORS: allow your web app origin on **both** APIs if the browser calls them directly.

---

## 7. SQL Server on the internet

Cloud Run has **no fixed outbound IP** on the default tier. Your SQL server must:

- Allow Cloud Run’s connections (often via **Cloud SQL** with connector, or **VPC connector** + static IP, or a host that allows broad SSL access — your security team should decide).

---

## Summary

- **Two images** → **two `docker build` / `push`** → **two `gcloud run deploy`** commands.  
- **Two URLs** in the client config.  
- **Same JWT signing secret** (and matching issuer/audience rules) on both APIs if Farm validates Login tokens.

More detail for LoginAPI only: `LoginAPI/CLOUD_RUN.md`.
