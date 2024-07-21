import os
import requests
import sys

def get_releases(repo):
    url = f"https://api.github.com/repos/{repo}/releases"
    headers = {"Authorization": f"token {os.getenv('GITHUB_TOKEN')}"}
    response = requests.get(url, headers=headers)
    # Check for rate limit issues
    if response.status_code == 403 and 'X-RateLimit-Remaining' in response.headers and int(response.headers['X-RateLimit-Remaining']) == 0:
        print("API rate limit exceeded.")
        sys.exit(1)  # Exit with a non-zero status to indicate failure
    response.raise_for_status()
    return response.json()

def compile_changelog(releases):
    changelog = []
    for release in releases:
        # Skip releases named "experimental" or "updater"
        if "experimental" in release['tag_name'].lower() or "updater" in release['tag_name'].lower():
            continue
        changelog.append(f"\\b {release['name']}\\b0\\par")
        changelog.append(f"{release['body']}\\par\\par")
    return "\n".join(changelog)

def main():
    repo = os.getenv('GITHUB_REPOSITORY')
    releases = get_releases(repo)
    changelog = compile_changelog(releases)
    
    # Start with the RTF header
    rtf_content = (
        "{\\rtf1\\ansi\\ansicpg1252\\deff0\\nouicompat\\deflang1033"
        "{\\fonttbl{\\f0\\fnil\\fcharset0 Calibri;}}"
        "{\\*\\generator Riched20 10.0.18362}\\viewkind4\\uc1 \n"
        "\\pard\\sa200\\sl276\\slmult1\\f0\\fs22\\lang9 "
        f"{changelog}"
        "}"

    )

    # Output the RTF content to standard output
    print(rtf_content)

if __name__ == "__main__":
    main()
