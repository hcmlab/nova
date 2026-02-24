#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

VENV_DIR=".venv-pandoc"

if [[ ! -d "$VENV_DIR" ]]; then
  python3 -m venv "$VENV_DIR"
fi

# shellcheck disable=SC1091
source "$VENV_DIR/bin/activate"

python -m pip install --upgrade pip >/dev/null
python -m pip install --quiet pypandoc-binary >/dev/null

python - <<'PY'
import pypandoc

pypandoc.convert_file(
    "index.md",
    "html",
    outputfile="index.html",
    extra_args=[
        "-s",
        "-f", "markdown",
        "--number-sections",
        "--template", "templates/standalone.html",
        "--css", "templates/template.css",
        "--toc",
        "--toc-depth=4",
        "--mathjax=https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-chtml-full.js",
    ],
)
PY

echo "Wrote index.html"
