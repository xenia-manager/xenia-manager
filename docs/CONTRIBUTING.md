# Contributing Guide

Welcome, and thank you for your interest in contributing to Xenia Manager. Please follow these guidelines to maintain
code quality and consistency across the project.

---

## Table of Contents

1. [Project Structure](#project-structure)
2. [Naming Conventions](#naming-conventions)
    - [Functions and Methods](#functions-and-methods)
    - [UI Elements (AXAML)](#ui-elements-axaml)
    - [Variables and Fields](#variables-and-fields)
    - [Properties](#properties)
3. [Coding Standards](#coding-standards)
    - [File Organization](#file-organization)
    - [MVVM Pattern](#mvvm-pattern)
    - [Commenting](#commenting)
    - [Error Handling and Logging](#error-handling-and-logging)
    - [Formatting](#formatting)
4. [Creating Custom Themes](#creating-custom-themes)
5. [Submitting Changes](#submitting-changes)

---

## Project Structure

The project is organized into the following projects:

- **XeniaManager**: Main application project containing Views, ViewModels, and UI-related logic
- **XeniaManager.BigScreen**: Fullscreen application project for TV/big-screen usage
- **XeniaManager.Core**: Core library containing business logic, services, and utilities
- **XeniaManager.Database**: Online database clients and their models for Xbox, compatibility, patches and optimized settings
- **XeniaManager.Files**: File format parsers and their models for Xbox/Xenia file types (ISO, XEX, STFS, GPD, ZAR, etc.)
- **XeniaManager.Logging**: Logging infrastructure built on NLog

All core logic should be placed in the appropriate library (**XeniaManager.Core**, **XeniaManager.Database**, **XeniaManager.Files** or **XeniaManager.Logging**) to facilitate easier implementation of features across different UI platforms (Desktop App, Fullscreen App, etc.).

---

## Naming Conventions

### Functions and Methods

- Use **PascalCase** for method names
- Methods should clearly describe their purpose
- Example:
  ```csharp
  public void LoadLibrary()
  {
      // Implementation here
  }
  ```

### UI Elements (AXAML)

- Use **Hungarian Notation** with type prefixes:
    - `ComboBox` → `Cmb`
    - `TextBox` → `Txt`
    - `Button` → `Btn`
    - `TextBlock` → `Tbl`
    - `StackPanel` → `Sp`
    - `Grid` → `Grd`
    - `ScrollViewer` → `Sv`
    - `Expander` → `Exp`

- Property order in AXAML elements:
    1. `x:Name` / `x:Class`
    2. `x:DataType` (if using compiled bindings)
    3. `Grid.Column`, `Grid.Row`, `Grid.ColumnSpan`, `Grid.RowSpan`
    4. Data bindings (`{Binding ...}`, `{DynamicResource ...}`)
    5. Layout properties (alphabetically): `HorizontalAlignment`, `Margin`, `Padding`, `VerticalAlignment`, etc.
    6. Style properties (alphabetically): `FontSize`, `FontWeight`, `Foreground`, etc.
    7. Event handlers

- Example:
  ```xaml
  <ComboBox x:Name="CmbLanguage"
            Grid.Column="1"
            AutomationProperties.Name="{DynamicResource SettingsPage_LanguageSelector}"
            AutomationProperties.HelpText="{DynamicResource SettingsPage_LanguageSelectorTooltip}"
            DisplayMemberPath="Name"
            SelectedValuePath="Name"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            MinWidth="150"
            SelectionChanged="CmbLanguage_SelectionChanged" />
  ```

### Variables and Fields

- **Private instance fields**: Use `_camelCase` with leading underscore
    - Example: `_settings`, `_releaseService`
- **Local variables**: Use `camelCase`
    - Example: `gameId`, `userInput`
- **Static fields**: Use `PascalCase` or `_camelCase` depending on visibility
    - Example: `Games` (public), `_isRunning` (private)

### Properties

- **Public properties**: Use **PascalCase**
    - Example: `TitleId`, `Games`
- **Partial methods for property changes** (CommunityToolkit.Mvvm):
  ```csharp
  [ObservableProperty]
  private bool _checkForUpdatesOnStartup;

  partial void OnCheckForUpdatesOnStartupChanged(bool oldValue, bool newValue)
  {
      if (oldValue == newValue) return;
      Logger.Info<SettingsPageViewModel>(
          $"Check for Updates on Startup changed from '{oldValue}' to '{newValue}'");
      _settings.Settings.UpdateChecks.CheckForUpdatesOnStartup = newValue;
      _settings.SaveSettings();
  }
  ```

---

## Coding Standards

### File Organization

- Place all business logic in the library projects (**XeniaManager.Core**, **XeniaManager.Database**, **XeniaManager.Files**, **XeniaManager.Logging**)
- Keep Views lightweight, delegating logic to ViewModels and Core services
- Organize files by feature/namespace rather than type when possible

### MVVM Pattern

- Use **ViewModels** for UI state and data binding
- Use **CommunityToolkit.Mvvm** for MVVM implementation:
    - `[ObservableProperty]` for observable properties
    - Partial methods (`On<PropertyName>Changed`) for property change logic
- Keep code-behind (`.axaml.cs`) files minimal, containing only view-specific logic

### Commenting

- Use XML documentation comments for public and internal types and members:
  ```csharp
  /// <summary>
  /// Manages the game library by loading from and saving to a local file
  /// </summary>
  public class GameManager
  {
      /// <summary>
      /// Loads the game library from the local file.
      /// If the file doesn't exist, creates a new empty library.
      /// If the file is corrupted, attempts to recover from backup.
      /// </summary>
      public static void LoadLibrary()
      {
          // Implementation
      }
  }
  ```

- Use inline comments sparingly, only when the intent is not obvious from the code itself

### Error Handling and Logging

- Use `try-catch` blocks to handle exceptions appropriately
- Log all exceptions using the `Logger` class:
  ```csharp
  try
  {
      // Operation
  }
  catch (Exception ex)
  {
      Logger.Error<ClassName>("Error description");
      Logger.LogExceptionDetails<ClassName>(ex);
  }
  ```

- Use appropriate log levels:
    - `Trace`: Detailed debugging information
    - `Debug`: General diagnostic information
    - `Info`: General operational messages
    - `Warning`: Potential issues that don't stop execution
    - `Error`: Errors that cause operations to fail
    - `Fatal`: Critical errors that may cause application termination

- Throw `Exception` (or specific exception types) for unimplemented features or invalid states

### Formatting

- Use **4 spaces** for indentation (no tabs)
- Place opening braces `{` on a **new line**
- Use expression-bodied bodies only for simple single-line methods; properties and accessors always use block bodies (`get { return ...; }`)
- Use file-scoped namespaces:
  ```csharp
  namespace XeniaManager.Core.Manage;

  public class GameManager
  {
      // Implementation
  }
  ```

- Use `using` directives sorted alphabetically, with system namespaces first
- Always write explicit types - never `var` and never target-typed `new()`
- Keep lines under 160 characters

The rules in this section are encoded in the root `.editorconfig` and enforced by `python scripts/lint.py` (ReSharper cleanupcode).

---

## Creating Custom Themes

Xenia Manager supports custom themes. To create a new theme:

1. **Copy the template file**
   - Navigate to `source/XeniaManager/Resources/Themes/`
   - Copy `Template.axaml` to a new file (e.g., `MyCustomTheme.axaml`)

2. **Define your theme colors**
   - Open your new `.axaml` file
   - Replace all color values with your theme's colors
   - Keep the `x:Key` names unchanged - they are required by the application

3. **Register your theme**
   - Open `source/XeniaManager.Core/Models/Theme.cs`
   - Add your theme name to the `Theme` enum:
   ```csharp
   public enum Theme
   {
       Light,
       Dark,
       MyCustom // Add your theme here
   }
   ```
   - Open `source/XeniaManager/Services/ThemeService.cs`
   - Add your theme to the `_themeConfigs` dictionary:
   ```csharp
   [Theme.MyCustom] = new ThemeConfiguration
   {
       BaseTheme = ThemeVariant.Dark, // or ThemeVariant.Light
       ResourcePath = "avares://XeniaManager/Resources/Themes/MyCustomTheme.axaml",
       FallbackTheme = Theme.Dark // optional fallback
   },
   ```

4. **Build and test**
   - Build the project and verify your theme loads correctly
   - Test with various controls (buttons, textboxes, lists, etc.)

### Theme Color Guidelines

- **For DARK themes:** Use dark backgrounds (`#FF000000`) with light text (`#FFFFFFFF`)
- **For LIGHT themes:** Use light backgrounds (`#FFFFFFFF`) with dark text (`#FF000000`)
- **Accessibility:** Maintain sufficient contrast ratios (WCAG AA minimum recommended)
- **Accent color:** Choose a color that works on both light and dark backgrounds

---

## Submitting Changes

1. **Create a Branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```
   Branch naming convention:
   - `feature/description` - New features
   - `bugfix/description` - Bug fixes
   - `refactor/description` - Code refactoring
   - `docs/description` - Documentation changes

2. **Run the Linter**:
    - Format your changes before committing:
      ```bash
      python scripts/lint.py
      ```
    - To verify without modifying files (same check CI runs):
      ```bash
      python scripts/lint.py --check
      ```
    - To format only specific files or globs (repeatable, supports `*` and `**`):
      ```bash
      python scripts/lint.py --include "source/XeniaManager.Core/Services/EventManager.cs"
      python scripts/lint.py --include "source/XeniaManager.Core/**/*.cs" --include "source/XeniaManager/ViewModels/Foo.cs"
      ```
    - To format only changed files:
      ```bash
      python scripts/lint.py --changed          # staged + unstaged + untracked
      python scripts/lint.py --staged           # staged only
      python scripts/lint.py --changed --include "source/**/*.cs"  # intersection: changed files matching glob
      ```

3. **Write Meaningful Commits**:
   - Use conventional commit format:
     ```bash
     git commit -m "[Feature] Add game details editor dialog"
     git commit -m "[Bugfix] Fix crash when loading corrupted library file"
     git commit -m "[Refactor] Extract logging logic to separate service"
     ```
   - Keep commits atomic and focused on a single change
   - Write clear, descriptive commit messages

4. **Submit a Pull Request**:
   - Push your branch to the remote repository
   - Open a pull request targeting the `dev` branch
   - Link to any related issues
   - Provide a clear description of:
     - What changes were made
     - Why the changes were necessary
     - Any testing performed
     - Screenshots (for UI changes)

---

Thank you for contributing to Xenia Manager!
