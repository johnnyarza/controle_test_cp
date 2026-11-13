# Demo database: accounts and passwords

`probeta_demo` is a database with made-up sample data, for trying the program and showing it.
It is not the development database (`probeta_control`) and not the test databases (`probeta_test_*`).

> **These passwords are public** (this repository is public). The MySQL accounts only work from the
> same PC (`@'localhost'`) and only on `probeta_demo`. Don't reuse any of them for real data.

## Program users (login screen)

| User | Password | Role | Notes |
|---|---|---|---|
| `admin` | `demo-admin-1` | Administrador | Everything, including Configuración and archiving |
| `msilva` | `demo-oper-1` | Operador | Mario Silva |
| `lrojas` | `demo-oper-1` | Operador | Lucía Rojas |
| `jperez` | `demo-oper-1` | Operador, **archived** | Can't log in (shows "Mostrar archivados" in Usuarios) |

## MySQL accounts

| Account | Password | Rights | Used for |
|---|---|---|---|
| `demo_app` | `Demo-App-2026` | SELECT, INSERT, UPDATE on `probeta_demo` | The program's connection |
| `demo_admin` | `Demo-Admin-2026` | ALL on `probeta_demo` | "Actualizar base de datos" and `DemoSeed --reset` |

## Setting it up on a PC

1. Once, as root: `dotnet/database/demo/create-demo.sql` (see the command at the top of the file;
   port 3306 or 3308 depending on the PC).
2. Build and fill it: `dotnet run --project dotnet/tools/DemoSeed`. It applies the schema scripts, creates
   the users above and the sample data, and puts a program copy connected to the demo in `dotnet/demo-app/`
   (git-ignored; the connection file is encrypted for that PC).
3. Run `dotnet/demo-app/ProbetaControl.exe` and log in with a user above.

The dates are relative to the day it was filled (probetas due today, tomorrow, overdue…). To refresh them,
or to start over after trying things: `dotnet run --project dotnet/tools/DemoSeed -- --reset`.

Options: `--host`, `--port` (default: the first server in `dotnet/tests/test.env`, else `localhost:3306`),
`--app-folder <folder>`.
