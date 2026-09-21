# Taskbar settings did not apply

Reproduced in the desktop UI: incrementing Margin from 3 to 4 and clicking Apply displayed the saved confirmation but persisted MarginBasic and all four margins as 3.

The NumberBox Value bindings relied on their default source-update trigger. This did not reliably push the displayed value into the page's settings model before Apply. Explicit `UpdateSourceTrigger=PropertyChanged` now updates all six numeric inputs immediately: basic margin, radius, top, bottom, left and right. Taskbar geometry logic is unchanged.

Validation:

- Live UI: Margin 4 persisted as 4, including all four basic-mode margins.
- The WPF regression runner uses the real NumberBox controls and binding declarations from the page. The old declarations reproduce `control=4, model=3`; all 18 updates pass with the fixed declarations.
- 130 existing geometry/settings assertions and all 29 localization dictionaries pass.

Run from the repository root:

```powershell
dotnet run --project tests/FluentTB.BindingTests/FluentTB.BindingTests.csproj -p:Platform=x64 -- src/FluentTB.Desktop/Pages/FluentTaskbarPage.xaml
```
