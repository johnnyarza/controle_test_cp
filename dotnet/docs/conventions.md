# Conventions

How the code is organized and the traps already found. Status: [porting-status.md](porting-status.md).

## Glossary

Screens are in Spanish; code, tables and stored values in English.

| Screen (Spanish) | Code / table |
|---|---|
| Ensayo | `CompressionTest` / `CompressionTests` |
| Probeta | `Specimen` / `Specimens` |
| Dosificación | `ConcreteDesign` / `ConcreteDesigns` (its materials: `ConcreteDesignMaterial`) |
| Cliente / proveedor | `Client`, `Provider` (both `ContactRecord`) |
| Material | `Material` |
| Rotura (carga) | `RuptureLoadTon`; fecha de rotura `RuptureDate`, moldeo `MoldDate` |
| Archivar | `IsArchived` (nothing is ever deleted) |
| Etiqueta QR | `SpecimenLabels`, code `Specimen.PublicCode` |
| Configuración | `SettingsPageViewModel`, sections `SettingsSection` |

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
- Help: every new page, dialog, Configuración section or startup step gets a topic in `ViewModels/HelpTopics.cs`
  (a test fails otherwise; point it at the closest existing topic). Don't edit `Help/ayuda.html` with each change:
  add a line to "Pending help updates" in [help-notes.md](help-notes.md) and remind the user; the page is
  updated in batches when the user asks.

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
- MigraDoc 6 ignores `PageSetup.Orientation`: give the page its landscape `PageWidth` / `PageHeight` instead
  (`ReportLayout.Create`).
- MigraDoc stores layout data on the document: never run two renderers on one `Document` (the PDF is made
  from a `Clone()`).
- GDI+: read `Graphics.DpiX` before `GetHdc()`, not while the device context is held.
- MigraDoc table borders add their width to each row, even with `RowHeightRule.Exactly`: a grid that must land
  on exact positions (labels) has no borders; outlines are `TextFrame`s placed relative to the page.
- Report pages drawn at screen resolution squeeze word spacing; the preview draws at about twice the pixels.
- The help page runs in the Windows web control (Internet Explorer 11): ES5 script only (a test rejects
  `=>`, `let`, `const`, template strings), `<meta http-equiv="X-UA-Compatible" content="IE=edge">` or it acts
  like IE7, and `main, nav, section { display: block; }` because IE11 treats `<main>` as inline. Links inside the
  page are handled by its script (the page is loaded from a string, so it has no address to navigate to).
- ZXing (tests) misses some perfectly sharp QR codes at one exact size, so tests read a code at several sizes
  (`LabelLayoutTests.Decode`). About 1 code in 450 it misses at most sizes (a mask pattern its detector dislikes;
  the QR is valid): tests that read labels back use the fixed `ReadableCodes`, never random codes (see decisions.md).
- A window shown while a modal dialog is open is disabled with the rest of its thread: the help window has its
  own UI thread and can't use the app's styles (they belong to the main thread), so it is built in code.
- A drop-down panel with a date field inside (the probetas filter) can't be a `StaysOpen="False"` Popup: the
  field's calendar is a popup of its own, which captures the mouse, and picking a day closed the panel. Use
  `b:PopupPanel.Opener` on a `StaysOpen="True"` Popup: it closes on Esc, on a click in the window itself (not in
  any popup) and when the window moves or loses focus. Check such panels with real mouse clicks, not UIA Invoke.

## Performance

Checked on 2026-11-11 with 25,000 ensayos, 150,000 probetas, 800 clients and 250 designs (MySQL 8.4,
same PC): every list query stays under 150 ms on the server; in the app the Ensayos list shows in about
0.6 s, "Mostrar más" takes about 0.2 s, a text search about 2 s including the 300 ms typing delay, and
memory stays near 200 MB. The warning after login was the only slow screen (about 6.6 s for 400 rows
built at once) until it got the 30-day window and a virtualized list.


A list that can grow (more than a screenful of rows) must virtualize: a `DataGrid`, or an `ItemsControl`
with a `VirtualizingStackPanel`, `CanContentScroll="True"` and a bounded height. Nested `ItemsControl`s


## Checking screens

When a change is visual (or on request) the real exe is driven with Windows UI Automation (`dotnet/tools/ui-automation.ps1`)
against a throwaway `probeta_test_*` database seeded with sample data. Screenshots are taken at normal
size and at the minimum window size, and the log must show no binding errors.
