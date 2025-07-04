# Translation Tutorial

Thank you for helping translate our application! This guide will walk you through the process of adding or updating translations.

## Prerequisites

- A GitHub account
- Either **Git** OR **GitHub Desktop** installed on your computer 
- A text editor (Visual Studio, VS Code...)
- Basic familiarity with **GitHub**  and **Git**/**GitHub Desktop**

## Step 1: Fork and Clone the Repository

1. **Fork the repository**
   - Go to the main repository page on GitHub
   - Click the "Fork" button in the top-right corner
   - This creates a copy of the repository in your GitHub account

2. **Clone your fork locally**

   ### Option 1: Using Git (command-line)
   ```bash
   git clone https://github.com/your-username/your-repository-name.git
   cd your-repository-name
   ```

   ### Option 2: Using GitHub Desktop
   - Open GitHub Desktop
   - Go to `File` > `Clone Repository`
   - Select the forked repository from your list
   - Choose a local path to clone it to
   - Click **Clone**

## Step 2: Create a New Branch

### Option 1: Using Git
```bash
git checkout -b translation/COUNTRY-CODE
```

### Option 2: Using GitHub Desktop
- Click on the **Current Branch** menu > `New Branch`
- Name the branch: `translation/COUNTRY-CODE`
- Click **Create Branch**

Replace `COUNTRY-CODE` with your language/country code (e.g., `translation/es-ES`, `translation/fr-FR`, `translation/de-DE`).
You can find the codes [here](https://azuliadesigns.com/c-sharp-tutorials/list-net-culture-country-codes/). (CultureInfo Column)

## Step 3: Locate the Resources File

Navigate to the ***Language*** folder (Located inside of `sources\XeniaManager.Desktop\Resources\Language`) and find the appropriate .resx file:

- **For new translations**: Copy `Resources.resx` and rename it to `Resources.COUNTRY-CODE.resx`
  - Example: `Resources.es-ES.resx` for Spanish (Spain)
  - Example: `Resources.fr-FR.resx` for French (France)
  - Example: `Resources.de-DE.resx` for German (Germany)

- **For updating existing translations**: Open the existing `Resources.COUNTRY-CODE.resx` file

## Step 4: Edit the Translation File

1. **Open the .resx file** in your preferred editor
   - Visual Studio: Double-click the file to open the resource editor
   - VS Code: Install the "[ResX Editor](https://marketplace.visualstudio.com/items?itemName=TimHeuer.resx-editor)" extension for better support

2. **Understanding the file structure**
   ```xml
   <data name="MainWindow_About" xml:space="preserve">
    <value>About</value>
   </data>
   ```

3. **Translate the values**
   - Only change the text inside `<value>` tags
   - Keep the `name` attribute unchanged
   - Preserve any placeholders like `{0}`, `{1}`, etc.

## Step 5: Translation Guidelines

### Important Rules
- **DO NOT** change the `name` attributes (these are the keys used in code)
- **DO NOT** remove or add new entries without discussion
- **DO** preserve formatting placeholders (`{0}`, `{1}`, `\n`, etc.)
- **DO** maintain the same tone and style throughout

### Examples

**Good:**
```xml
<data name="MessageBox_RemoveGameText" xml:space="preserve">
    <value>Do you want to remove {0}?</value>
</data>
```

**Bad (missing placeholder):**
```xml
<data name="MessageBox_RemoveGameText" xml:space="preserve">
    <value>Do you want to remove?</value>
</data>
```

### Special Characters and Formatting
- Use proper Unicode characters for your language
- If you want to add a new line to the text you can add `\n` or just add the new line to the text value

## Step 6: Test Your Translation (Optional but Recommended)

If you can build the project locally:

1. **Enable your language in the code**
   - Find the `_supportedLanguages` array in the code (found in `source\XeniaManager.Desktop\Utilities\LocalizationHelper.cs`)
   - Uncomment the line for your language or add it if it doesn't exist:
   ```csharp
   /// <summary>
   /// Array of all the supported languages
   /// </summary>
   private static readonly CultureInfo[] _supportedLanguages =
   [
       _defaultLanguage, // English
       new CultureInfo("hr-HR"), // Croatian
       new CultureInfo("ja-JP"), // Japanese/日本語 (uncomment this line)
       new CultureInfo("de-DE"), // Deutsche (uncomment this line)
       new CultureInfo("fr-FR"), // Français (uncomment this line)
       new CultureInfo("es-ES"), // Español
       // ... uncomment or add your language code here
   ];
   ```

2. **Build the project** to ensure no syntax errors
3. **Run the application** and switch to your language
4. **Check that all strings display correctly** and fit in the UI

**Note:** Don't commit the changes to the `_supportedLanguages` array - this will be handled by the maintainers when your translation is ready for release.

## Step 7: Commit Your Changes

### Option 1: Using Git
```bash
git add sources/XeniaManager.Desktop/Resources/Language/Resources.COUNTRY-CODE.resx
git commit -m "Add [Language Name] translation (COUNTRY-CODE)"
```

Example:
```bash
git commit -m "Add Croatian translation (hr-HR)"
```

### Option 2: Using GitHub Desktop
- Go to the **Changes** tab
- Make sure your `.resx` file is selected
- Write a clear commit message (e.g., `Add Croatian translation (hr-HR)`)
- Click **Commit to translation/COUNTRY-CODE**

## Step 8: Push and Create Pull Request

### Option 1: Using Git
```bash
git push origin translation/COUNTRY-CODE
```

### Option 2: Using GitHub Desktop
- Click the **Push origin** button in the top bar

Then:

1. Go to your fork on GitHub
2. Click "Compare & pull request"
3. Use a descriptive title: `"Add [Language] translation"` or `"Update [Language] translation"`
4. In the description, mention:
   - Which language you're translating to
   - Any questions or notes about specific translations
   - Your native language proficiency level

## Step 9: Review Process

- A maintainer will review your translation
- You may be asked to make changes or clarifications
- Once approved, your translation will be merged into the main project
- You'll be credited as a contributor!
