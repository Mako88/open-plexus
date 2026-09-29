#!/usr/bin/env bash
# bAbI tasks 1-20, en-valid test split, one JSON row per question with its story so far.
# The original tarball's host no longer serves it; this is the same data as HELM builds it.
set -euo pipefail
cd "$(dirname "$0")/../data/babi"
curl -sL -o babi_test.jsonl \
  "https://huggingface.co/datasets/Muennighoff/babi/resolve/main/babi_test.jsonl"
curl -sL -o babi_train.jsonl \n  "https://huggingface.co/datasets/Muennighoff/babi/resolve/main/babi_train.jsonl"
