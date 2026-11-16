# Probeta Control (C# / WPF)

The new Probeta Control: a WPF app on .NET Framework 4.8, delivered as one `ProbetaControl.exe`.

- [docs/porting-status.md](docs/porting-status.md): what is done and what is next
- [docs/decisions.md](docs/decisions.md): decisions and why
- [docs/conventions.md](docs/conventions.md): glossary, how screens are built, pitfalls, performance
- [docs/help-notes.md](docs/help-notes.md): built-in help topics and pending help updates

## Folders

| Folder | Content |
|---|---|
| `src/ProbetaControl` | The program (Data, Models, Services, ViewModels, Views, Themes, Help, Database/Scripts) |
| `tests/ProbetaControl.Tests` | NUnit tests; database tests run in throwaway `probeta_test_*` databases |
| `database` | `setup` (development MySQL accounts), `demo` (demo database and its public credentials) |
| `tools` | `DemoSeed` (fills the demo database), UI-automation and icon scripts |
| `demo-app` | Git-ignored copy of the program connected to the demo database |

## Development setup
- **Tools:** .NET SDK 10, VS Code with the C# extensions. F5 = "Probeta Control" (builds Debug, starts
  under the debugger); tasks for build, release build, clean and tests are in `.vscode/tasks.json`.
- **MySQL:** the project's own instance is the `MySQL2` service on port **3308** (on the second
  development PC: `MySQL84` on 3306, set in that PC's `test.env`).
- **Accounts:** run `dotnet/database/setup/create-dev-accounts.sql` once as root (replace the
  `CHANGE_ME_*` passwords first):

  | Account | Use |
  |---|---|
  | `specimen_app` | The app's connection (SELECT/INSERT/UPDATE on `probeta_control`) |
  | `specimen_admin` | Applying schema updates from the app's "Actualizar base de datos" (never saved) |
  | `specimen_test` | Tests: create/drop `probeta_test_*` databases |
  | `specimen_app_test` | Tests: the app's rights on `probeta_test_*` |

- **Tests:** copy `dotnet/tests/test.env.example` to `test.env` (git-ignored) and fill in the test
  passwords; `dotnet test dotnet/ProbetaControl.sln`. Each fixture runs in its own `probeta_test_*`
  database, which is dropped afterwards; leftovers are removed after a day.
- **Demo database:** `probeta_demo` with made-up sample data, for trying and showing the program. Set up with
  `database/demo/create-demo.sql` (root, once) and `dotnet run --project dotnet/tools/DemoSeed` (`--reset` to
  refill; dates are relative to that day). Accounts and passwords (public, demo only):
  [DEMO-CREDENTIALS.md](database/demo/DEMO-CREDENTIALS.md).
- **Logs:** `%LOCALAPPDATA%\ProbetaControl\logs`. Debug builds also log WPF binding errors there.
- **Self-test:** `ProbetaControl.exe --selftest` checks that the packed libraries load, including drawing and
  saving a PDF page, writing and reopening an Excel file and making a QR code (exit code 0 = OK).
- **Eclipse files:** `.vscode/settings.json` stops the Java extension from rewriting `.project`,
  `.classpath` and `.settings/`. If they show up as modified, restore them; don't commit them.

