#!/usr/bin/env bash
# Low-token Unity interact CLI for PolyPets / Cursor agents.
# Requires the project open in Unity Editor (bridge polls Temp/PolyPetsAgent/cmd.txt).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
AGENT_DIR="$ROOT/Temp/PolyPetsAgent"
CMD_FILE="$AGENT_DIR/cmd.txt"
STATUS_FILE="$AGENT_DIR/status.json"
LINES="${UNITY_LOG_LINES:-40}"

mkdir -p "$AGENT_DIR"

usage() {
  cat <<'EOF'
Usage: bash scripts/unity-interact.sh <command>

  status          Print Temp/PolyPetsAgent/status.json (tiny)
  play|stop|pause|unpause
  refresh         Assets → Refresh (reload assets)
  ping            Bridge alive check
  setup           Run FirstTimeSetupBatch (editor must be open)
  focus-game|focus-console
  log [N]         Tail last N lines of Editor.log (default 40)
  errors [N]      Tail log filtered to error/exception (default 60 scanned)
  wait-ready [S]  Poll status until not compiling (default 90s)
  watch           Tail status.json changes

Token rule: prefer status + errors over screenshots.
EOF
}

send_cmd() {
  local c="$1"
  printf '%s\n' "$c" > "$CMD_FILE"
  # Give the editor a moment to consume.
  sleep 0.5
  if [[ -f "$STATUS_FILE" ]]; then
    cat "$STATUS_FILE"
    echo
  else
    echo "{\"ok\":false,\"hint\":\"Open Unity on this project; bridge writes status.json\"}"
  fi
}

find_editor_log() {
  # Prefer project-local log (Unity 6+), then macOS global, then Linux.
  local candidates=(
    "$ROOT/Logs/Editor.log"
    "$HOME/Library/Logs/Unity/Editor.log"
    "$HOME/.config/unity3d/Editor.log"
  )
  local f
  for f in "${candidates[@]}"; do
    if [[ -f "$f" ]]; then
      echo "$f"
      return 0
    fi
  done
  return 1
}

cmd="${1:-}"
case "$cmd" in
  ""|-h|--help|help) usage; exit 0 ;;
  status)
    if [[ -f "$STATUS_FILE" ]]; then cat "$STATUS_FILE"; echo
    else echo '{"ok":false,"error":"no status yet — is Unity open on this project?"}'; fi
    ;;
  play|stop|pause|unpause|resume|refresh|reload|assets|ping|setup|focus-game|focus-console)
    # normalize aliases
    case "$cmd" in reload|assets) cmd=refresh ;; resume) cmd=unpause ;; esac
    send_cmd "$cmd"
    ;;
  log)
    n="${2:-$LINES}"
    log="$(find_editor_log)" || { echo "Editor.log not found"; exit 1; }
    echo "# $log (last $n)"
    tail -n "$n" "$log"
    ;;
  errors)
    n="${2:-60}"
    log="$(find_editor_log)" || { echo "Editor.log not found"; exit 1; }
    echo "# errors from $log"
    tail -n "$n" "$log" | grep -iE 'error|exception|assert|failed' || echo "(none in last $n lines)"
    ;;
  wait-ready)
    max="${2:-90}"
    end=$((SECONDS + max))
    while (( SECONDS < end )); do
      if [[ -f "$STATUS_FILE" ]]; then
        if grep -q '"compiling":false' "$STATUS_FILE" && grep -q '"updating":false' "$STATUS_FILE"; then
          cat "$STATUS_FILE"; echo; exit 0
        fi
      fi
      sleep 0.5
    done
    echo '{"ok":false,"error":"timeout waiting for compile"}'
    [[ -f "$STATUS_FILE" ]] && cat "$STATUS_FILE" && echo
    exit 1
    ;;
  watch)
    if command -v fswatch >/dev/null 2>&1; then
      fswatch -o "$STATUS_FILE" | while read -r _; do cat "$STATUS_FILE"; echo; done
    else
      prev=""
      while true; do
        cur=""
        [[ -f "$STATUS_FILE" ]] && cur="$(cat "$STATUS_FILE")"
        if [[ "$cur" != "$prev" ]]; then
          echo "$cur"
          prev="$cur"
        fi
        sleep 0.5
      done
    fi
    ;;
  *)
    echo "Unknown command: $cmd" >&2
    usage
    exit 1
    ;;
esac
