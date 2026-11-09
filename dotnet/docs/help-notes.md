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
- **Only administrators archive and restore.** Operators can create and edit clients, providers and
  materials; the "Archivar"/"Restaurar" button is disabled for them (tooltip explains why).
  Archived records are hidden, never deleted: "Mostrar archivados" shows them in grey.
- **Over-privileged MySQL accounts are refused.** The connection cannot be saved with an account
  that can DELETE, DROP, ALTER or CREATE (e.g. `root`). Explain why: records are archived, never
  deleted, and the program must not be able to change the database structure.
