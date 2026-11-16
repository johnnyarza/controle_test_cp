# Decisions

Decisions taken while porting Probeta Control to C# / WPF, and why. Status: [porting-status.md](porting-status.md).

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
- Reports open in the program's own preview window (some PCs are old and may have no PDF viewer or PDF
  printer); printing goes straight to a Windows printer, "Guardar PDF…" saves a file to e-mail.
- **PDFsharp / MigraDoc stay on 6.2.x (MIT).** From 6.5.0 they are AGPL or commercial and check a license at
  runtime ("The loaded license is not valid"). Don't let an update move them to 6.5 or later.
- **Excel files are written with ClosedXML 0.105.1 (MIT).** Everything it brings is MIT, except
  SixLabors.Fonts 1.0.0 (Apache-2.0); check the licence of SixLabors.Fonts before letting an update move it
  to a newer major version. It grows the exe from about 2.6 MB to 5.5 MB.
- Lab name, footer, note and paper size are edited in Configuración and stored in the database (like the
  logo and stamp). The legacy reports were A4; "Carta" is a setting because printers here often default to it.
- QR labels: the QR holds a link to a future read-only page on the lab's server (phase 9), used by lab staff
  on the lab Wi-Fi, so the address uses the server's name, not its IP (an IP is refused, and so is
  `localhost`). The link is `http://SERVER/p/CODE`, written all in capitals in the QR (alphanumeric mode keeps
  it at 29 × 29 modules with 25 % error correction); **the phase 9 page must answer `/p/{code}` ignoring case.**
  Each probeta gets a random code (`Specimens.PublicCode`, 16 hex digits = 64 bits, unique key), never its
  number, so other results can't be guessed. The repository makes it on every insert (a copy gets its own);
  it never changes. Until an administrator sets the server name, labels can't be printed (a wrong address on
  a stuck label can't be fixed).
- Label paper is always A4 adhesive sheets, whatever the report paper is; the sheet (presets or custom grid,
  margins and gaps in mm) is a setting shared by every PC. Labels print at real size through the same
  preview / printer / PDF path as the reports.
- **QR codes are made with QRCoder 1.6.0 (MIT, no dependencies).** The tests read them back with ZXing.Net
  0.16.9 (Apache-2.0), which is only in the test project, never in the exe.
- Screen text in Spanish; code, tables and stored values in English. Dates `dd/MM/yyyy`, invariant culture.
  Number fields accept a comma or a point as decimal separator (`NumberInput`); lists show a point.
  Dates are typed day first (`DateInput`: `05/11/2026`, `5-11-26`, `5.11.2026`) in the `DateBox` field,
  never with the PC's regional settings; the stock DatePicker is not used for that reason.
- Tests are locked: an operator changes only the tests they created in this session (or that an
  administrator unlocks with "Desbloquear"); administrators change any test (`AppSession.IsTestUnlocked`).
  Archiving probetas is for administrators, like every other archive.
- Every window must handle resizing (minimums fit about 900 × 500).

