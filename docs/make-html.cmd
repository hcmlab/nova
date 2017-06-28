@echo off
cd /D "%~dp0"

pandoc -o index.html -s -f markdown --number-sections --template templates/standalone.html --css templates/template.css --toc --toc-depth=4 --latexmathml index.md