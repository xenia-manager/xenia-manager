param(
    [string]$LanguageDir = "source/XeniaManager/Resources/Language"
)

try {
    # Check if language directory exists
    if (-not (Test-Path $LanguageDir)) {
        Write-Host "[FATAL] Language directory not found: $LanguageDir"
        exit 1
    }

    # Get all .axaml files in the language directory
    $axamlFiles = Get-ChildItem -Path $LanguageDir -Filter "*.axaml"
    if ($axamlFiles.Count -eq 0) {
        Write-Host "[FATAL] No .axaml files found in language directory"
        exit 1
    }

    # Find the main English file (reference for total strings)
    $mainFile = $axamlFiles | Where-Object { $_.Name -eq "en.axaml" }
    if (-not $mainFile) {
        Write-Host "[FATAL] Main en.axaml file not found in language directory"
        exit 1
    }

    # Function to parse axaml file and count strings
    function Get-TranslationCount {
        param(
            [string]$FilePath,
            [bool]$IsMainFile
        )
        try {
            $content = Get-Content -Path $FilePath -Raw
            $xml = [xml]$content
            # Count all sys:String elements
            $nodes = $xml.ResourceDictionary.GetElementsByTagName("sys:String")

            if ($IsMainFile) {
                # For main file, return total count
                return $nodes.Count
            }
            else {
                # For translation files, count only non-empty values that are not marked as #NOTTRANSLATED#
                $count = 0
                foreach ($node in $nodes) {
                    if ($node.InnerText -and $node.InnerText.Trim().Length -gt 0) {
                        $trimmedText = $node.InnerText.Trim()
                        if ($trimmedText -ne '#NOTTRANSLATED#') {
                            $count++
                        }
                    }
                }
                return $count
            }
        }
        catch {
            Write-Host "[ERROR] Failed to parse: $FilePath - $($_.Exception.Message)"
            return 0
        }
    }

    # Get total strings from English file
    $totalStrings = Get-TranslationCount -FilePath $mainFile.FullName -IsMainFile $true
    Write-Host "[OK] Total strings to translate: $totalStrings"

    # Process all translation files and calculate progress
    $translations = @{}
    foreach ($file in $axamlFiles) {
        $basename = $file.BaseName

        $langCode = $basename
        $translatedStrings = Get-TranslationCount -FilePath $file.FullName -IsMainFile $false

        if ($totalStrings -gt 0) {
            $percentage = [math]::Round(($translatedStrings / $totalStrings) * 100)
        }
        else {
            $percentage = 0
        }

        $translations[$langCode] = @{
            Translated = $translatedStrings
            Total      = $totalStrings
            Percentage = $percentage
        }

        Write-Host "[OK] $langCode`: $translatedStrings/$totalStrings ($percentage%)"
    }

    if ($translations.Count -eq 0) {
        Write-Host "[FATAL] No translations found"
        exit 1
    }

    # Generate JSON output for the Node.js chart generator
    $output = @{
        TotalStrings = $totalStrings
        Translations = $translations
    } | ConvertTo-Json -Depth 3

    # Output JSON to stdout for the workflow to capture
    Write-Output $output
}
catch {
    Write-Host "[FATAL] Error: $($_.Exception.Message)"
    exit 1
}
