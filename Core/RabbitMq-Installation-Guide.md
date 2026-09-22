# RabbitMQ Installation Guide (Windows 11 Pro — Developer Desktops)

This guide is for developers setting up RabbitMQ **for the first time** on a Windows 11 Pro
desktop, in order to run the UBIS 2.0 Core microservices locally:

- **AIM** (publishes OTP send requests + log events)
- **Email** (consumes OTP send requests, publishes log events)
- **LogWriter** (consumes log events from both AIM and Email)

All three talk to each other **only through RabbitMQ** — there is no direct HTTP call between
them. If RabbitMQ isn't running locally, AIM/Email will still start, but OTP emails and centralized
logging silently fall back to local log files (see each service's `RabbitMqLogWriterClient`/
`RabbitMqOtpEmailConsumer` for that fallback behavior) instead of actually flowing between services.

RabbitMQ on Windows requires **Erlang** to be installed first — RabbitMQ itself is an Erlang
application. Install Erlang, then RabbitMQ, in that order.

---

## 1. Install Erlang/OTP

RabbitMQ 4.3.x (current stable series) requires **Erlang/OTP 26.2 or newer**, with **27.x**
being the recommended, fully-supported series. Do not install Erlang/OTP 28 or 29 — RabbitMQ
either doesn't support them yet or only partially supports them for brand-new clusters.

1. Go to the official Erlang download page: **https://www.erlang.org/downloads#prebuilt**
2. Under the Windows section, download the **64-bit** installer for the latest **27.x** release
   (e.g. `otp_win64_27.x.exe`).
3. Right-click the installer → **Run as administrator**.
4. Accept the defaults through the wizard. The installer normally sets the `ERLANG_HOME`
   environment variable for you.
5. Open a **new** Command Prompt (must be new, so it picks up the updated environment) and verify:
   ```powershell
   erl -version
   echo %ERLANG_HOME%
   ```
   You should see an Erlang version string and a path like `C:\Program Files\Erlang OTP-27.x`.

> **Only one Erlang version can be installed at a time.** If you already have an older Erlang
> installed for another purpose, uninstall it first, or RabbitMQ may fail to start.

---

## 2. Install RabbitMQ Server

1. Go to **https://github.com/rabbitmq/rabbitmq-server/releases**
2. Under the latest release's **Assets**, download the Windows installer —
   `rabbitmq-server-<version>.exe` (e.g. `rabbitmq-server-4.3.2.exe`).
3. Right-click → **Run as administrator**.
4. Keep the default install location. If you do change it, avoid spaces or non-ASCII characters
   in the path — the installer/service can fail to start otherwise.
5. Finish the wizard. The installer:
   - Installs RabbitMQ as a Windows service named **RabbitMQ**.
   - Starts that service automatically.

### Verify it's running

Open **Services** (`services.msc`) and confirm **RabbitMQ** shows status **Running**, startup
type **Automatic**. Alternatively, from an elevated Command Prompt:

```powershell
sc query RabbitMQ
```

If you ever change an environment variable that RabbitMQ/Erlang depends on (e.g. `ERLANG_HOME`),
you must **reinstall the Windows service**, not just restart it — a plain restart won't pick up
the change. From the RabbitMQ `sbin` folder (see below) run:

```powershell
rabbitmq-service.bat remove
rabbitmq-service.bat install
rabbitmq-service.bat start
```

---

## 3. Enable the Management UI (first-time setup)

The management plugin gives you a web dashboard to see queues, exchanges, and messages flowing
between AIM, Email, and LogWriter — very useful the first time you're testing this locally.

1. Open an elevated Command Prompt.
2. `cd` into the RabbitMQ `sbin` folder. The exact path includes the version number, e.g.:
   ```powershell
   cd "C:\Program Files\RabbitMQ Server\rabbitmq_server-4.3.2\sbin"
   ```
3. Enable the plugin:
   ```powershell
   rabbitmq-plugins enable rabbitmq_management
   ```
4. Open a browser to **http://localhost:15672/**
5. Log in with the default credentials: **guest / guest**

> **Important:** the `guest` account only works when connecting from **localhost** — this is a
> RabbitMQ security default (`loopback_users`), not a bug. It's exactly why every service's
> `appsettings.json` in this repo defaults `RabbitMq:HostName` to `localhost` for local dev. Don't
> try to loosen this for convenience; if you need remote access for some reason, create a proper
> non-guest user instead (see §5).

### What you should see once services have run at least once

- **Exchanges** tab → `email` (topic exchange, declared durable by AIM's OTP publisher and Email's
  consumer — updated 2026-07 from a plain default-exchange queue) and `log` (topic exchange,
  declared durable by AIM/Email/MenuGenerator/UBIS_Web's log publishers and by LogWriter's listener)
- **Queues** tab → a durable queue bound to the `email` exchange with pattern `email.otp.#` (OTP
  requests publish under routing key `email.otp.send`; future email types can share the same
  exchange/queue under `email.notification.send` etc. without a new binding), and LogWriter's own
  queue bound to the `log` exchange with pattern `log.*` (publishers now send `log.information` /
  `log.warning` / `log.error` instead of one flat `log` key)

If you don't see these yet, that's fine — they get declared the first time any of the three
services actually connects and calls `QueueDeclareAsync`/`ExchangeDeclareAsync`. Start one of the
services and refresh.

---

## 4. Where to update the connection settings

Every service's RabbitMQ configuration lives under a `RabbitMq` section in its `appsettings.json`
(base/dev defaults) and `appsettings.Production.json` (placeholders to fill in for a real
deployment). For local desktop development, **the checked-in defaults already point at
`localhost` with the `guest`/`guest` account** — you normally don't need to change anything to get
started.

| Service | Settings file | Keys |
|---|---|---|
| **AIM** | `Core/AIM/WebApi/appsettings.json` (and `.Development.json`) | `RabbitMq:HostName`, `Port`, `UserName`, `Password`, `EmailExchange`, `OtpRoutingKey`, `NotificationRoutingKey`, `LogExchange` |
| **AIM (prod)** | `Core/AIM/WebApi/appsettings.Production.json` | Same keys — replace `your-prod-rabbitmq-server` and the password placeholder |
| **Email** | `Core/Email/WebApi/appsettings.json` (and `.Development.json`) | `RabbitMq:HostName`, `Port`, `UserName`, `Password`, `EmailExchange`, `OtpBindingPattern`, `LogExchange` |
| **Email (prod)** | `Core/Email/WebApi/appsettings.Production.json` | Same keys — replace the placeholders |
| **LogWriter** | `Core/LogWriter/appsettings.json` | `RabbitMq:HostName`, `UserName`, `Password`, `Exchange`, `RoutingKeyPattern` (default `log.*`) |

> **Naming note:** LogWriter's own config calls the log pipe's settings `Exchange`/`RoutingKeyPattern`
> (it's the one *listening*, using a wildcard binding), while AIM's and Email's config call the
> exchange `LogExchange` and publish a level-specific key (`log.information`/`log.warning`/
> `log.error`) rather than a single fixed routing key. Different key names, but the exchange name
> must match across all publishers and LogWriter (`log` by default) or messages won't be delivered.
> Similarly, AIM's `EmailExchange`/Email's `EmailExchange` must both point at the same `email`
> topic exchange, with Email's consumer bound via `OtpBindingPattern` (`email.otp.#`) so it keeps
> receiving whatever routing key AIM's publisher uses.

If you install RabbitMQ on a **different machine** than the one running the .NET services (e.g. a
shared dev VM), update `RabbitMq:HostName` in the relevant `appsettings.Development.json` to that
machine's hostname or IP, and make sure Windows Firewall on the RabbitMQ machine allows inbound
TCP on **5672** (AMQP) and, if you want the dashboard, **15672** (management UI) from your
network. Since each microservice is hosted on IIS separately, production `HostName` values should
point at whichever server actually hosts the shared RabbitMQ broker for that environment.

---

## 5. (Optional but recommended) Create a dedicated application user

`guest`/`guest` is fine for a single-developer localhost setup, but don't reuse it once you're
past that — including for the `Production` appsettings placeholders already in this repo
(`aim_service`, `email_service`). To create one:

```powershell
cd "C:\Program Files\RabbitMQ Server\rabbitmq_server-4.3.2\sbin"
rabbitmqctl add_user aim_service "SomeStrongPassword"
rabbitmqctl set_permissions -p / aim_service ".*" ".*" ".*"
rabbitmqctl set_user_tags aim_service management
```

Repeat with a different username/password for the Email service. Then update the corresponding
`UserName`/`Password` in that service's `appsettings.Development.json` or
`appsettings.Production.json`.

---

## 6. Quick smoke test

1. Make sure the **RabbitMQ** Windows service is running (§2) and the management UI is reachable
   at `http://localhost:15672` (§3).
2. Run `Email.WebApi` (`dotnet run` from `Core/Email/WebApi`, or via IIS Express/Visual Studio).
   Its console/log output should say the OTP email consumer is listening on the `email.otp.send.queue`
   queue, bound to the `email` exchange with pattern `email.otp.#`.
3. Run `AIM.WebApi` the same way.
4. In the management UI, open **Exchanges → email** and **Exchanges → log** — both should now
   exist, along with their bound queues, with 0 messages (until an OTP is actually generated or a
   log event fires).
5. Trigger a login attempt against AIM's `/api/auth/login` — you should see a message briefly
   pass through the `email` exchange under routing key `email.otp.send`, and log events land on
   LogWriter under keys like `log.information`/`log.error` (check
   `Core/LogWriter/logs/logwriter-*.txt` or its `Logs` table if RabbitMQ delivery succeeded).

---

## 7. Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| `rabbitmq-plugins`/`rabbitmqctl` not recognized | You're not in the `sbin` folder, or not using an elevated prompt. |
| Windows service won't start after install | Erlang not installed, wrong bitness, or more than one Erlang version present — uninstall extras and reinstall RabbitMQ's service (`rabbitmq-service.bat remove` / `install` / `start`). |
| Changed `ERLANG_HOME` but nothing changed | You restarted instead of reinstalling the service — see the note at the end of §2. |
| App logs show "Failed to publish log entry to LogWriter via RabbitMQ" / OTP not received | RabbitMQ service isn't running, or `RabbitMq:HostName`/port in `appsettings.json` doesn't match where RabbitMQ is actually listening. Both AIM's and Email's clients fall back to local logging on publish failure rather than crashing — check the local log file first. |
| `ACCESS_REFUSED` connecting as `guest` from another machine | Expected — `guest` is localhost-only by default. Create a dedicated user (§5) instead. |
| Can't reach `http://localhost:15672` | Management plugin not enabled (§3), or a firewall is blocking port 15672. |

---

## Reference

- Erlang downloads: https://www.erlang.org/downloads#prebuilt
- Erlang/RabbitMQ compatibility matrix: https://www.rabbitmq.com/docs/which-erlang
- RabbitMQ Windows install docs: https://www.rabbitmq.com/docs/install-windows
- RabbitMQ Windows installer releases: https://github.com/rabbitmq/rabbitmq-server/releases
- Management plugin docs: https://www.rabbitmq.com/docs/management
