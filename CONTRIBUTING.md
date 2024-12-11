## **Contributing Guide**

Welcome, and thank you for your interest in contributing to Xenia Manager. Please follow these guidelines to maintain code quality and consistency across the project.

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
      -  Examples:
         - TextBox: `TxtCustomerName`
         - Button: `BtnSubmit`
         - DataGrid: `DgOrderList`
   - Make sure all of the properties are following the order in the example below
      - Example:
      ```xaml
      <!-- Open Repository Button -->
      <Button x:Name="BtnRepository"
            Grid.Column="0"
            AutomationProperties.Name="Open Repository Button"
            Content="&#xED15;"
            HorizontalAlignment="Left"
            Margin="21,0,0,0"
            Style="{StaticResource TitleBarButton}"
            Click="BtnRepository_Click">
            <Button.ToolTip>
               <TextBlock TextAlignment="Left">
                  Opens the repository page in the default web browser
               </TextBlock>
            </Button.ToolTip>
      </Button>
      ```
      - **Order**: Name of the element, Grid.Column/Grid.Row.., alphabetically ordered styles, events

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
   - Each class should be in its own folder.
      - Example: A `GameManager` class should be in `GameManager` folder.
   - Make sure that all of the logic is in the "Library" project of Xenia Manager (XeniaManager) so it's easier to implement features in both Desktop App and Fullscreen App (When it comes)

2. **Project Structure**:
   - Organize related files into folders by their type (e.g., Window/Page):
      - `Properties.cs` should include all variables and the constructor
      - `Functions.cs` should contain custom functions
      - `WindowName.xaml.cs` should handle all UI-related event logic
   - Organize classes into dedicated folders:
      - `Properties/Base.cs` should define all variables and constructors
      - The `Models` folder should house all "model" classes
      - The `Functions` folder should contain functions specific to each class

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
   - Always use `try-catch` blocks to handle exceptions and log them appropriately using Serilog.

5. **Formatting**:
   - Use spaces (not tabs) with an indentation size of 4 spaces.
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