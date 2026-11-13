# Help file: topics to cover

Running list of things the built-in help (`Help/ayuda.html`, phase 6) must explain.
Add an item whenever a question comes up that a user would also ask. The help itself is written in Spanish.

## Primer inicio / instalación

- **First login after creating the database.** The database update creates one program user:
  `admin` / `admin`. At the first login the program forces a new password (minimum 8 characters).
  There is no other user until an administrator creates them (Configuración → Usuarios).
  The login screen deliberately shows no hint about the default credentials.
- **Two kinds of "user".** The connection screen asks for the program's *MySQL account*
  (`specimen_app`), not for a program user. Program users (`admin`, operators) are entered on the
  login screen. Schema updates ask for a third account with structure rights (`specimen_admin`),
  which is never saved.
- **Only administrators archive and restore.** Operators can create and edit clients, providers,
  materials, dosificaciones and ensayos; the "Archivar"/"Restaurar" button is disabled for them (tooltip
  explains why), and the program refuses it even if it were reached another way.
  Archived records are hidden, never deleted: "Mostrar archivados" shows them in grey.
- **Right-click menu on lists.** Right-clicking a row (or the menu key / Shift+F10 on the selected row)
  selects it and offers "Editar" and "Archivar" ("Restaurar" on an archived row), the same as the
  toolbar buttons (Dosificaciones also has "Duplicar"); double-click or Enter also opens "Editar".
- **Records in use can't be archived.** "Archivar" shows a warning (no question) listing up to 5 of the
  records that still use it, and how many more:
  - a client: active ensayos where it is the client or the concrete provider;
  - a provider: active materials;
  - a material: active dosificaciones;
  - a dosificación: active ensayos.
  Archived records don't count, so the way out is to archive those first (or change the material /
  provider on them). Restoring is never blocked.
- **Over-privileged MySQL accounts are refused.** The connection cannot be saved with an account
  that can DELETE, DROP, ALTER or CREATE (e.g. `root`). Explain why: records are archived, never
  deleted, and the program must not be able to change the database structure.

## Ensayos

- **Start page.** The program opens on Ensayos, newest first. "Para romper" (red badge) counts the
  probetas without a rupture load whose rupture date is today or already passed. "Solo con
  probetas para romper (N)" shows only those ensayos; N counts them over all clients.
- **Searching.** Buscar finds the ensayo number, client, project or address. "Cliente: Todos los
  clientes" opens the client search to show one client's ensayos; the × removes the filter.
- **Client search dialog.** Type part of the name, phone or e-mail; double-click, Enter or "Elegir"
  picks the client. With a single match it is already selected. Only active clients are listed.
  When choosing the client or concrete provider of an ensayo, "Nuevo cliente" creates one (name
  pre-filled with the search) and chooses it at once; the list filter doesn't offer it.
- **Dates.** Typed day first: "05/11/2026", "5/11/26", "5-11-2026" and "5.11.2026" are all
  5 November 2026 (the PC's regional settings don't matter). The calendar button (or Alt+↓ / F4)
  opens a calendar; the field tidies the text to dd/mm/aaaa when you leave it.
- **New ensayo.** Fecha (today by default), Cliente and Proveedor del hormigón (both chosen with
  "Buscar…"; "Igual que el cliente" copies the client), Dosificación, Proyecto / obra, Ubicación.
- **Locked ensayos.** An operator can change the ensayos they created since logging in; others open
  read-only ("El ensayo está bloqueado…", "Cerrar"). An administrator can change any ensayo, and
  unlock one for an operator. Archiving ensayos is for administrators.

## Aviso de probetas para romper

- **When it appears.** Right after logging in, if there are unbroken probetas whose rupture date is
  today or tomorrow, or passed at most 30 days ago. It lists them by ensayo — today first, then
  tomorrow, then the overdue ones — with the counts and the same colours as the ensayo page.
- **Older overdue probetas** (more than 30 days late) are not listed one by one: a line says how many
  there are, and "Ver en Ensayos" shows the Ensayos list with "Solo con probetas para romper". If only
  such old ones exist, the warning doesn't open.
- **Abrir ensayo** closes the warning and opens that ensayo; "← Ensayos" goes back to the list.
- **No volver a mostrar hoy en este equipo** hides it until tomorrow, on this PC and Windows user only.
  The Ensayos list still shows "Para romper" and "Solo con probetas para romper".

## Configuración

- **What each user sees.** Administrators: Usuarios, Datos del informe, Logo y sello, Conexión, Mi contraseña, Acerca de.
  Operators: Conexión, Mi contraseña, Acerca de.
- **Usuarios.** "Nuevo usuario" asks for the user name (no spaces), full name, role and a first
  password (minimum 8 characters) that the user must change at the first login. "Restablecer
  contraseña…" sets a temporary password with the same rule. "Archivar" stops the user from logging in
  (never deleted; "Mostrar archivados" + "Restaurar"). Your own user can't be archived or reset here
  (use Mi contraseña), your own role can't be changed, and there must always be one active administrator.
- **Logo y sello.** The images printed on reports, stored in the database so every PC prints the same.
  PNG or JPG, up to 2 MB; a file that only has a .png/.jpg name but is another format is refused.
  The image is saved as soon as it is chosen; "Quitar" removes it.
- **Conexión.** Shows the server, database and MySQL account of this PC. "Cambiar conexión…" closes
  the session and opens the connection form of the startup window; after saving, log in again.
- **Mi contraseña.** Current password, new password twice (minimum 8 characters, different from the
  current one).
- **Acerca de.** Program version, database version, MySQL server version, PC name and the log folder
  ("Abrir carpeta de registros"), useful when asking for support.

## Ensayo (detail page)

- **Opening.** "Abrir", double-click or Enter on an ensayo; "Nuevo" asks for the header and then opens
  the new ensayo. "← Ensayos" (or Alt+←) returns to the list with the same search and filters.
- **Colours.** Red = retrasada (rupture date passed, not broken); yellow = to break today or tomorrow;
  grey = archived. The Estado column says the same in words (Retrasada, Hoy, Mañana, Pendiente, Rota),
  so colour is never the only clue. "N probetas para romper" counts today's and overdue ones.
- **Locked / unlocked.** The line under the header says what the user may do. Locked: an operator can
  only type the rupture load of a probeta due today or later (the form says so and starts on
  "Rotura (t)"); overdue or already broken probetas need an administrator. "Desbloquear" asks an
  administrator's user and password and unlocks this ensayo for the signed-in operator until
  "Bloquear" or logout. Administrators are always unlocked.
- **Layout.** Title with the lock state (Bloqueado / Desbloqueado / Administrador) and "Editar datos",
  the header on one line, four tiles (probetas, para romper, rotas, average strength with % fck; they
  always cover the whole ensayo, filter or not), then the probetas table with its buttons.
- **Registrar rotura** (button, Ctrl+R, right-click, or double-click / Enter on a probeta without a
  load): a small dialog showing the probeta read-only with one large field for the load and the
  resulting strength and % fck. "Guardar y siguiente (Nº X)" saves and opens the next probeta due
  today or earlier, so the day's probetas are broken one after another. Double-click on a broken
  probeta opens the full form ("Editar").
- **Probeta form.** Sections Identificación, Fechas (Moldeo + Edad = Rotura), Medidas, Rotura.
  "Hoy" fills the mold date; Edad (días) or the 7 / 14 / 28 buttons fill the rupture
  date (changing the rupture date updates the age; changing the mold date keeps the age). The
  calculator button next to Diámetro / Altura averages two readings. Densidad and Resistencia are
  shown as you type, with the strength as a percentage of the design fck. A new probeta starts with
  the slump, diameter and height of the ensayo's newest probeta (a note says which); the weight and
  the dates are typed each time. Saving locks the measurements (legacy rule).
- **% fck.** The grid shows each broken probeta's strength as a percentage of the design fck
  (shown with the design in the header). Early ages naturally give lower values.
- **Copiar.** A new probeta with the selected one's measurements and dates, without its rupture load.
- **Filtrar.** By mold date range and/or probeta numbers ("1-5, 8, 10-12"); the active filter shows in
  a blue bar with "Quitar filtro". The due count always covers the whole ensayo.

## Informes

- **Imprimir.** The ensayo page's "Imprimir" (Ctrl+P) opens the report in a preview window. It prints the
  probetas the page shows (with the filter, if one is set), never archived ones; the tooltip says which.
  The data is read again from the database, so the report is always up to date.
- **Preview window.** No PDF program is needed. "Imprimir…" (Ctrl+P) opens the Windows print dialog
  (printer, copies, pages "Desde/Hasta"); "Guardar PDF…" (Ctrl+S) saves a PDF, e.g. to send by e-mail
  (suggested name: date, ensayo Nº, client, project). − / + (Ctrl+− / Ctrl++) and "Ancho de página"
  (Ctrl+0) change the zoom. "Cerrar" or Esc.
- **No printer installed.** "Imprimir…" says so; save the PDF and print it on another PC.
- **Paper.** Reports use the paper chosen in Configuración → Datos del informe (A4 or Carta). If the printer
  doesn't have that paper, the page is shrunk to fit on what it has instead of being cut off.
- **What the ensayo report shows.** Header (logo, title, lab name, ensayo Nº and date, printed date,
  "Hoja X de Y"); client, concrete provider, project, location, design with fck, slump with its tolerance
  (up to 9 cm ± 1, up to 15 cm ± 2, more ± 3) and a/c; the mix per m³; the probetas table ("—" = not broken
  yet); a summary per age (number, minimum, average, % fck) with the strength-vs-age chart (points =
  probetas, line = average per age, dashed red = fck); the note and the stamp; the lab's address at the
  bottom of every page.
- **Datos del informe** (administrators, Configuración): lab name (required), footer, note, paper. Saved
  in the database, so every PC prints the same; the logo and stamp are in "Logo y sello".

## Dosificaciones

- **Materials panel.** The list shows description, fck, slump, a/c and the number of materials (hover
  for the full list). The panel below the list shows the selected design's materials (#, material,
  provider, type, kg/m³) with the total and a/c. Drag the bar between them to give either more room.

- **Decimal numbers.** Every numeric field accepts a comma or a point as the decimal separator
  ("12,5" = "12.5"). Thousands separators are not accepted ("1.234,5" gives "Número no válido."),
  so a value is never misread. Lists show a point.
- **Material table.** One row per material with its kg/m³, at most 8 rows; at least one material is
  required. "Agregar material" adds a row (disabled at 8, tooltip says why); the bin removes a row; the
  arrows change the order, which is the order saved and printed (rows are renumbered 1..n). Each row's
  list only offers materials not used on another row. Empty rows are ignored when saving. The total
  kg/m³ is shown under the quantities.
- **Material type and a/c.** Each material has a Tipo: Cemento, Agua, Agregado, Aditivo or Otro
  (Materiales → Editar). The design form and the Dosificaciones list show the water/cement ratio
  a/c = kg of Agua / kg of Cemento (2 decimals); it shows "—" until the design has a Cemento and an
  Agua with quantities. After the database update (script 011) existing materials get a type guessed
  from their name (cemento…, agua…, arena/grava/piedra/árido…, aditivo…); the rest are "Otro" and
  should be checked once.
- **Designs used by ensayos.** Opening one shows a warning with the number of ensayos (archived ones
  included): changing it also changes their data and reports. Only an administrator can change it;
  an operator sees it read-only ("Cerrar" instead of "Guardar"). To change a mix that is already in use
  without touching old reports, create a new design instead ("Duplicar", below).
- **Duplicar.** Dosificaciones has "Duplicar" (toolbar and right-click menu), for operators too: it opens
  a new design with the same fck, slump, materials and quantities, named "… (copia)". The description
  must be unique, so rename it before saving. Archived materials are not copied (a yellow note names
  them). The original design is not changed.
- **Archived materials.** A design keeps showing a material that was archived later, marked
  "(archivado)", and can still be saved with it. New designs can only pick active materials.
- **Calculadora.** Fills the quantity of one material as a percentage of another
  (destino = origen × % / 100), e.g. an admixture at 0,8 % of the cement. It needs at least two
  materials chosen and the origin's quantity typed; it shows the result before "Aplicar" and writes it
  into the destination row (3 decimals). It doesn't save: the design is saved with "Guardar".
