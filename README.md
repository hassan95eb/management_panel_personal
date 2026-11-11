# Command Center

The MVP-02 scaffold uses React, TypeScript, and Tailwind in `web/` and ASP.NET Core 10 in `api/`. The home page displays the owner sign-in form and session status.

## Run locally with Docker Compose

Start Docker Desktop or Docker Engine with the Compose plugin. From the repository root, run:

```sh
docker compose up --build -d
```

Open http://localhost:5173 and sign in with the local-only default password
`LocalPanelOwner-ChangeMe`. To override it, set `PANEL_OWNER_PASSWORD` in a
root `.env` file before starting Compose. Use at least 16 characters.

Compose starts PostgreSQL, waits for it to accept connections, and applies EF Core
migrations before the API starts serving requests. Check
http://localhost:5173/api/health for `{"status":"ok"}`. If startup fails, run
`docker compose logs db api web` to see the service output.

Use `docker compose down` to stop the stack. PostgreSQL data persists in a named
volume.

## Run locally without Docker

Prerequisites are Node.js 24 and the .NET 10 SDK.

From the repository root, start the API in a Bash terminal.

```sh
read -rsp 'Owner password (at least 16 characters): ' Owner__Password
printf '\n'
export Owner__Password
ASPNETCORE_ENVIRONMENT=Development dotnet run --project api/ManagementPanel.Api.csproj -- --urls http://127.0.0.1:5080
```

In a second terminal, start the frontend.

```sh
cd web
npm ci
npm run dev
```

The frontend runs at `http://localhost:5173` by default. Vite proxies `/api` requests to port 5080. The `/api/health` endpoint returns `{"status":"ok"}`.

## Owner account and sessions

Generate a unique, random password of at least 16 characters for the single owner account. For deployment, set it as the `Owner__Password` secret on Render and sign in from the frontend's HTTPS URL. The password is sent only to the API. The session cookie is valid for 12 hours. Signing out removes the cookie from the current browser. Other browser sessions are independent. Restarting an environment without persistent Data Protection keys can invalidate sessions.

API routes require the owner role by default, except for health and sign-in. Sign-in and sign-out requests require the `X-Panel-Request: 1` header. Each API instance allows up to 10 sign-in attempts per minute.

## PostgreSQL and migrations

Create an empty `management_panel` database in local PostgreSQL or Neon. Set the Npgsql connection string in the `ConnectionStrings__Panel` environment variable. In Bash, enter the full value at a hidden prompt.

```sh
read -rsp 'PostgreSQL connection string: ' ConnectionStrings__Panel
printf '\n'
export ConnectionStrings__Panel
dotnet tool restore
dotnet ef database update --project api
```

The local connection string format is `Host=localhost;Port=5432;Database=management_panel;Username=<user>;Password=<password>`. For Neon, use the endpoint and TLS settings from the Neon dashboard. Set the actual connection string as an API environment secret on Render. Run migrations from an environment with authorized access to the database.

The initial migration creates the `areas` and `projects` tables and seeds the `projects` area. The following query should return one row.

```sql
SELECT "Key", "Name" FROM areas WHERE "Key" = 'projects';
```

The `/api/health` endpoint reports HTTP service availability and works without a database connection.

## Build and CI

```sh
dotnet build api/ManagementPanel.Api.csproj --configuration Release
cd web
npm ci
npm run build
```

The `.github/workflows/ci.yml` workflow runs both builds on pushes and pull requests.

## Test deployment

1. Create the Render Docker service using `render.yaml` or equivalent settings. Set the root directory to `api`, the port to 10000, and the health check path to `/api/health`.
2. Replace the example Render URL in the rewrite destination in `web/vercel.json` with your service URL.
3. Import the repository into Vercel with `web` as the root directory, Vite as the framework preset, and Node.js 24. The build command is `npm run build` and the output directory is `dist`.
4. Open the Vercel URL and check the home page and `/api/health`. The API should return JSON with `Cache-Control: no-store`.

Apply the migration to the target database before using features that depend on stored data.