## **Contributing Guide**

Welcome, and thank you for your interest in contributing to Xenia Manager. Please follow these guidelines to maintain
code quality and consistency across the project.

---

### **Table of Contents**

1. [Naming Conventions](#naming-conventions)
    - [Functions](#functions)
    - [UI Elements](#ui-elements)
    - [Variables and Fields](#variables-and-fields)
2. [Coding Standards](#coding-standards)
3. [Submitting Changes](#submitting-changes)

---

### **Naming Conventions**

#### **Functions**

- Use **PascalCase** for function names.
- Function names should clearly describe their purpose.
- Example:
  ```csharp
  public void LoadSettings()
  {
      // Implementation here
  }
  ```

#### **UI Elements**

- Use **Hungarian Notation** for UI element names:
    - Prefix with the control type, followed by a descriptive name.
        - Examples:
            - TextBox: `TxtCustomerName`
            - Button: `BtnSubmit`
            - DataGrid: `DgOrderList`
    - Make sure all the properties are following the order in the example below
        - Example:
       ```xaml
       <!-- Open Repository Button -->
       <ComboBox x:Name="CmbLanguage"
                 Grid.Column="1"
                 AutomationProperties.Name="{DynamicResource SettingsPage_LanguageSelector}"
                 AutomationProperties.HelpText="{DynamicResource SettingsPage_LanguageSelectorTooltip}"
                 DisplayMemberPath="NativeName"
                 SelectedValuePath="Name"
                 HorizontalAlignment="Center"
                 VerticalAlignment="Center"
                 MinWidth="150"
                 SelectionChanged="CmbLanguage_SelectionChanged" />
       ```
        - **Order**: Name of the element, Grid.Column/Grid.Row…, alphabetically ordered styles, events

#### **Variables and Fields**

- **Private fields**: Use `_camelCase`.
    - Example: `_titleId`.
- **Local variables**: Use `camelCase`.
    - Example: `titleId`.
- **Public properties**: Use **PascalCase**.
    - Example: `TitleId`.

---

### **Coding Standards**

1. **File Organization**:
    - Make sure that all the logic is in the "Library" project of Xenia Manager (XeniaManager), so it's easier to
      implement features in both Desktop App and Fullscreen App (When it comes)

2. **Project Structure**:
    - Make sure to use ViewModel for storing variables for UI elements while the rest of the logic for the UI is in the designated `.cs` file

3. **Commenting**:
    - Use XML comments for public methods and classes:
      ```csharp
       /// <summary>
       /// Used to emulate a WaitForCloseAsync function that is similar to the Process Class has
       /// </summary>
       /// <returns></returns>
       public Task WaitForCloseAsync()
       {
         return closeWindowCheck.Task;
       }
      ```

4. **Error Handling**:
    - Always use `try-catch` blocks to handle exceptions and log them appropriately using `Logger`
    - When something is not implemented, throw an `Exception`

5. **Formatting**:
    - Use spaces (not tabs) with an indentation size of four  spaces.
    - Place opening braces `{` on a new line.

---

### **Submitting Changes**

1. **Create a Branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Write Meaningful Commits**:
    - Example:
      ```bash
      git commit -m "[Feature/Bugfix].."
      ```

3. **Submit a Pull Request**:
    - Push your branch and open a pull request to the `dev` branch.
    - Link to any related issues and provide a clear description of the changes.

---

Thank you for contributing to this project!

--- 