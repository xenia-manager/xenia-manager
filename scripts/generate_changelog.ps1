param(
    [string]$LastCommitSha = "",
    [string]$Repository = "xenia-manager/xenia-manager"
)

try {
    $latestCommitSha = $LastCommitSha

    # Define commit titles to ignore
    $ignorePatterns = @(
        '^chore: Update translation progress chart$',
        '^chore: Update README',
        'ci:',
        '^docs?:',
        '^merge '
        # TODO: Add more patterns as needed
    )

    $changelog = ""
    if ($latestCommitSha -ne "") {
        $commits = git log --pretty=format:"%H|%s" "$latestCommitSha..HEAD"
        if ($commits) {
            $changelog = "## Changelog`n`n"
            foreach ($commit in $commits) {
                if ($commit.Trim() -eq "") { continue }
                $parts = $commit -split '\|', 2
                $commitHash = $parts[0]
                $commitTitle = $parts[1] -replace '\s*\(#\d+\)$', ''

                # Skip commits matching ignore patterns
                $shouldIgnore = $false
                foreach ($pattern in $ignorePatterns) {
                    if ($commitTitle -match $pattern) {
                        $shouldIgnore = $true
                        break
                    }
                }
                if ($shouldIgnore -or $commitTitle.Trim() -eq "") { continue }

                $shortCommitHash = $commitHash.Substring(0, 7)
                $commitUrl = "https://github.com/$Repository/commit/$commitHash"
                $changelog += "- **$commitTitle** ([$shortCommitHash]($commitUrl))`n"
            }
        } else {
            $changelog = "## Changelog`n`nNo new commits since last release.`n"
        }
    } else {
        $changelog = "## Changelog`n`nFirst release or unable to determine previous release.`n"
    }
    
    $changelog = $changelog.TrimEnd()
    
    # Output the changelog to GitHub Actions environment
    Write-Output "CHANGELOG<<EOF" >> $env:GITHUB_ENV
    Write-Output "$changelog" >> $env:GITHUB_ENV
    Write-Output "EOF" >> $env:GITHUB_ENV
    
    Write-Host "Changelog generated successfully"
} catch {
    Write-Host "Error generating changelog: $_"
    # Set empty changelog on error
    Write-Output "CHANGELOG<<EOF" >> $env:GITHUB_ENV
    Write-Output "" >> $env:GITHUB_ENV
    Write-Output "EOF" >> $env:GITHUB_ENV
}