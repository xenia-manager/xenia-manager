import os
import requests
import re
import json

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
        if release['prerelease'] or "experimental" in release['tag_name'].lower() or "updater" in release['tag_name'].lower():
            continue
        changelog.append({
            "version": release["name"],
            "release_date": release["published_at"],
            "changes": parse_changes(release["body"])
        })
    return changelog

def parse_changes(body):
    changes = re.findall(r"\* (.+)", body)
    # Strip trailing whitespace from each change
    changes = [change.strip() for change in changes]
    return changes

def main():
    repo = os.getenv('GITHUB_REPOSITORY')
    if not repo:
        print("GITHUB_REPOSITORY environment variable is not set.")
        sys.exit(1)

    releases = get_releases(repo)
    changelog = compile_changelog(releases)
    
    # Writing the extracted information to a JSON file
    with open('changelog.json', 'w') as json_file:
        json.dump(changelog, json_file, indent=4)

    print("JSON file created successfully.")

if __name__ == "__main__":
    main()
