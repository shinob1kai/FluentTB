# Taskbar settings translations

Taskbar shape uses the same runtime language selection and resource dictionaries as the rest of the desktop app. All 27 strings, including the page title and save confirmation, are present in all 29 `Resources/Localization/Dictionary-*.xaml` files. The existing English fallback still serves other incomplete upstream pages. The Sinhala dictionary is included even though it is not currently a separate entry in the upstream language picker.

Edit each language's XAML dictionary directly. This addition has been checked for completeness and XAML validity; wording across all languages has not been reviewed by native speakers.

Run `./tests/Localization.Tests.ps1` from the repository root to check required keys, nonempty values, duplicate taskbar keys and the Home / Taskbar shape navigation order. Publish/build also compiles all resource dictionaries.

Taskbar labels wrap for longer translations, and the save confirmation uses a dynamic resource so an existing confirmation follows a later language change.
