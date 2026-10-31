#!/usr/bin/env bash
# Uso: Tools/run-tests.sh EditMode|PlayMode [filtro]
# Requer o editor do Unity fechado. Exit 0 = passou.
set -u
PLATFORM=${1:-EditMode}
FILTER=${2:-}
UNITY="/c/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe"
ROOT="$(cd "$(dirname "$0")/.." && pwd -W)"
mkdir -p "$ROOT/Logs"
ARGS=(-batchmode -projectPath "$ROOT" -runTests -testPlatform "$PLATFORM"
      -testResults "$ROOT/Logs/$PLATFORM.xml" -logFile "$ROOT/Logs/$PLATFORM.log")
[ -n "$FILTER" ] && ARGS+=(-testFilter "$FILTER")
"$UNITY" "${ARGS[@]}"
CODE=$?
XML="$ROOT/Logs/$PLATFORM.xml"
if [ -f "$XML" ]; then
  grep -o '<test-run[^>]*>' "$XML" | grep -o '\(total\|passed\|failed\|skipped\)="[0-9]*"' | tr '\n' ' '; echo
  grep -B1 -A4 'result="Failed"' "$XML" | grep -E 'name=|<message>' | sed 's/^ *//' | head -40
else
  echo "Sem XML de resultado. Erros de compilacao:"; grep -E "error CS|Scripts have compiler errors|Aborting" "$ROOT/Logs/$PLATFORM.log" | sort -u | head -30
fi
echo "exit=$CODE"
exit $CODE
