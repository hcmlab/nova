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
    "pdf",
    outputfile="index.pdf",
    extra_args=[
        "-s",
        "--number-sections",
        "--toc",
        "--toc-depth=4",
        "--template=templates/default.latex",
        "-V", "documentclass=scrartcl",
    ],
)
PY

echo "Wrote index.pdf"
