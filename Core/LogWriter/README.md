# LogWriter Microservice

This microservice listens for log events on RabbitMQ (topic exchange `log`, binding pattern `log.*` — updated 2026-07 from the old exact-match `log` key) and writes them to a SQL Server table and a local rolling log file via Serilog. Publishers (AIM, MenuGenerator, Email, UBIS_Web) each publish under a level-specific routing key — `log.information` / `log.warning` / `log.error` — instead of a single flat `log` key, so the queue binding uses a wildcard to keep receiving all of them.

Quick start:

1. Update `appsettings.json` `ConnectionStrings:DefaultConnection` and `RabbitMq` settings.
2. Restore and build:

```powershell
dotnet restore
dotnet build
```

3. Apply EF Core migrations (from this project folder):

```powershell
dotnet tool install --global dotnet-ef --version 8.0.0
dotnet ef migrations add InitialCreate --project . --startup-project . -o Persistence/Migrations
dotnet ef database update --project . --startup-project .
```

4. Run the microservice:

```powershell
dotnet run --project .
```

Endpoints:
- `POST /api/logs` — accept a JSON `LogEntry` object and persist it.
