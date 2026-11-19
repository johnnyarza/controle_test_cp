# Porting status: Probeta Control (Java → C# / WPF)

The legacy Java/JavaFX app (`legacy-java/`) is being rewritten as a WPF app on .NET Framework 4.8
in `dotnet/`, on branch `csharp-port`. The Java code stays untouched until the C# version reaches parity
and is signed off.

See also: [decisions](decisions.md) · [conventions, glossary and pitfalls](conventions.md) ·
[development setup](../README.md) · [help topics](help-notes.md).

_Last updated: 2026-11-18 (numbers with the PC's separators, Excel like the report, phase 9 photos: schema 014 and the desktop photo panel)._

## Status

| Phase | Content | State |
|---|---|---|
| 1 | Solution, single-exe packing (Costura), theme (`Themes/`), shell window, async MVVM base | Done |
| 2 | Numbered schema scripts 001–010 (011, material type, added in phase 4), `SchemaMigrator`, dev accounts, NUnit + throwaway databases | Done |
| 3 | Models, Dapper repositories (paging, audit, RowVersion, archive), services, business rules, login, DPAPI config, startup checks | Done |
| 4 | Screens, in six groups (review after each) | Done |
| 5 | PDF reports (PDFsharp), Excel export (ClosedXML), QR labels (QRCoder), in four groups | Done (awaiting review) |
| 6 | Built-in help (`Help/ayuda.html`), opened by "?" / F1. Topics: [help-notes.md](help-notes.md) | Done (awaiting review) |
| 7 | One-off import of the legacy `cp_db` data | Done (tool tested; the real import runs on switch day) |
| 8 | Inno Setup 7 installer (with .NET 4.8 offline installer) | To do |
| 9 | Phone page from the label's QR (ASP.NET Core): probeta data + photos, same users; photos also from the desktop probeta edit dialog ([decisions](decisions.md)) | Not in v1. Done: database (script 014) and the desktop photo panel in the probeta dialog |
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
   "Probetas para romper" warning after login (probetas due today, tomorrow, then overdue for at most
   30 days, grouped by ensayo; older overdue ones are one line with "Ver en Ensayos"; "Abrir ensayo";
   "No volver a mostrar hoy en este equipo" kept in `local.ini`). Its list is one flat virtualized list
   (headings + rows), so 2,000 probetas open in about 0.2 s.
   Configuración with a side menu: Administración (Usuarios: new / edit / reset password / archive;
   Logo y sello: PNG or JPG up to 2 MB, checked by its first bytes, stored in the DB), Este equipo
   (Conexión: shows the settings; "Cambiar conexión…" signs out and opens the startup connection form),
   Mi cuenta (Mi contraseña, Acerca de: versions, machine, log folder). Operators see only Este equipo and
   Mi cuenta. The placeholder page is gone. Admin-only rules (archive/restore everywhere, users, images)
   are enforced in the services; an administrator can't change their own role.

### Phase 5 groups

1. Done (awaiting review): report engine, preview window and the ensayo report. One MigraDoc layout is drawn three
   ways (`Services/Reports/ReportRenderer`): screen images for the preview, straight onto a Windows printer
   (print dialog, page range; picks the report's paper, shrinks to fit when the printer lacks it) and a PDF
   file. The preview (`ReportPreviewWindow`) needs no PDF viewer or PDF printer: Imprimir…, Guardar PDF…,
   zoom, "Ancho de página"; pages are drawn when scrolled into view, at most 12 kept in memory.
   Ensayo report (landscape, "Imprimir" / Ctrl+P on the ensayo page): header with logo, title, lab name,
   ensayo Nº, date, printed date, "Hoja X de Y"; test data with design fck, slump ± legacy tolerance and a/c;
   the mix (materials, providers, kg/m³, total); the probetas table (heading repeated on each page, "—" for
   unbroken); summary by age (count, minimum, average, % fck) beside the strength-vs-age chart; note and
   stamp at the end; lab address in the footer. It prints what the page's filter shows, never archived
   probetas. Configuración → "Datos del informe" (administrators): lab name, footer, note, paper A4 / Carta,
   stored in `AppSettings` (script 012, starting with the legacy text).
2. Done (awaiting review): report by client and the list reports. Every list page has a print button
   (`ListPageViewModel.SupportsPrint` / `Print`, Ctrl+P). Clientes, Proveedores, Materiales and Dosificaciones:
   "Imprimir" → "Listado de …" (portrait, `ListReports`) with everything the search and "Mostrar archivados"
   find, read again page by page, not only the rows on screen. Ensayos: "Informe por cliente…" → the client of
   the filter, or one chosen in "Buscar cliente" → dates of the ensayos to print (`ClientReportOptionsViewModel`,
   both optional and inclusive) → `ClientReport` (landscape): client data and period, then the active
   probetas of the client's active ensayos, oldest ensayo first, under a line per ensayo (Nº, date, project,
   fck) with the ensayo report's columns, then note and stamp. Both stop at `ReportService.MaxRows` (3,000
   rows, about 100 pages) and say so. The Dosificaciones list is a block per design (title line with fck, slump and a/c, then its
   materials table and total; layout A chosen by the user from two mockups), never split between pages.
3. Done (awaiting review): Excel export of an ensayo (what the legacy app exported). "Excel" (Ctrl+E) on the
   ensayo page asks where to save (same suggested name as the PDF) and writes an .xlsx with what the page's
   filter shows, never archived probetas (`TestExcelExport`, same data as the ensayo report). Since 2026-11-18
   one sheet "Ensayo" laid out like the report (asked by the user): logo, title, lab name, Nº and dates; the
   test's data beside the mix; the probetas as an Excel table "Probetas" (filter arrows, headings shown in place
   of the column letters when scrolling) with real numbers and dates (dd/mm/yyyy), plus "% fck"; probetas not
   broken yet leave load, strength and % fck empty (the legacy file had 0); summary by age with the chart as a
   picture (ClosedXML can't write Excel charts); note and stamp. Prints landscape on the report's paper, one
   page wide, table heading repeated, footer and "Hoja X de Y". A file that can't be written (open in Excel) is
   explained in a warning; "Archivo guardado" is a plain information dialog (`IDialogService.Inform`).
4. Done (awaiting review): QR labels, one per probeta; layout B chosen by the user from three mockups (QR on
   the left; big "Nº 345", a rule, "Ensayo 12", "Rotura 12/11/2026 · 7 d", the client cut to one line).
   "Etiquetas" (Ctrl+L) on the ensayo page labels the probetas the page shows (never archived ones; "Solo
   probetas sin romper" ticked by default); right-click "Imprimir etiqueta…" labels one probeta, broken or not.
   The dialog (`LabelOptionsViewModel`) draws the first sheet: click a label to start there on a used sheet.
   `SpecimenLabels` lays out one MigraDoc table per sheet with the labels' exact row and column sizes (gaps are
   empty rows / columns, no borders, so nothing drifts); fonts are fitted to the longest number and date line.
   Configuración → Etiquetas (administrators): server name and the sheet (six A4 presets or custom), with
   "Hoja de prueba" (label outlines as page-positioned frames, for plain paper) to check the measurements
   before saving. Script 013 adds `Specimens.PublicCode` (filled for existing probetas) and the two settings.

### Phase 6: built-in help

Done (awaiting review). Layout A chosen by the user: the program's own help window (contents on the left with a
search box, the topic on the right), not the PC's browser. `Help/ayuda.html` (Spanish, inside the exe) has 20
topics from help-notes.md plus the startup screens, keyboard shortcuts and common problems. "?" and F1 open it
at the topic of the screen: the page on screen (Configuración: its section), any dialog (a message box falls back
to the screen behind it), the report preview and every startup step (`ViewModels/HelpTopics.cs`).
`Views/Help/HelpViewer` shows it in the Windows web control (IE11, on every Windows, offline) on a UI thread of
its own, so it stays usable while a dialog is open; one window, moved to the new topic on each F1; zoomed to the
screen's scaling (also when moved to another screen). The search ignores accents and case, shows only the topics
with every word typed and marks them. Tests check the ids and links, that every screen has a topic, and run the
page's script in the real web control (IE11 mode, topics, search, layout).


### Phase 7: legacy data import (tool built and tested with the office dump of 28/02/2026)

Decided with the user on 2026-11-15:

- A command-line tool (`dotnet/tools/LegacyImport`, like DemoSeed) reads a copy of `cp_db` and writes through the
  program's services (audit rows, `PublicCode` for every probeta). It refuses a database that already has data and
  writes a report of what it imported and what it couldn't, with reasons.
- The source is a dump of the office server's `cp_db`, provided by the user. It holds real client data and the repo
  is public: it stays outside the repository and is loaded into a local scratch database, never the office server.
- Keep the legacy ensayo and probeta numbers, so printed reports and labels still match.
- Probetas the legacy app stored with load 0 ("not broken") are imported as not broken (`RuptureLoadTon` NULL),
  old ones included: they show as overdue until someone deals with them (option B; archiving them was declined).
- Users are not imported: the new database starts with `admin` only.
- Source: root is fine (the tool opens `cp_db` in a read-only session, so MySQL refuses any write); target:
  `specimen_admin` (needed for `ALTER TABLE … AUTO_INCREMENT`). Passwords are asked for, never saved.
- The next number of every table is set from `cp_db`'s own counter (`information_schema.TABLES.AUTO_INCREMENT`), so
  numbers of deleted legacy rows are never handed out again. Done before the data goes in (ALTER commits by itself);
  the data is one transaction, checked before the commit.
- More gaps found in the schema and handled the same way (all listed in the report): ensayo without provider → the
  client; without date → first mold date of its probetas (left out if none); probeta with neither rupture date nor
  age → left out; dosificación without fck → 0; repeated or missing material in a slot → row dropped.

How to run (switch day, on the office server; to test, point `--source-db` at a local copy of the dump):

```
dotnet run --project dotnet/tools/LegacyImport -- --check --source-db cp_db     (report only, nothing saved)
dotnet run --project dotnet/tools/LegacyImport -- --source-db cp_db             (the import)
```

Options: `--source-host/-port/-db/-user`, `--target-host/-port/-db/-user`, `--report file` (default `import-report.txt`).
Tests: `tests/ProbetaControl.Tests/Import` build a small legacy database with the awkward rows and import it end to end.

**For phase 8:** the office server needs the new database's MySQL accounts (`specimen_app`, `specimen_admin`) created with
real passwords: a setup script for production (today only `database/setup/create-dev-accounts.sql` exists).

Test run with the office dump of 28/02/2026 (2026-11-15): everything imports (139 clients, 28 providers, 46 materials,
90 dosificaciones, 396 ensayos, 2,190 probetas), next numbers 402 (ensayos) and 2297 (probetas), in about 8 s. Left
for the switch day: 2 materials typed "Otro" (Paver, "0") and 14 dosificaciones without materials (all at 0 kg/m³ in
the legacy data) to fix by hand afterwards. The real import runs on the office server with the latest data.
