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
  inside `xvfb-run`). Wine substitutes another font for Segoe UI, so judge spacing and
  hierarchy there, not exact font rendering. Give a freshly published build 20 seconds
  to start before taking a screenshot.

## The look this app has chosen

Paper and ink, like a library catalogue: a light page (`Theme.Page`), dark text
(`Theme.Ink`), ruled tables that sit directly on the page, and no cards. The school's
navy and gold live in the sidebar. On the page, navy marks the one primary action of a
screen; red, amber and green appear only on status words. Page titles and the brand are
set in Crimson Text; everything else is Segoe UI. New work should extend this, not
reintroduce a dark dashboard.

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
- Inputs have a visible caption (`FormField`), which is also set as `InputBox.Caption` so
  the inner `TextBox` gets it as its `AccessibleName`. An input without a visible caption
  (a search box) still sets `Caption`. A placeholder is not a label.
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
- Labels and custom buttons set `UseMnemonic = false`, so an `&` in a book title or in
  "Issue & return" is shown rather than swallowed. Global shortcuts live in
  `MainForm.ProcessCmdKey` (Ctrl+1 to Ctrl+6 for the screens, F5 to reload).
- Toasts and overlays never cover the focused control.

### Forms (web: autocomplete, types, labels, errors)

- Never block paste or the context menu on text boxes (`ShortcutsEnabled` stays true).
- Set `MaxLength`; for codes and IDs (barcode, member ID) consider
  `CharacterCasing.Upper` rather than reformatting the text while the user types.
- The submit button stays enabled until the request starts, then the shell's busy state
  takes over (`IShell.RunAsync` sets the `BusyBar` and wait cursor). Do not add a second
  spinner.
- **Validation errors go next to the field**, not in a toast: set `FormField.Error` (or
  call `FormField.Require`), which shows the reason under the input and outlines it in
  red. Check all fields, then move focus to the first bad one.
- Placeholders show an example in the expected shape (`e.g. BTS-10-214`).
- Unsaved edits are not lost silently. The desk screen does this with
  `ManageView.ConfirmDiscardEdits()`, called before opening another book, navigating away
  and closing the window; any new editable screen needs the same.

### Motion (web: prefers-reduced-motion, transition: all)

- Respect the Windows "Show animations" setting through `Drawing.AnimationsEnabled`: when
  it is off, show a static state instead of moving anything (see `BusyBar`).
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
- The title typeface (Crimson Text, `AppFonts.Title`) only for page titles, the brand and
  a book's title on the desk screen. Everything a librarian reads or types (tables, forms,
  buttons, captions) uses Segoe UI (`AppFonts.Body`, `AppFonts.Strong`). Never set a size
  below 9 pt.
- Use the fonts in `Theme.cs`; no ad-hoc `new Font(...)` in views.
- **Only ship fonts whose licence allows it.** Embedded fonts must be under an open
  licence such as the SIL Open Font License, with the licence file kept next to the font
  in `Assets/Fonts/`. Before adding one, read the copyright and licence fields inside the
  file itself (name IDs 0, 13 and 14); a "free download" page is not a licence. The app
  once shipped a renamed copy of a commercial typeface this way.

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
  framework does not scale itself (grid row height, column widths) are scaled by
  `DeviceDpi / 96f`.
- A size set in code after the form has been scaled must be scaled by hand (see
  `AppButton.SetText`), and a control that positions a child itself must lay out again
  when it becomes visible (see `InputBox.OnVisibleChanged`): WinForms scales with layout
  switched off.
- In tables, numbers, identifiers, dates and statuses get a fixed width
  (`ColumnDefinition.Fixed`) so they are never cut off; only free text shares the rest.
- Check at 100 % and 150 % scaling. No horizontal scrollbar at the minimum window size
  (1120 × 680).

### Native parts (web: color-scheme, theme-color)

- The app is light on purpose, so the parts Windows draws (title bar, scroll bars,
  `MessageBox`, file dialogs, check boxes) match without any tricks. Do not add a dark
  mode without also handling every one of those.
- Use native controls where they exist (`CheckBox` in Settings) rather than drawing a
  web-style replacement.
- Avoid chains of message boxes. Prefer inline confirmation for frequent actions.

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
  "Continue". A button that opens a dialog ends with `…`.
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
should look shaped by those jobs. Each rule names the place in this app that shows the
intended answer.

1. **No identical stat-tile rows.** A row of equal tiles with a coloured dot, a small
   caption and a big number is the default dashboard. State the one or two numbers that
   need action as plain large text and the rest as one sentence (`TodayView`), or as a
   plain two-column list (`ReportsView.BuildFigures`).
2. **Don't repeat navigation.** No "Quick actions" panel that duplicates the sidebar. Give
   the space to real content: the list of books to chase, the books registered today.
3. **No subtitle that restates the title.** Use a subtitle only for real information: a
   date, a count, the next book number. `Make.Header` accepts `null`.
4. **Not everything is a card.** There are no cards in this app. Tables sit on the page;
   groups are separated by space, a heading (`Make.Section`) or a 1 px `Divider`.
5. **Vary size by importance.** The primary content of a screen gets most of the space;
   secondary panels are narrower and quieter. On the desk screen lending gets 56 % and
   the screen's only primary button; editing the record gets 44 % and plain buttons.
6. **One accent, used sparingly.** Navy (`ButtonKind.Primary`) is for the single primary
   action per screen. Gold appears only on the navy sidebar (current page, sample-data
   warning). Status colours (`Theme.Overdue`, `DueSoon`, `Available`) appear only where
   status is the information; "On loan" is not a colour.
7. **Colour dots are not decoration.** Put the status colour on the status word itself.
8. **Density that suits the work.** Table rows are 32 px, controls 32 px
   (`Theme.ControlHeight`), body text 10 pt. Do not add padding to look "clean".
9. **Real domain language and data.** "On the shelf", "8 days overdue", "Due tomorrow",
   "Class 10-B", "Books to chase". No marketing tone ("seamlessly", "Welcome back!"), no
   exclamation marks, no emoji, no icons standing in for words.
10. **No ornamental gradients, glows, shadows or glassmorphism.** Flat surfaces, hairline
    rules. One corner radius (`Theme.CornerRadius`, 4 px) for inputs and buttons only.
11. **Typographic hierarchy comes from size and weight, not from new fonts or colours.**
    Two families: Crimson Text for titles, Segoe UI for the rest.
12. **Feedback where the action happened.** The result of a button press is a line of
    text next to it (`ManageView.Say`, `RegisterView._result`, `ReportsView._saved`).
    `IShell.Notify` (the toast) is only for failures and background events.
13. **The school, not a theme.** The crest and the school's navy and gold are in the
    sidebar and nowhere else.
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
## src/LibraryManagement.App/Views/ExampleView.cs

src/LibraryManagement.App/Views/ExampleView.cs:22 - [generic] 5 identical stat tiles → state the 2 that need action, rest as one line
src/LibraryManagement.App/Views/ExampleView.cs:58 - [forms] empty-field error shown as toast → FormField.Error
src/LibraryManagement.App/Views/ExampleView.cs:71 - [copy] subtitle restates title

## src/LibraryManagement.App/Ui/Controls/ExampleControl.cs

src/LibraryManagement.App/Ui/Controls/ExampleControl.cs:20 - [a11y] custom-drawn, no AccessibleName/AccessibleRole

## Top 3
1. …
```
