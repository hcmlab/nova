@echo off
cd /D "%~dp0"

pandoc -o index.pdf -s --number-sections --toc --toc-depth=4 --template=templates/default.latex -V documentclass=scrartcl index.md
