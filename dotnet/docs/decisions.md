# Decisions

Decisions taken while porting Probeta Control to C# / WPF, and why. Status: [porting-status.md](porting-status.md).

- One `ProbetaControl.exe` (WPF, net48, Costura.Fody); config `ProbetaControl.config` next to the exe,
  encrypted with DPAPI for the PC.
- MySQL schema `probeta_control`, utf8mb4 / utf8mb4_0900_ai_ci, SQL limited to what MySQL 8.0.20 supports.
  The startup check refuses servers older than 8.0.20 (`StartupChecker.MinimumServerVersion`).
- The app's MySQL account has SELECT, INSERT, UPDATE and EXECUTE only. Nothing is deleted: records are archived
  (the one exception, photos, goes through a stored procedure; see phase 9 below).
  The connection is refused if the account could DELETE/DROP/ALTER/CREATE (e.g. `root`).
- Every data change writes an `AuditLog` row in the same transaction.
- Login required; users in the database with BCrypt passwords. A new database starts with
  `admin` / `admin`, which must be changed at first login. No hint about it on the login screen.
- Roles: `admin`, `operator`. Archive/restore and user administration are admin-only; operators create
  and edit. On a locked test an operator may only record a pending rupture load, and the weight while it is still empty (the lab weighs at molding or on rupture day; the weight is optional until the load is recorded). The admin-only rules
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
- QR labels: the QR holds a link to a future page on the lab's server (phase 9), used by lab staff
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
  Found 2026-11-20: every QR is valid, but for about 1 code in 450 the standard mask choice (QRCoder's, and
  ZXing's own encoder too) gives a pattern ZXing's image detector misses at most sizes; the same text with any
  other of the 8 masks reads at every size. The tests that read labels back therefore use a fixed list of
  checked codes (`tests/.../Reports/ReadableCodes.cs`); random codes are tested by decoding the QR's modules
  directly. Decided with the user: the program is left as is for now. Nobody scans labels before phase 9, and
  probetas are broken within weeks, so labels printed until then are gone by the time the phone page exists.
  **For phase 9:** make every label read with ZXing-based scanner apps, e.g. draw the QR with ZXing.Net and
  pick a mask its detector reads (try the standard one, check it at a few sizes, else the next mask).
- Screen text in Spanish; code, tables and stored values in English. Dates `dd/MM/yyyy`, never the PC's format.
  Numbers shown read-only (lists, tiles, totals, reports, labels) follow the PC's regional settings for both
  the decimal and the thousands separator (`1.050,5` or `1,050.5`); IDs (ensayo / probeta Nº, label Nº) are
  never grouped. Number fields being edited show no thousands separator and accept a comma or a point as
  decimal separator (`NumberInput`), so a typed `1.050` is never ambiguous. (Changed 2026-11-18; until then
  every number used the invariant culture.)
  Dates are typed day first (`DateInput`: `05/11/2026`, `5-11-26`, `5.11.2026`) in the `DateBox` field,
  never with the PC's regional settings; the stock DatePicker is not used for that reason.
- Tests are locked: an operator changes only the tests they created in this session (or that an
  administrator unlocks with "Desbloquear"); administrators change any test (`AppSession.IsTestUnlocked`).
  Archiving probetas is for administrators, like every other archive.
- Every window must handle resizing (minimums fit about 900 × 500).
- **Phase 9 phone page: photos of probetas** (decided 2026-11-18, not built yet). Opened from the label's QR, the
  page shows the probeta and lets lab staff add, replace and remove photos taken with the phone; nothing else can
  be changed there. Staff sign in with the same program users as the desktop app (remembered on the phone), so the
  audit says who did it; the link is plain HTTP, for the lab Wi-Fi only. The page has its own MySQL account,
  `web_app_user`, so MySQL itself enforces the limit: SELECT on what the page shows (ideally one view) and on the
  users' login columns, INSERT on `SpecimenPhotos` and `AuditLog`, EXECUTE (to delete). Table-level grants need
  the tables to exist, so its setup script runs after the schema update.
  Photos are stored in the database (`SpecimenPhotos`, script 014: MEDIUMBLOB, outside `Specimens` so lists stay
  fast; JPEG or PNG, at most 2 MB, size worked out by MySQL), resized to about 1600 px JPEG (~300 KB) before saving,
  a few per probeta at most (an app rule). **Deleting a photo really deletes the row** (an exception to "nothing is
  deleted", to save server space), with an `AuditLog` row (action `delete`) of who, when, which PC / phone, which
  probeta and the size, not the image; replacing is delete + add. MySQL reuses the freed space; the file only
  shrinks after `OPTIMIZE TABLE`, and older backups still hold deleted photos.
  **No app account gets DELETE.** A photo is deleted only with `CALL DeleteSpecimenPhoto(photoId, userId, machine)`,
  a stored procedure that runs as its definer (the schema owner that applied script 014), writes the audit row and
  deletes, inside the caller's transaction. The app accounts get EXECUTE on the database instead (database-level,
  so it works before the tables exist and on the `probeta_test_*` wildcard), and the startup check that refuses an
  account able to DELETE stays as it is. If the account that applied the scripts is ever dropped, the procedure
  stops working (MySQL: definer missing) until it is recreated.
  The desktop app can add, replace and remove the same photos too, in the probeta edit dialog (a photo section),
  with the same rules: same table, same resizing, same procedure. **Built 2026-11-18** (layout B chosen by the user from
  three mockups): a photo panel on the right of the probeta dialog (1000 px wide) with a large preview, thumbnails,
  Agregar… / Cambiar… / Quitar / Descargar…, drag and drop and Ctrl+V (`SpecimenPhotosViewModel`, `SpecimenPhotoService`).
  Changes are saved at once, not with "Guardar", so they work on a locked ensayo (any user) and on a fully read-only
  probeta; a new probeta gets photos once saved. The phone's EXIF orientation is applied before resizing; PNG and
  pasted images are stored as JPEG; "Descargar" saves the photo shown as "Probeta 2297 - foto 1.jpg".

