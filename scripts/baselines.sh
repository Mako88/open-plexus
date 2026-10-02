#!/usr/bin/env bash
# Re-take the baselines phases 1, 2 and 4 of the red set need, after the house changed.
# About 1h45m on the 1080 Ti; it stops every llama-server and leaves the 0.8B up on 8094.
#   bash scripts/baselines.sh "what changed the house" > baselines.log 2>&1
set -u
cd /d/repos/unfused
HUB=/c/Users/John/.cache/huggingface/hub
M08=$HUB/models--bartowski--Qwen_Qwen3.5-0.8B-GGUF/snapshots/f36b1ea49a332ede8fe5f389bbf5b3575ef71f48/Qwen_Qwen3.5-0.8B-Q8_0.gguf
M2=$HUB/models--bartowski--Qwen_Qwen3.5-2B-GGUF/snapshots/7d26695454df6de5fbcce2e58681e62dae06ce43/Qwen_Qwen3.5-2B-Q8_0.gguf
M9=$HUB/models--bartowski--Qwen_Qwen3.5-9B-GGUF/snapshots/ff13963796ee209598509a81340172bb1c3869fe/Qwen_Qwen3.5-9B-Q6_K_L.gguf
SERVER=/d/tools/llama.cpp-src/build/bin/Release/llama-server.exe
NOTE="${1:?say what changed the house}"

say() { echo "[$(date +%H:%M)] $*"; }

stop_servers() {
  powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='llama-server.exe'\" | ForEach-Object { Stop-Process -Id \$_.ProcessId -Force }" >/dev/null 2>&1
  sleep 5
}

start_server() {  # model ctx port
  "$SERVER" -m "$1" -ngl 99 -c "$2" --parallel 1 --jinja --reasoning-budget 0 \
    --host 127.0.0.1 --port "$3" > "${TMPDIR:-/tmp}/llama-server-$3.log" 2>&1 &
  for _ in $(seq 1 120); do
    curl -sf "http://127.0.0.1:$3/health" >/dev/null 2>&1 && { say "server up on $3"; return 0; }
    sleep 5
  done
  say "SERVER FAILED on $3"; return 1
}

exam() {  # served port arms seed
  uv run python scripts/exam.py --faculty served --served "$1" --port "$2" --arms "$3" \
    --seed "$4" --note "$NOTE" 2>&1 | grep -E "^[a-z0-9]+ +score|Error|Traceback" | cut -c1-160
}

say "start"
for s in 1 2 3; do
  uv run python scripts/exam.py --arms blind --seed $s --note "$NOTE" 2>&1 | grep -E "score|Error" | cut -c1-120
done

stop_servers
start_server "$M08" 16384 8094 || exit 1
for s in 0 1 2 3; do exam Qwen3.5-0.8B-Q8_0 8094 full $s; done
exam Qwen3.5-0.8B-Q8_0 8094 linked 0

stop_servers
start_server "$M2" 16384 8094 || exit 1
exam Qwen3.5-2B-Q8_0 8094 full 0
exam Qwen3.5-2B-Q8_0 8094 linked 0

stop_servers
start_server "$M9" 8192 8093 || exit 1
for a in full recall recall2 linked; do exam Qwen3.5-9B-Q6_K_L 8093 $a 0; done

stop_servers
# leave the 0.8B as it was found
start_server "$M08" 8192 8094
say "BASELINES DONE"
