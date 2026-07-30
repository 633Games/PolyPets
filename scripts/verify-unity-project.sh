#!/usr/bin/env bash
# Quick sanity check that this folder is ready to open in Unity Hub.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

fail=0
check() {
  if [[ -e "$1" ]]; then
    echo "OK  $1"
  else
    echo "MISS $1"
    fail=1
  fi
}

echo "PolyPets Unity project check — $ROOT"
check Assets
check Packages/manifest.json
check ProjectSettings/ProjectVersion.txt
check Assets/Scripts/Editor/PolyPetsFirstRun.cs
check Assets/Scripts/Editor/PolyPetsSceneBootstrap.cs
check Assets/Shaders/PolyPetsCelShade.shader

ver=$(tr -d '\r' < ProjectSettings/ProjectVersion.txt | awk -F': ' '/m_EditorVersion:/{print $2; exit}')
echo "Pinned editor: ${ver:-unknown}"
if [[ "$ver" != 6000.3.* ]]; then
  echo "WARN expected Unity 6000.3.x"
fi

if rg -q 'com.unity.render-pipelines.universal' Packages/manifest.json; then
  echo "OK  URP in manifest"
else
  echo "MISS URP package"
  fail=1
fi

if rg -q 'com.unity.inputsystem' Packages/manifest.json; then
  echo "OK  Input System in manifest (First-Time Setup sets Active Input Handling → Both)"
fi

echo
if [[ "$fail" -eq 0 ]]; then
  echo "Ready for Unity Hub → Open → this folder"
  echo "Editor: Unity 6.3 LTS ($ver)"
  echo "Then: PolyPets → ★ First-Time Setup (run this)  OR click Run setup on the welcome dialog"
  exit 0
fi
echo "Project incomplete — pull latest from the setup branch"
exit 1
