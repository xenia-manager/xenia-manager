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
        changelog.append(f"# [{release['name']}]\n")
        changelog.append(f"{release['body']}\n")
    return "\n".join(changelog)

def save_file(content, filename):
    with open(filename, 'w') as file:
        file.write(content)

def main():
    repo = os.getenv('GITHUB_REPOSITORY')
    releases = get_releases(repo)
    changelog = compile_changelog(releases)
    
    # Save the changelog to .md and .txt files
    save_file(changelog, 'CHANGELOG.md')
    # Convert markdown to plain text (simple conversion by removing markdown syntax)
    changelog_txt = changelog.replace('# ', '').replace('\n', ' ').strip()
    save_file(changelog_txt, 'CHANGELOG.txt')

if __name__ == "__main__":
    main()
