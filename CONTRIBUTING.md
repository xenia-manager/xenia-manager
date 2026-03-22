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
4. [Submitting Changes](#submitting-changes)

---

## Project Structure

The project is organized into two main projects:

- **XeniaManager**: Main application project containing Views, ViewModels, and UI-related logic
- **XeniaManager.Core**: Core library containing all business logic, services, models, and utilities

All core logic should be placed in **XeniaManager.Core** to facilitate easier implementation of features across different UI platforms (Desktop App, Fullscreen App, etc.).

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

- Place all business logic in **XeniaManager.Core** project
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
- Use expression-bodied members when appropriate for simple methods/properties
- Use file-scoped namespaces:
  ```csharp
  namespace XeniaManager.Core.Manage;

  public class GameManager
  {
      // Implementation
  }
  ```

- Use `using` directives sorted alphabetically, with system namespaces first
- Prefer `var` when the type is obvious, explicit types when clarity is needed

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

2. **Write Meaningful Commits**:
   - Use conventional commit format:
     ```bash
     git commit -m "[Feature] Add game details editor dialog"
     git commit -m "[Bugfix] Fix crash when loading corrupted library file"
     git commit -m "[Refactor] Extract logging logic to separate service"
     ```
   - Keep commits atomic and focused on a single change
   - Write clear, descriptive commit messages

3. **Submit a Pull Request**:
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
