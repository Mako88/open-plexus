# Open Plexus

An attempt at a language model that approaches a frontier model and runs on one old consumer
GPU, by keeping knowledge and memory outside the weights. A small frozen model does the
language; a store on disk holds what it has read and been told; a small trained piece reads
and writes the store. Learning a new fact is appending a row. See `docs/plan.md`.
