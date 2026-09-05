#!/usr/bin/env bash
#
# Fetches the corpora the external worlds read. Nothing here is in the
# repository: the data is somebody else's, it is large, and a test that cannot
# find it says so and names this script.
#
# Run from anywhere:  bash corpora/fetch.sh
#
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ONE DOWNLOAD, WITH A HOST THAT CRAWLS TREATED AS A HOST THAT IS DOWN.
#
# WHY THIS EXISTS: CI died twice in a row here, thirty minutes each time, with
# curl's exit 28 -- `cs.toronto.edu` accepted the connection for CIFAR and then
# delivered almost nothing, so `--max-time 1800` elapsed in full. The suite never
# built, and nine commits sat unvalidated behind a stalled socket.
#
# A PLAIN TIMEOUT IS THE WRONG INSTRUMENT FOR THAT. It cannot tell a large file
# on a slow link from a dead transfer, so it has to be set long enough for the
# former and then pays that in full for the latter. `--speed-limit` asks the
# question directly: under ten kilobytes a second for a minute is not slow, it is
# stopped, and it gives up in a minute rather than in half an hour.
#
# AND THE RETRIES ARE WHAT MAKE A BLIP COST SECONDS. Three of them, so a
# transient refusal self-heals; `--retry-all-errors` because a stalled transfer
# is not in curl's default retry set, which is the exact case this is for.
#
# IT WOULD NOT HAVE CAUGHT THE FAILURE THAT PROMPTED IT, AND SAYING SO IS THE
# POINT. Measured after the fact, `cs.toronto.edu` was serving at about 106 kB/s
# -- slow, and ten times over this floor, so the transfer never looks stopped and
# this never fires. What it guards is a host that goes silent. The slow one is
# handled by the timeout below and by the cache, and properly by a mirror.
grab() {
  curl -sS -L     --connect-timeout 30     --speed-limit 10000 --speed-time 60     --retry 3 --retry-delay 5 --retry-all-errors     "$@"
}

# bAbI — Weston et al. 2015, "Towards AI-Complete Question Answering: A Set of
# Prerequisite Toy Tasks". CC BY 3.0, Copyright (c) 2015-present Facebook, Inc.
#
# The original download at research.fb.com is gone and the URL the HuggingFace
# loader still points at 404s. This mirror is the tarball Keras shipped against
# for years and is byte-identical in layout: tasks_1-20_v1-2/{en,en-10k,hn,...}.
babi_url="https://s3.amazonaws.com/text-datasets/babi_tasks_1-20_v1-2.tar.gz"
babi_dir="$here/tasks_1-20_v1-2"

if [ -d "$babi_dir/en" ]; then
  echo "bAbI: already at $babi_dir"
else
  echo "bAbI: fetching 11.7 MB from $babi_url"
  grab --max-time 300 -o "$here/babi.tar.gz" "$babi_url"
  tar -xzf "$here/babi.tar.gz" -C "$here"
  echo "bAbI: extracted to $babi_dir"
fi

echo
echo "en/     1,000 training stories per task, which is the harder published setting"
echo "en-10k/ 10,000, which is the setting most reported numbers use"
echo

# Tatoeba — the English sentence export. CC BY 2.0 FR, (c) Tatoeba contributors.
#
# WHY THIS IS HERE: six bAbI tasks score exactly nought because their answers --
# `yes`, `no`, `maybe`, the counting words -- never occur as WORDS in the corpus,
# only in the answer column, so there is no node for the walk to arrive at. This
# is the plain English that puts them in the graph.
#
# SHORT EVERYDAY SENTENCES RATHER THAN PROSE, deliberately. `yes` and `no` are
# things people say to each other; narrative uses them only inside dialogue, so a
# novel of the same size carries far fewer of them.
#
# The download is 25 MB compressed and 108 MB extracted, which is the largest
# thing here -- but only the first few tens of thousands of lines are ever read,
# and the export has no smaller published slice.
tatoeba_url="https://downloads.tatoeba.org/exports/per_language/eng/eng_sentences.tsv.bz2"
tatoeba_file="$here/tatoeba_eng.tsv"

if [ -f "$tatoeba_file" ]; then
  echo "Tatoeba: already at $tatoeba_file"
else
  echo "Tatoeba: fetching 25 MB from $tatoeba_url"
  grab --max-time 600 -o "$here/tatoeba_eng.tsv.bz2" "$tatoeba_url"
  bunzip2 -kf "$here/tatoeba_eng.tsv.bz2"
  echo "Tatoeba: extracted to $tatoeba_file"
fi

# CLEVR — Johnson et al. 2017, "CLEVR: A Diagnostic Dataset for Compositional
# Language and Elementary Visual Reasoning". CC BY 4.0, (c) 2017 Facebook, Inc.
#
# The no-images archive is 89 MB against 18 GB for the full one, and the images
# are of no use here: the scene graphs ship as JSON with every object's colour,
# size, shape and material already separated, which is the front end this
# architecture would otherwise have to fake.
#
# ONLY THE VALIDATION SPLIT IS EXTRACTED. Train questions alone are 712 MB and
# 700,000 questions, against 15,000 scenes and 150,000 questions in val — far
# more than anything here can get through, and the split means nothing to a
# system with no training phase.
clevr_url="https://dl.fbaipublicfiles.com/clevr/CLEVR_v1.0_no_images.zip"
clevr_dir="$here/CLEVR_v1.0"

if [ -f "$clevr_dir/scenes/CLEVR_val_scenes.json" ]; then
  echo "CLEVR: already at $clevr_dir"
else
  echo "CLEVR: fetching 89 MB from $clevr_url"
  grab --max-time 900 -o "$here/clevr.zip" "$clevr_url"
  unzip -o -q "$here/clevr.zip" -d "$here" \
    "CLEVR_v1.0/scenes/CLEVR_val_scenes.json" \
    "CLEVR_v1.0/questions/CLEVR_val_questions.json" \
    "CLEVR_v1.0/README.txt" "CLEVR_v1.0/LICENSE.txt" "CLEVR_v1.0/COPYRIGHT.txt"
  echo "CLEVR: extracted the validation split to $clevr_dir"
fi

# CLUTRR — Sinha et al. 2019, "CLUTRR: A Diagnostic Benchmark for Inductive
# Reasoning from Text". CC BY-NC 4.0, (c) 2019 Facebook, Inc.
#
# WHY THIS IS HERE: `Kind.Role` is the only mechanism in this design that can
# hold a fact naming no argument -- whatever fills one slot of a relation fills
# another slot of a different one -- and it has only ever been measured on cases
# built here. This is kinship composition on somebody else's data: `grandson`
# then `brother` is `grandson`, whoever the people are.
#
# NO LANGUAGE MODEL IS NEEDED AND NONE IS CLAIMED. The chain ships as columns --
# `story_edges` says which pairs are joined, `edge_types` says with what,
# `query_edge` says which pair is asked about -- exactly as CLEVR ships its scene
# graph. The English story is ignored.
#
# ONLY THE TEST SPLIT. It carries chains of two to ten hops where train carries
# two to three, so it holds both the short chains a rule can be learned from and
# the long ones that need the rule composed. The split means nothing to a system
# with no training phase; the chain length is what matters, and this is the file
# that has the range.
clutrr_url="https://raw.githubusercontent.com/kliang5/CLUTRR_huggingface_dataset/main/gen_train23_test2to10/test.csv"
clutrr_file="$here/clutrr_test.csv"

if [ -f "$clutrr_file" ]; then
  echo "CLUTRR: already at $clutrr_file"
else
  echo "CLUTRR: fetching 1.9 MB from $clutrr_url"
  grab --max-time 300 -o "$clutrr_file" "$clutrr_url"
  echo "CLUTRR: fetched to $clutrr_file"
fi

# CIFAR-10 — Krizhevsky 2009, "Learning Multiple Layers of Features from Tiny
# Images". Freely distributed by the University of Toronto.
#
# WHY THIS IS HERE, AND WHY CLEVR IS NOT ENOUGH. Step four is the only place the
# project's own bet gets measured, and it needs a world where the front end has
# to MAKE the symbols. Every corpus above ships its symbols already separated --
# CLEVR's scene graphs give every object's colour, size, shape and material as
# JSON, which is exactly the front end this architecture would otherwise have to
# fake. That was the right trade for the worlds above and it is precisely wrong
# here: an encoder has nothing to encode and "raw" has nothing to be raw about.
#
# THE IMAGES ARE THE POINT, SO THE NO-IMAGES SHORTCUT IS NOT AVAILABLE. CLEVR
# with pictures is 18 GB. CIFAR-10 is 162 MB, and it is what the fly-hash
# lineage was measured on -- Dasgupta, Stevens and Navlakha evaluate on data of
# this shape, so `Winnow` is being asked a question its source paper asked.
#
# AND IT HAS A PUBLISHED NUMBER FOR THE ARM TO SIT AGAINST: a linear probe on
# frozen CLIP ViT-B/32 features scores about 95% here. A raw-pixel `Winnow` that
# lands anywhere near that is the finding; one that does not is also a finding,
# and a cheaper one to get than 18 GB.
#
# THE BINARY DISTRIBUTION RATHER THAN THE PYTHON ONE. The `.pkl` version needs
# an interpreter to open; this one is fixed-width records -- one label byte then
# 3072 pixel bytes -- which C# reads with a `BinaryReader` and no dependency.
cifar_url="https://www.cs.toronto.edu/~kriz/cifar-10-binary.tar.gz"
cifar_dir="$here/cifar-10-batches-bin"

if [ -f "$cifar_dir/test_batch.bin" ]; then
  echo "CIFAR-10: already at $cifar_dir"
else
  echo "CIFAR-10: fetching 162 MB from $cifar_url"
  # AN HOUR, BECAUSE THIRTY MINUTES IS NOT ENOUGH AT THE RATE THIS HOST SERVES.
  # It was 1800 and CI spent every second of it twice before dying with exit 28;
  # 162 MB at the ~106 kB/s measured from two places is about twenty-five
  # minutes, which is inside 1800 only when nothing else goes wrong.
  #
  # THIS BUYS ONE SLOW RUN AND THE CACHE TAKES IT FROM THERE, so it is a way of
  # letting the cache get populated rather than a fix. The fix is a mirror, and
  # picking one is a decision about PROVENANCE -- the red-ball property rests on
  # every machine coding the same bytes, and this file's bAbI note is careful to
  # say its mirror is byte-identical in layout. Nobody should choose that quickly.
  grab --max-time 3600 -o "$here/cifar-10-binary.tar.gz" "$cifar_url"
  tar -xzf "$here/cifar-10-binary.tar.gz" -C "$here"
  echo "CIFAR-10: extracted to $cifar_dir"
fi

# ---------------------------------------------------------------------------
# THE ENCODERS, WHICH ARE NOT CORPORA AND LIVE HERE ANYWAY.
#
# They are somebody else's, they are large, and nothing in the repository should
# carry them -- which is the same argument the file opens with, so they get the
# same treatment rather than a second mechanism.
#
# WHAT THEY ARE FOR: the arm against raw. The bet is that `Winnow` -- a fixed
# random projection and a k-winners-take-all, no weights and no training --
# recovers enough of what a trained encoder buys. That claim is unfalsifiable
# without something trained to lose to, so these are the yardstick.
#
# FROZEN IS WHAT MAKES THEM LEGAL HERE. The red-ball property says two machines
# must agree about what they are looking at, and a published file of constants
# satisfies it exactly as `Winnow`'s arithmetic-derived wiring does: same file,
# same numbers, every machine, forever. An encoder that adapted during a run
# would be a codebook fitted to the data, which the property forbids outright.
#
# AND A FRONT END MAY SAY WHAT IT IS LOOKING AT, NEVER WHAT TO CONCLUDE. That
# rule is why the MobileNet graph gets cut below -- see the note there.
# ---------------------------------------------------------------------------

encoders="$here/encoders"

# CLIP ViT-B/32, vision tower only — Radford et al. 2021, "Learning Transferable
# Visual Models From Natural Language Supervision". Original weights MIT, (c)
# 2021 OpenAI; this ONNX export by Qdrant.
#
# THE STRONG ARM, AND THE EXPENSIVE ONE. 224x224x3 in, 512 floats out, ~88M
# constants, ~4.4 GFLOPs an image. Measured on an i7-4790 (2014, four cores):
# 46 ms an image on four threads, 101 ms on one, 416 MB resident.
#
# IT EMITS AN EMBEDDING AND NOT CLASS SCORES, which is why it needs no surgery.
clip_dir="$encoders/clip-vit-b32-vision"
clip_repo="https://huggingface.co/Qdrant/clip-ViT-B-32-vision/resolve/main"

if [ -f "$clip_dir/model.onnx" ]; then
  echo "CLIP: already at $clip_dir"
else
  echo "CLIP: fetching 352 MB from $clip_repo"
  mkdir -p "$clip_dir"
  for file in model.onnx config.json preprocessor_config.json; do
    grab --max-time 900 -o "$clip_dir/$file" "$clip_repo/$file"
  done
  echo "CLIP: fetched to $clip_dir"
fi

# MobileNetV3-Small — Howard et al. 2019, "Searching for MobileNetV3". Apache
# 2.0; these are the `timm` lamb_in1k weights, ONNX export by onnx-community.
#
# THE ARM THAT FITS THE BUDGET. Same measurement, same machine: 1.7 ms an image
# on four threads and 3.3 ms on one, against CLIP's 46 and 101. Thirty times
# cheaper on one core, and 6 MB against 352.
#
# THE FLOP RATIO OVERSTATES IT AND THE WALL CLOCK IS WHAT MATTERS. MobileNet is
# memory-bound and barely uses a second core (3.3 ms on one thread, 1.7 on
# four); CLIP is compute-bound and nearly halves. For twenty phones each running
# one encoder that is the good direction.
#
# DO NOT REACH FOR THE QUANTIZED BUILD WITHOUT MEASURING. `model_int8.onnx` in
# the same repository is TWENTY TIMES SLOWER than fp32 on the 2014 machine --
# 35-42 ms an image -- because Haswell has no VNNI and the int8 kernels fall
# through to a slow path. It would win on a phone with int8 silicon. That is the
# whole point: it is a fact about the target and not about the file.
mobilenet_dir="$encoders/mobilenetv3-small"
mobilenet_repo="https://huggingface.co/onnx-community/mobilenetv3_small_100.lamb_in1k/resolve/main"

if [ -f "$mobilenet_dir/model.onnx" ]; then
  echo "MobileNetV3: already at $mobilenet_dir"
else
  echo "MobileNetV3: fetching 10 MB from $mobilenet_repo"
  mkdir -p "$mobilenet_dir"
  grab --max-time 300 -o "$mobilenet_dir/model.onnx" "$mobilenet_repo/onnx/model.onnx"
  for file in config.json preprocessor_config.json; do
    grab --max-time 120 -o "$mobilenet_dir/$file" "$mobilenet_repo/$file"
  done
  echo "MobileNetV3: fetched to $mobilenet_dir"
fi

# AND THE PUBLISHED EXPORT ENDS AT A 1000-WAY CLASSIFIER, WHICH BREAKS THE RULE.
# A front end may say what it is looking at and never what to conclude, and a
# vector of ImageNet class scores is a conclusion -- it says "this is a red
# ball", which is the one thing forbidden. CLIP needs no such surgery because
# its vision tower emits an embedding by construction.
#
# SO THE GRAPH IS CUT ONE `Gemm` EARLY, at the 1024-d pooled features the
# classifier reads from. Nothing is retrained and nothing is chosen: the cut is
# at the layer boundary the architecture already has, and the output is renamed
# `features` so nothing downstream depends on a `timm` export's internal node
# name.
#
# IT NEEDS `onnx` AND SAYS SO RATHER THAN FAILING QUIETLY. This is the only step
# here that is not a download, and a machine without the package gets a named
# reason instead of a missing file.
headless="$mobilenet_dir/model_headless.onnx"

if [ -f "$headless" ]; then
  echo "MobileNetV3: headless encoder already at $headless"
elif python -c "import onnx" 2>/dev/null; then
  echo "MobileNetV3: cutting the classifier off"
  python - "$mobilenet_dir/model.onnx" "$headless" <<'CUT'
import sys
import onnx
from onnx.utils import extract_model

source, target = sys.argv[1], sys.argv[2]
extract_model(source, target, ['pixel_values'], ['/flatten/Flatten_output_0'])

model = onnx.load(target)
model.graph.output[0].name = 'features'
for node in model.graph.node:
    node.output[:] = ['features' if o == '/flatten/Flatten_output_0' else o
                      for o in node.output]
onnx.save(model, target)
onnx.checker.check_model(onnx.load(target))
CUT
  echo "MobileNetV3: headless encoder at $headless (1024-d 'features')"
else
  echo "MobileNetV3: SKIPPED the headless cut -- no python with the 'onnx'"
  echo "             package. Install it and re-run, or the cheap arm will be"
  echo "             stuck emitting 1000 class scores instead of a reading."
fi

# all-MiniLM-L6-v2, sentence encoder -- Reimers & Gurevych 2019,
# sentence-transformers. Apache 2.0; this ONNX export is the one published in the
# model repository.
#
# WHY A TEXT ENCODER: the two above are vision towers, and the spine world's
# things are WORDS. `Worded` asks it for one word at a time and `WordedTests`
# prices what it knows about the house's alphabet before any learner is asked to
# use it.
#
# NINETY MEGABYTES, WHICH IS THE FP32 EXPORT. There is an int8 build in the same
# repository at about a quarter the size. Do not switch without measuring:
# quantisation moves every vector slightly, and what is being asked is whether a
# direction through that space separates two kinds of word.
#
# THE VOCABULARY COMES WITH IT because nothing here splits word-pieces. A word is
# looked up whole and refused if the vocabulary does not hold it, which is a
# stated restriction rather than an approximation.
minilm_dir="$encoders/all-minilm-l6-v2"
minilm_repo="https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main"

if [ -f "$minilm_dir/model.onnx" ]; then
  echo "MiniLM: already at $minilm_dir"
else
  echo "MiniLM: fetching 90 MB from $minilm_repo"
  mkdir -p "$minilm_dir"
  grab --max-time 600 -o "$minilm_dir/model.onnx" "$minilm_repo/onnx/model.onnx"
  grab --max-time 120 -o "$minilm_dir/vocab.txt" "$minilm_repo/vocab.txt"
  echo "MiniLM: fetched to $minilm_dir"
fi
