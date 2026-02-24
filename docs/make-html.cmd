@echo off
cd /D "%~dp0"

pandoc -o index.html -s -f markdown --number-sections --template templates/standalone.html --css templates/template.css --toc --toc-depth=4 --mathjax=https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-chtml-full.js index.md