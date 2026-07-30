#!/usr/bin/env bash
# Regenerates the "Episódios" section of README.md and the docs/index.html
# GitHub Pages listing, based on the repos cloned into episodios/.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EPISODES_DIR="$ROOT_DIR/episodios"
README="$ROOT_DIR/README.md"
DOCS_DIR="$ROOT_DIR/docs"
GITHUB_USER="carlosmachel"

get_title() {
  grep -m1 '^# ' "$1" 2>/dev/null | sed -E 's/^# *//'
}

get_description() {
  awk '
    /^# / { seen=1; next }
    seen && /^[[:space:]]*$/ { next }
    seen && /^!\[/ { next }
    seen && /^#/ { next }
    seen { gsub(/^[[:space:]]+|[[:space:]]+$/, ""); print; exit }
  ' "$1" 2>/dev/null
}

html_escape() {
  sed -e 's/&/\&amp;/g' -e 's/</\&lt;/g' -e 's/>/\&gt;/g' -e 's/"/\&quot;/g'
}

strip_markdown() {
  sed -E \
    -e 's/\[([^]]+)\]\([^)]+\)/\1/g' \
    -e 's/\*\*([^*]+)\*\*/\1/g' \
    -e 's/`([^`]+)`/\1/g'
}

mapfile -t episodes < <(
  find "$EPISODES_DIR" -mindepth 1 -maxdepth 1 -type d -name 'maf-video-*' -exec basename {} \; \
    | sort -t'-' -k3 -n
)

readme_items=()
card_items=()
missing_readme=()

for name in "${episodes[@]}"; do
  episode_readme="$EPISODES_DIR/$name/README.md"
  title="$name"
  description="Confira o repositório do episódio."

  if [ -s "$episode_readme" ]; then
    t="$(get_title "$episode_readme")"
    d="$(get_description "$episode_readme")"
    [ -n "$t" ] && title="$t"
    [ -n "$d" ] && description="$d"
  elif [ -f "$episode_readme" ]; then
    missing_readme+=("$name (README.md vazio)")
  else
    missing_readme+=("$name (sem README.md)")
  fi

  ep_num="$(echo "$name" | sed -E 's/^maf-video-0*([0-9]+)$/\1/')"
  ep_label="Ep. $(printf '%02d' "$ep_num")"

  repo_url="https://github.com/$GITHUB_USER/$name"

  readme_items+=("- **$ep_label — [$title]($repo_url)** — $description")

  title_html="$(printf '%s' "$title" | strip_markdown | html_escape)"
  description_html="$(printf '%s' "$description" | strip_markdown | html_escape)"
  card_items+=("      <article class=\"card\">
        <span class=\"ep-num\">$ep_label</span>
        <h2>$title_html</h2>
        <p>$description_html</p>
        <a class=\"repo-link\" href=\"$repo_url\" target=\"_blank\" rel=\"noopener noreferrer\">Ver repositório original ↗</a>
      </article>")
done

if [ "${#missing_readme[@]}" -gt 0 ]; then
  echo "⚠️  Episódios sem README (título usa o nome da pasta):"
  printf '   - %s\n' "${missing_readme[@]}"
fi

# ---- README.md: replace/insert the EPISODES block ----
section_file="$(mktemp)"
{
  echo "<!-- EPISODES:START -->"
  echo "## 🎬 Episódios"
  echo
  printf '%s\n' "${readme_items[@]}"
  echo "<!-- EPISODES:END -->"
} > "$section_file"

if grep -q '<!-- EPISODES:START -->' "$README" 2>/dev/null; then
  awk -v section_file="$section_file" '
    BEGIN { while ((getline line < section_file) > 0) section = section line "\n" }
    /<!-- EPISODES:START -->/ { printf "%s", section; skip=1; next }
    /<!-- EPISODES:END -->/ { skip=0; next }
    skip { next }
    { print }
  ' "$README" > "$README.tmp"
  mv "$README.tmp" "$README"
else
  {
    echo
    cat "$section_file"
  } >> "$README"
fi
rm -f "$section_file"

# ---- docs/index.html ----
mkdir -p "$DOCS_DIR"
touch "$DOCS_DIR/.nojekyll"

{
  cat <<HTML
<!doctype html>
<html lang="pt-br">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>MAF AzureBrasil Hub</title>
  <style>
    :root { color-scheme: light dark; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
      max-width: 960px;
      margin: 0 auto;
      padding: 2rem 1.25rem 4rem;
      line-height: 1.5;
    }
    header { margin-bottom: 2rem; }
    header a { color: inherit; }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
      gap: 1rem;
    }
    .card {
      position: relative;
      border: 1px solid rgba(127, 127, 127, 0.35);
      border-radius: 10px;
      padding: 1rem 1.25rem;
    }
    .ep-num {
      display: inline-block;
      font-size: 0.75rem;
      font-weight: 700;
      letter-spacing: 0.03em;
      opacity: 0.6;
      margin-bottom: 0.35rem;
    }
    .card h2 { font-size: 1.05rem; margin: 0 0 0.5rem; }
    .card p { margin: 0 0 0.75rem; opacity: 0.85; font-size: 0.92rem; }
    .repo-link { font-size: 0.9rem; font-weight: 600; text-decoration: none; }
    .repo-link:hover { text-decoration: underline; }
    footer { margin-top: 3rem; font-size: 0.85rem; opacity: 0.7; }
  </style>
</head>
<body>
  <header>
    <h1>MAF AzureBrasil Hub</h1>
    <p>Hub com o código de cada vídeo da playlist do Microsoft Agent Framework no canal AzureBrasil.Cloud.</p>
    <p><a href="https://github.com/$GITHUB_USER/maf-azurebrasil-hub" target="_blank" rel="noopener noreferrer">Repositório do hub ↗</a></p>
  </header>
  <main>
    <div class="grid">
HTML
  printf '%s\n' "${card_items[@]}"
  cat <<HTML
    </div>
  </main>
  <footer>
    Gerado automaticamente pela GitHub Action de sincronização.
  </footer>
</body>
</html>
HTML
} > "$DOCS_DIR/index.html"
