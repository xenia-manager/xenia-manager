import os
import sys
import requests
import pypandoc

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
        changelog.append(f"# [{release['name']}]\n")
        changelog.append(f"{release['body']}\n")
    return "\n".join(changelog)

def main():
    repo = os.getenv('GITHUB_REPOSITORY')
    releases = get_releases(repo)
    changelog_md = compile_changelog(releases)
    
    # Convert Markdown to RTF
    changelog_rtf = pypandoc.convert_text(changelog_md, 'rtf', format='md')
    
    # Output the RTF content to a file
    with open('CHANGELOG.rtf', 'w') as file:
        file.write(changelog_rtf)

if __name__ == "__main__":
    main()
