---
name: winforms-design-guidelines
description: Design and review rules for this repo's WinForms app (src/LibraryManagement.App). Use when adding or changing any screen, control, colour, copy or layout in the app, when asked to "review the UI", "audit the design", "check accessibility", or when asked to make the app look less generic, less templated or less "AI-made". Translates Vercel's Web Interface Guidelines to WinForms and adds rules against the generic dashboard look.
argument-hint: <file-or-pattern>
---

# WinForms design guidelines (Library Management System)

Rules for `src/LibraryManagement.App`. Two parts:

1. **Interface rules**: Vercel's Web Interface Guidelines
   (`vercel-labs/web-interface-guidelines`, `command.md`, snapshot taken 2026-11-11),
   translated from HTML/CSS/React to WinForms. Rules with no desktop equivalent are listed
   at the end so nobody wonders whether they were forgotten.
2. **Anti-generic rules**: what makes this app look like a template or AI output, and what
   to do instead.

Unlike the `web-design-guidelines` skill, this one does not fetch anything at run time. The
rules are here, in the repo, and change only through a commit.

## How to use

- **Building or changing UI:** follow the rules while writing the code. Use the existing
  building blocks: colours and fonts in `Ui/Theme.cs`, layout helpers in `Ui/Make.cs`,
  controls in `Ui/Controls/`, screens in `Views/`. Do not add colours, fonts or sizes
  outside `Theme.cs`.
- **Reviewing:** read the files given (default: `src/LibraryManagement.App/**/*.cs`), check
  every rule below, and report in the output format at the bottom. Only report what the
  code actually shows; if a rule needs a running app to judge (contrast, real layout), say
  "needs visual check" instead of guessing.
- **Verifying visually:** the app runs under Wine in cloud sessions
  (`dotnet publish -r win-x64 --self-contained`, then `wine LibraryManagementSystem.exe --demo`
  inside `xvfb-run`). Wine substitutes Segoe UI and ignores the dark title bar, so judge
  spacing and hierarchy there, not exact font rendering.

---

## Part 1. Interface rules

### Accessibility (web: aria-label, semantic HTML, alt)

- Every custom-drawn control (anything with `ControlStyles.UserPaint`) sets
  `AccessibleName`, and `AccessibleRole` when it acts like a standard control
  (`PushButton`, `CheckButton`, `StaticText`, `Alert`). Screen readers see nothing else of
  what `OnPaint` draws.
- Values that change (stat numbers, connection state, toast text) update
  `AccessibleName`/`AccessibleDescription` when they change, and call
  `AccessibilityNotifyClients(AccessibleEvents.NameChange, -1)` for live updates (web:
  `aria-live`).
- Inputs have a visible caption (`Make.Field`) and the `InputBox` passes it on as the inner
  `TextBox`'s `AccessibleName`. A placeholder is not a label.
- Decorative images (crest, status dots) are not focusable and need no name; meaningful
  images get `AccessibleDescription`.
- Use real `Button`/`CheckBox` subclasses for actions, never a `Panel` or `Label` with a
  `Click` handler.
- Screen order equals tab order: controls are added in reading order, or `TabIndex` is set
  explicitly. Check it by tabbing through each screen.

### Keyboard and focus (web: focus-visible, outline)

- Every interactive control is reachable with Tab and works with Enter/Space.
- Custom controls draw a focus indicator when `Focused && ShowFocusCues`. `ShowFocusCues`
  is WinForms' `:focus-visible`: it is false after mouse clicks and true after keyboard use.
- Never hide focus without a replacement. (`BookGrid` returns `ShowFocusCues => false`
  because the selected row is the indicator; keep the selected row clearly visible.)
- Each screen with one obvious action makes Enter do it (`InputBox.EnterPressed`, or
  `AcceptButton` on dialogs) and Esc cancel or clear (`CancelButton`).
- Consider access keys (`&Save`) on primary buttons; `Make.Text` sets `UseMnemonic = false`
  for data labels only, so `&` in book titles is not swallowed.
- Toasts and overlays never cover the focused control.

### Forms (web: autocomplete, types, labels, errors)

- Never block paste or the context menu on text boxes (`ShortcutsEnabled` stays true).
- Set `MaxLength`; for codes and IDs (barcode, member ID) consider
  `CharacterCasing.Upper` rather than reformatting the text while the user types.
- The submit button stays enabled until the request starts, then the shell's busy state
  takes over (`IShell.RunAsync` sets the `BusyBar` and wait cursor). Do not add a second
  spinner.
- **Validation errors go next to the field**, not in a toast: an `ErrorProvider` or a
  small `Theme.Danger` caption under the input, and focus moves to the first bad field.
  Toasts are for results ("Issued to …"), not for "this field is empty".
- Placeholders show an example in the expected shape (`e.g. BTS-10-214`).
- Unsaved edits (e.g. changed title on the Manage screen) are not lost silently: warn on
  navigation and in `FormClosing`, or save on leave.

### Motion (web: prefers-reduced-motion, transition: all)

- Respect the Windows "Show animations" setting (`SystemInformation.UIEffectsEnabled`, or
  `SPI_GETCLIENTAREAANIMATION`): no sliding toasts or moving busy bars when it is off; show
  a static state instead.
- Animate with a `System.Windows.Forms.Timer` that only invalidates the area that changed;
  never re-layout on every tick.
- Any animation must be interruptible: clicks and typing work while it runs.

### Typography

- `…` not `...`; curly quotes `“ ”` and apostrophes `’` in UI strings.
- Busy text ends with `…`: `Connecting…`, `Saving…`.
- Commands that open a dialog end with `…` (Windows convention): `Choose key file…`,
  `Change folder…`.
- Numbers that are compared (counts, IDs, days) are right-aligned in grid columns
  (`DataGridViewContentAlignment.MiddleRight`) so the digits line up.
- Display font (Starz 2016, `AppFonts.Display`) only for the page title and the brand.
  Everything a librarian reads or types (tables, forms, buttons, captions) uses Segoe UI
  (`AppFonts.Body`). Never set a size below 9 pt.
- Use the font sizes in `Theme.cs`; no ad-hoc `new Font(...)` in views.

### Content handling

- Text that can be long (titles, author names, borrower names) truncates with an ellipsis
  (`StringTrimming.EllipsisCharacter`, `AutoEllipsis`) or wraps (`WrapLabel`); it never
  overflows or gets cut mid-glyph. Truncated text shows the full value in a tooltip.
- Test every screen with: an empty library, one book, 2,000 books, a 120-character title,
  a name with an apostrophe, and a missing date.
- Empty states say what is empty and what to do next, in one line.

### Performance (web: virtualization, layout thrashing)

- Above ~1,000 rows, use `DataGridView.VirtualMode` instead of creating a row object per
  book.
- Wrap bulk changes in `SuspendLayout()`/`ResumeLayout()`; replace grid rows with one
  `Rows.AddRange` call.
- Never measure text or call `PerformLayout` from `OnPaint`. Cache measured sizes.
- Custom controls set `OptimizedDoubleBuffer` (no flicker) and dispose every `Brush`,
  `Pen`, `Font`, `Image` and timer they create.

### State and navigation (web: URL reflects state)

- The desktop equivalent of "state in the URL" is "state survives a restart": window size
  and position, the last screen, and the Library screen's sort column and filter belong in
  `AppSettings`.
- Destructive actions (delete) need a confirmation that names the item, with **No** as
  the default button, or an undo.

### DPI and layout (web: safe areas, flex over JS measurement)

- Layout comes from `TableLayoutPanel`/`Make.*`/`ColumnsPanel`, docking and auto-size, not
  hard-coded `Location`s.
- Every pixel value used in painting goes through `LogicalToDeviceUnits(...)`; values the
  framework does not scale itself (grid row height, column minimum width) are scaled by
  `DeviceDpi / 96f`.
- Check at 100 % and 150 % scaling. No horizontal scrollbar at the minimum window size
  (1120 × 720).

### Dark theme (web: color-scheme, theme-color)

- `NativeTheme.UseDarkTitleBar` on every top-level form and `UseDarkScrollBars` on every
  scrolling control.
- Native `MessageBox`, open/save and folder dialogs stay light; that is acceptable, but
  avoid chains of them. Prefer inline confirmation for frequent actions.

### Locale

- Dates and numbers shown to people use `CultureInfo.CurrentCulture`
  (`ToString("d MMM yyyy", CultureInfo.CurrentCulture)`, `"N0"`).
- Formats used for storage (`dd-MM-yyyy` in Firestore, `yyyy-MM-dd` in file names) use
  `CultureInfo.InvariantCulture` and never appear in the UI as such.

### States

- Every custom control has distinct rest, hover, pressed, focused and disabled looks;
  hover/pressed/focus have more contrast than rest, never less.
- Disabled controls also explain why when it is not obvious (e.g. "Issue book" is disabled
  until a known member ID is entered: say so under the field, as the Manage screen does).

### Copy

- Active voice, second person or plain imperative: "Choose your key file", not "The key
  file can be chosen".
- **Sentence case** for titles, headings and buttons. (Vercel's list says Title Case; this
  app follows the Windows convention instead, which uses sentence case.)
- Numerals for counts: "8 books", not "eight books".
- Button labels name the action: "Issue book", "Mark as returned", not "OK", "Submit",
  "Continue".
- Error messages say what happened and what to do next.

### Web rules that do not apply here

Hydration, SSR, `<a>` vs `<button>` routing, touch-action, tap highlight, overscroll,
safe-area insets, `loading="lazy"`, `fetchpriority`, preconnect/preload, `<meta>` tags,
`translate="no"`, zoom blocking, video vs GIF. Skip them.

---

## Part 2. Anti-generic rules (removing the "AI-made" look)

Generic UI comes from defaults applied evenly: every block is the same rounded card, every
number gets a tile, every heading gets a helper sentence, one accent colour goes on
everything. A school librarian at a desk does a few jobs many times a day; the design
should look shaped by those jobs. Check each rule against the code; the parenthesised notes
say where this app currently does the generic thing.

1. **No identical stat-tile rows.** A row of five equal tiles with a coloured dot, a small
   caption and a big number is the default dashboard. Show the one or two numbers that
   need action (overdue, due soon) prominently, and the rest as a single plain line of
   text. (`DashboardView`, `ReportsView`: 5 and 6 `StatCard`s.)
2. **Don't repeat navigation.** A "Quick actions" panel that duplicates the sidebar is
   filler. Give the space to real content, e.g. the list of overdue books with a Return
   button per row. (`DashboardView.BuildQuickActions`.)
3. **No subtitle that restates the title.** "Register a book / Add a new copy to the
   catalogue." says nothing new. Use a subtitle only for real information: a date, a count,
   a state. (`RegisterView`, `ManageView`.)
4. **Not everything is a card.** Use cards for things that are separate objects. The main
   table can sit directly on the page; groups inside a form can be separated by space or a
   1 px divider. Never put a card inside a card.
5. **Vary size by importance.** Equal widths, equal radii and equal gaps everywhere read as
   a template. The primary content of a screen gets most of the space; secondary panels are
   narrower and quieter.
6. **One accent, used sparingly.** Gold (`Theme.Accent`) is for the single primary action
   per screen and the current page marker. Not for chips, focus rings, toggle tracks, stat
   dots and borders all at once. Status colours (green/amber/red) appear only where status
   is the information.
7. **Colour dots are not decoration.** A dot next to a caption that also has a colour
   name is noise. Use a status colour on the status word itself, once.
8. **Density that suits the work.** Librarians scan lists of books. Rows of ~32–36 px,
   10 pt text and compact padding beat airy 40 px rows and 24 px card padding.
9. **Real domain language and data.** Use the library's terms (accession number, return,
   overdue, class 10-B) and realistic sample data. No marketing tone ("seamlessly",
   "effortlessly", "Welcome back!"), no exclamation marks, no emoji.
10. **No ornamental gradients, glows, shadows or glassmorphism.** Flat surfaces with clear
    contrast. Rounded corners: one small radius (4–6 px) for inputs and buttons; panels may
    be square.
11. **Typographic hierarchy comes from size and weight, not from new fonts or colours.**
    Two families at most (Starz 2016 for the title and brand, Segoe UI for the rest).
12. **Feedback where the action happened.** A confirmation next to the button or row that
    triggered it is more convincing than a toast at the bottom of the window; keep toasts
    for background events (auto-saved report, connection lost).
13. **The school, not a theme.** Use the crest and the school's own colours deliberately in
    one or two places (sidebar header, report export) rather than tinting the whole UI.
14. **Fewer explanations.** If a screen needs a paragraph of helper text, fix the screen.
    Keep helper text for things a librarian genuinely cannot know (where the Firebase key
    comes from).

When applying Part 2, change one screen at a time and capture a before/after screenshot so
the change can be judged, not assumed.

---

## Output format (for reviews)

Group by file, `file:line` (clickable), one finding per line, terse. Tag the rule:
`[a11y]`, `[focus]`, `[forms]`, `[motion]`, `[type]`, `[content]`, `[perf]`, `[state]`,
`[dpi]`, `[theme]`, `[locale]`, `[copy]`, `[generic]`. Files with nothing to report get
`✓ pass`. No preamble. End with the three findings that would improve the app most,
ranked.

```text
## src/LibraryManagement.App/Views/DashboardView.cs

src/LibraryManagement.App/Views/DashboardView.cs:22 - [generic] 5 identical stat tiles → lead with Overdue/Due soon, rest as one line
src/LibraryManagement.App/Views/DashboardView.cs:58 - [generic] Quick actions duplicate sidebar nav

## src/LibraryManagement.App/Ui/Controls/StatCard.cs

src/LibraryManagement.App/Ui/Controls/StatCard.cs:20 - [a11y] no AccessibleRole; value not announced on change

## Top 3
1. …
```
