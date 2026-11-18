# Deploying the live demo to Azure

The live demo runs on Azure's free tiers:

| Part | Azure service | Tier |
|---|---|---|
| API **and** the Angular site | App Service (Linux, .NET 10) | Free F1 |
| Database | Azure SQL Database | Free offer (serverless) |

One App Service serves both: the deploy workflow copies the Angular build into the API's `wwwroot`,
and the API serves it (`Program.cs`). The browser talks to a single origin, so no CORS is needed –
the same idea as the nginx proxy in `docker-compose.yml`.

[`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml) tests, builds and deploys after
every push to `main`.

## What visitors can and cannot do

The demo runs with `Demo__Enabled=true` (see `DemoOptions`):

- The published accounts (`…@school.demo`) cannot get a new password, be deactivated or deleted, or
  change their email or role – otherwise one visitor could lock everyone else out.
- Every start of the app clears the data and seeds a fresh school. On the free tier the app is
  stopped after about 20 idle minutes, so visitors' changes disappear and the demo dates
  ("this week", "the last 30 days") always stay around today.

## Free-tier limits to know

- **Cold start.** After an idle period the first request wakes the app and, if it was paused, the
  database, and the demo data is seeded again: the first page can take 30–60 seconds.
  Then it is fast.
- **App Service F1** allows 60 CPU minutes a day – plenty for a demo.
- **Azure SQL free offer** includes 100,000 vCore seconds and 32 GB a month. Choose
  "auto-pause until next month" when the limit is reached, so it never costs money.

## One-time setup (Azure portal)

You need an Azure account (https://azure.microsoft.com/free). Use the same region for everything,
for example *West Europe* or *Israel Central*.

### 1. Database

1. **Create a resource → SQL Database.** On the top of the page click **Apply offer** (the free offer).
2. Resource group: create `school-demo`. Database name: `SchoolManagement`.
3. Server: **Create new**. Authentication: **Use SQL authentication**. Choose an admin login and a
   strong password and keep them for step 3.
4. "Behavior when free limit reached": **Auto-pause the database until next month**.
5. **Networking** tab: Connectivity method **Public endpoint**, and
   **Allow Azure services and resources to access this server: Yes**.
6. Create. When it is ready, open the database → **Connection strings** → **ADO.NET (SQL
   authentication)** and copy the string. Replace `{your_password}` with the password from step 3.

### 2. App Service

1. **Create a resource → Web App.** Resource group `school-demo`.
2. Name: for example `school-management-demo` (it becomes part of the address).
   Publish: **Code**. Runtime stack: **.NET 10 (LTS)**. Operating system: **Linux**.
3. Pricing plan: create a new Linux plan and choose **Free F1**.
4. Create. Then, in the new app:
   - **Settings → Configuration → General settings**: turn **SCM Basic Auth Publishing Credentials**
     **On** (needed for the publish profile), set **HTTPS Only** to **On**, and set
     **Startup command** to `dotnet SchoolManagement.Api.dll`. Save.

### 3. App settings

In the app: **Settings → Environment variables → App settings**, add:

| Name | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | the connection string from step 1 |
| `Jwt__SigningKey` | a long random secret – open **Cloud Shell** (the `>_` icon at the top of the portal) and run `openssl rand -base64 48` |
| `Database__InitializeOnStartup` | `true` |
| `Demo__Enabled` | `true` |
| `Swagger__Enabled` | `true` |
| `FileStorage__RootPath` | `/home/data/uploads` |

`/home` is the persistent, writable storage of App Service (the deployed code folder is read-only).
These values live only in Azure – never commit them.

### 4. Connect GitHub

1. In the app's **Overview**, click **Download publish profile**.
2. In GitHub: **Settings → Secrets and variables → Actions**.
   - **Secrets** tab → New repository secret: `AZURE_WEBAPP_PUBLISH_PROFILE`, value = the whole
     content of the downloaded file.
   - **Variables** tab → New repository variable: `AZURE_WEBAPP_NAME`, value = the app name from step 2.
3. **Actions → Deploy → Run workflow** (or push to `main`).

The address is shown on the app's **Overview** page ("Default domain") and in the finished workflow run.
Open it and log in with a demo account (the login page can fill it in).

## When something goes wrong

- **The site does not start**: the app's **Log stream** shows the startup log. A missing
  `Jwt__SigningKey` or connection string stops the app on purpose, with a clear message.
- **"Cannot open server … requested by the login"**: the firewall setting "Allow Azure services"
  (step 1.5) is off, or the password in the connection string is wrong.
- **The deploy step fails with 401**: SCM Basic Auth Publishing Credentials is off (step 2.4), or the
  publish profile was downloaded before it was turned on – download it again and update the secret.
