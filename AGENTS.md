# AGENTS.md

## Cursor Cloud specific instructions

This repository is **documentation-only**. It currently contains just:

- `README.md`
- `docs/GAME_DESIGN_DOCUMENT.md` (the PolyPets game design document)

There is **no application code, dependency manifest, build system, test suite, or lint config** yet. The "product" it describes — a Unity (Windows) desktop pet game called PolyPets — has not been implemented. Do not try to install language/runtime dependencies, run a build, or start a Unity project; none exist.

### Working in this repo
- The only development activity today is authoring/editing Markdown docs. No update/install step is required — `git` plus any Markdown-capable editor is enough.
- Keep internal doc links relative (e.g. `README.md` links to `docs/GAME_DESIGN_DOCUMENT.md`).

### Previewing the docs (optional)
To view the Markdown rendered as HTML locally, render with any Markdown tool and serve statically. One quick approach (installs a one-off tool, not needed for normal work):

```bash
pip install markdown
python3 -m markdown docs/GAME_DESIGN_DOCUMENT.md > /tmp/gdd.html
python3 -m http.server 8080   # then open http://localhost:8080
```

### When code is added later
Once real application code (e.g. a Unity project or tooling) lands, update this section with the actual build/test/run commands and revisit the startup update script accordingly.
