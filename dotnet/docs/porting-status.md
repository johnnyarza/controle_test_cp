# Porting status: Probeta Control (Java → C# / WPF)

The legacy Java/JavaFX app (repo root, `src/`) is being rewritten as a WPF app on .NET Framework 4.8
in `dotnet/`, on branch `csharp-port`. The Java code stays untouched until the C# version reaches parity
and is signed off.

_Last updated: 2026-11-11 (end of phase 4)._

## Status

| Phase | Content | State |
|---|---|---|
| 1 | Solution, single-exe packing (Costura), theme (`Themes/`), shell window, async MVVM base | Done |
| 2 | Numbered schema scripts 001–010 (011, material type, added in phase 4), `SchemaMigrator`, dev accounts, NUnit + throwaway databases | Done |
| 3 | Models, Dapper repositories (paging, audit, RowVersion, archive), services, business rules, login, DPAPI config, startup checks | Done |
| 4 | Screens, in six groups (review after each) | Done |
| 5 | PDF reports (PDFsharp), Excel export (ClosedXML), QR labels (QRCoder) | **Next** |
| 6 | Built-in help (`Help/ayuda.html`), opened by "?" / F1. Topics: [help-notes.md](help-notes.md) | To do |
| 7 | One-off import of the legacy `cp_db` data | To do |
| 8 | Inno Setup 7 installer (with .NET 4.8 offline installer) | To do |
| 9 | Read-only phone page (ASP.NET Core) | Not in v1 |
| 10 | Release check (tests, 100–150 % scaling), retire the Java code | To do |

### Phase 4 groups

1. Done: startup checks, connection settings, database update, login, forced password change, logout.
2. Done: Clientes, Proveedores, Materiales (list pages + edit dialogs).
3. Done: Dosificaciones: list + editor with fck, slump (optional) and a material table (add / remove /
   reorder rows, up to 8, total kg/m³, water/cement ratio from the material types), and the percentage
   calculator (target quantity = origin quantity × % / 100, previewed before applying). Designs used by
   ensayos show a warning and only an administrator can change them; "Duplicar" makes an editable
   copy. The list shows the selected design's materials in a resizable panel below it. Also added in
   this group: material type (Cemento / Agua / Agregado / Aditivo / Otro) and the "in use" rule for
   archiving.
4. Done: Ensayos (start page): newest first, "Para romper" badge (untested specimens due today or
   earlier), search, client filter, "Solo con probetas para romper (N)"; new / edit test header form
   (date field with calendar, client and concrete provider via the client search dialog, "Igual que el
   cliente", design, project, address); "Nuevo cliente" inside the client search. Locked tests open
   read-only for operators.
5. Done: Ensayo detail page (Abrir / double-click; "Nuevo" opens the new ensayo at once; "← Ensayos"
   returns to the list as it was). Layout chosen by the user from mockups: title with lock chip and
   "Editar datos" / "Desbloquear…", header on one line, indicator tiles (probetas, para romper, rotas,
   average strength + % fck), full-width probetas table (red = overdue, yellow = due today/tomorrow,
   Estado and % fck columns). Probeta form in sections (Moldeo + Edad = Rotura, Hoy, average of two
   readings, live density / strength / % fck, pre-filled from the newest probeta); "Registrar rotura"
   quick dialog with "Guardar y siguiente" (also double-click / Ctrl+R on an unbroken probeta).
   Copiar, Archivar, filter by mold dates and numbers (`1-5, 8`), Desbloquear / Bloquear.
6. Done: layouts chosen by the user from mockups (A for Configuración, D for the warning).
   "Probetas para romper" warning after login (probetas due today or tomorrow plus overdue ones, grouped
   by ensayo, "Abrir ensayo", "No volver a mostrar hoy en este equipo" kept in `local.ini`).
   Configuración with a side menu: Administración (Usuarios: new / edit / reset password / archive;
   Logo y sello: PNG or JPG up to 2 MB, checked by its first bytes, stored in the DB), Este equipo
   (Conexión: shows the settings; "Cambiar conexión…" signs out and opens the startup connection form),
   Mi cuenta (Mi contraseña, Acerca de: versions, machine, log folder). Operators see only Este equipo and
   Mi cuenta. The placeholder page is gone. Admin-only rules (archive/restore everywhere, users, images)
   are enforced in the services; an administrator can't change their own role.

## Decisions

- One `ProbetaControl.exe` (WPF, net48, Costura.Fody); config `ProbetaControl.config` next to the exe,
  encrypted with DPAPI for the PC.
- MySQL schema `probeta_control`, utf8mb4 / utf8mb4_0900_ai_ci, SQL limited to what MySQL 8.0.20 supports.
  The startup check refuses servers older than 8.0.20 (`StartupChecker.MinimumServerVersion`).
- The app's MySQL account has SELECT, INSERT, UPDATE only. Nothing is deleted: records are archived.
  The connection is refused if the account could DELETE/DROP/ALTER/CREATE (e.g. `root`).
- Every data change writes an `AuditLog` row in the same transaction.
- Login required; users in the database with BCrypt passwords. A new database starts with
  `admin` / `admin`, which must be changed at first login. No hint about it on the login screen.
- Roles: `admin`, `operator`. Archive/restore and user administration are admin-only; operators create
  and edit. On a locked test an operator may only record a pending rupture load. The admin-only rules
  are checked in the services (`AppSession.RequireAdmin`), not only by disabled buttons.
- Large tables: all database access is async, lists are searched and paged on the server
  (100 rows at a time), long work shows a cancellable busy overlay.
- A record can't be archived while active records use it (client → ensayos, provider → materials,
  material → dosificaciones, dosificación → ensayos); the user gets a warning listing them.
  Checked in the services, so every screen gets it.
- A dosificación used by any ensayo (archived ones included) can be changed only by an administrator.
- Quantities stay in kg/m³ for every material (no litres or % units).
- Logo and stamp for reports are stored in the database (same on every PC).
- Legacy data will be migrated (phase 7). Excel export is kept. One QR label per specimen.
- Screen text in Spanish; code, tables and stored values in English. Dates `dd/MM/yyyy`, invariant culture.
  Number fields accept a comma or a point as decimal separator (`NumberInput`); lists show a point.
  Dates are typed day first (`DateInput`: `05/11/2026`, `5-11-26`, `5.11.2026`) in the `DateBox` field,
  never with the PC's regional settings; the stock DatePicker is not used for that reason.
- Tests are locked: an operator changes only the tests they created in this session (or that an
  administrator unlocks with "Desbloquear"); administrators change any test (`AppSession.IsTestUnlocked`).
  Archiving probetas is for administrators, like every other archive.
- Every window must handle resizing (minimums fit about 900 × 500).

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
- **Logs:** `%LOCALAPPDATA%\ProbetaControl\logs`. Debug builds also log WPF binding errors there.
- **Self-test:** `ProbetaControl.exe --selftest` checks that the packed libraries load (exit code 0 = OK).
- **Eclipse files:** `.vscode/settings.json` stops the Java extension from rewriting `.project`,
  `.classpath` and `.settings/`. If they show up as modified, restore them; don't commit them.

## How screens are built

- List page: view model derives from `ListPageViewModel<T>`; the view puts its `DataGrid` inside
  `<ContentControl Style="{StaticResource ListPageFrame}">`. Register it in `ShellViewModel` and map it
  in `ShellWindow.xaml`.
- Dialog: view model derives from `EditorDialogViewModel` (`InitializeAsync` loads fresh data,
  `SaveCoreAsync` throws `ValidationException` with Spanish field messages); template in
  `Views/Dialogs/DialogTemplates.xaml`; opened through `IDialogService`. Tests answer dialogs with
  `ScriptedDialogs`.
- Forms: field errors via `Errors[Field]` and the `FieldError` style; banner via `MessageBanner`.
  Name the bound property like the error key (e.g. `Fck` as text) so the field also gets the red border.
  Numbers are typed as text and parsed with `NumberInput`. Wider dialogs override `PreferredWidth`.
- Buttons: text as content plus `b:ButtonIcon.Icon`; icon-only buttons need
  `AutomationProperties.Name` and a tooltip. Menu items take the same `b:ButtonIcon.Icon`.
- List rows: `RowStyle="{StaticResource ListPageDataGridRow}"` gives archived rows in grey and the
  right-click menu (Editar / Archivar or Restaurar), bound to the page's commands through the row's `Tag`.
- Colours only through the `Brush.*` keys of `Themes/Colors.xaml`.
- File dialogs go through `IDialogService.ChooseFile` (tests answer with `ScriptedDialogs.AnswerFile`).
- Configuración sections derive from `SettingsSection` (an `EditorViewModel`: banner, field errors,
  `LoadAsync` each time the section is shown); add one to `SettingsPageViewModel` and give it a
  `DataTemplate` in `Views/Settings/SettingsView.xaml`.

### Pitfalls already hit

- Templates inside a merged resource dictionary can't see keys declared directly in `App.xaml`: put
  shared resources (e.g. converters) in their own dictionary merged earlier.
- Attached properties in theme templates: use `{TemplateBinding b:Prop}`.
- Don't put `Padding` on a text box's `PART_ContentHost` (WPF applies it already).
- Items and dialogs are announced by `ToString()`: override it to return the visible title.
- A build failing with "BG1002 … .baml cannot be found": run the "clean" task.
- Per-row combo box lists (e.g. materials not used on other rows): edit the row's collection in place;
  replacing `ItemsSource` can make the combo push a null selection back to the view model.
- Error tooltips bind `(Validation.Errors)/ErrorContent`, not `[0]` (logs a binding error when errors clear).
- MySQL: `MySqlErrorCode` has no value for 3819 (check constraint), so compare `ex.Number`; use
  `INSERT … AS alias ON DUPLICATE KEY UPDATE` instead of `VALUES()`.
- Windows PowerShell 5.1 reads a `.ps1` without a BOM as ANSI: save UI-check scripts with Spanish text
  ("Iniciar sesión") as UTF-8 with BOM.
- A left-aligned panel with only `MaxWidth` shrinks to its widest label; give forms a `Width`.
- Build fails with a locked `*_wpftmp.csproj.nuget.g.props`: delete `bin`/`obj` and build with `-m:1`.

## Checking screens

After each group the real exe is driven with Windows UI Automation (`dotnet/tools/ui-automation.ps1`)
against a throwaway `probeta_test_*` database seeded with sample data. Screenshots are taken at normal
size and at the minimum window size, and the log must show no binding errors.
