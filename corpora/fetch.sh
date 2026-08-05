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
  curl -sS -L --max-time 300 -o "$here/babi.tar.gz" "$babi_url"
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
  curl -sS -L --max-time 600 -o "$here/tatoeba_eng.tsv.bz2" "$tatoeba_url"
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
  curl -sS -L --max-time 900 -o "$here/clevr.zip" "$clevr_url"
  unzip -o -q "$here/clevr.zip" -d "$here" \
    "CLEVR_v1.0/scenes/CLEVR_val_scenes.json" \
    "CLEVR_v1.0/questions/CLEVR_val_questions.json" \
    "CLEVR_v1.0/README.txt" "CLEVR_v1.0/LICENSE.txt" "CLEVR_v1.0/COPYRIGHT.txt"
  echo "CLEVR: extracted the validation split to $clevr_dir"
fi
