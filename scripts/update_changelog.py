import os
import requests

def get_releases(repo):
    url = f"https://api.github.com/repos/{repo}/releases"
    headers = {"Authorization": f"token {os.getenv('GITHUB_TOKEN')}"}
    response = requests.get(url, headers=headers)
    response.raise_for_status()
    return response.json()

def compile_changelog(releases):
    changelog = []
    for release in releases:
        # Skip releases named "experimental" or "updater"
        if "experimental" in release['name'].lower() or "updater" in release['name'].lower():
            continue
        changelog.append(f"## {release['name']} - {release['published_at']}\n")
        changelog.append(f"{release['body']}\n")
    return "\n".join(changelog)

def main():
    repo = os.getenv('GITHUB_REPOSITORY')
    releases = get_releases(repo)
    changelog = compile_changelog(releases)
    print(changelog)

if __name__ == "__main__":
    main()
