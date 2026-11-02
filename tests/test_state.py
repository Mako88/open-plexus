"""The transcript renders as the slot's own tokens, the saved slot is addressed by the server's
slots endpoint, a turn is pinned to its slot and read for what the server had to process, and the
comparison names the same reply and the gap."""

import json
import sys
import threading
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.state import EMPTY_THOUGHT, SlotState, render, same_state, turn  # noqa: E402

SEEN = []


class Fake(BaseHTTPRequestHandler):
    def log_message(self, *_):
        pass

    def do_POST(self):
        body = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        SEEN.append((self.path, body))
        if self.path.startswith("/slots/"):
            action = self.path.split("action=")[1]
            out = {"id_slot": 0, "filename": body["filename"], {"save": "n_saved", "restore": "n_restored"}.get(action, "n_erased"): 1234}
        else:
            out = {"content": " Tim Burton", "stop_type": "eos", "tokens_predicted": 3, "tokens_evaluated": 7,
                   "completion_probabilities": [{"token": " Tim", "logprob": -0.25,
                                                 "top_logprobs": [{"token": " Tim", "logprob": -0.25}, {"token": " Ed", "logprob": -1.5}]}],
                   "timings": {"prompt_n": 7, "prompt_ms": 12.5, "predicted_n": 3}}
        data = json.dumps(out).encode()
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)


def test_the_transcript_renders_every_past_assistant_turn_after_the_empty_thinking_block():
    messages = [{"role": "system", "content": "Answer."}, {"role": "user", "content": "Who directed Ed Wood?"},
                {"role": "assistant", "content": "Tim Burton"}, {"role": "user", "content": "When?"}]
    assert render(messages) == ("<|im_start|>system\nAnswer.<|im_end|>\n<|im_start|>user\nWho directed Ed Wood?<|im_end|>\n"
                                f"<|im_start|>assistant\n{EMPTY_THOUGHT}Tim Burton<|im_end|>\n<|im_start|>user\nWhen?<|im_end|>\n"
                                f"<|im_start|>assistant\n{EMPTY_THOUGHT}")
    # the rendering before a turn is a prefix of the rendering after it: the slot's tokens, extended
    before = render(messages[:2])
    after = render(messages)
    assert after.startswith(before + "Tim Burton<|im_end|>\n")


def test_the_slot_is_saved_restored_and_a_turn_is_pinned_to_it_and_read_for_its_cost():
    server = HTTPServer(("127.0.0.1", 0), Fake)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    url = f"http://127.0.0.1:{server.server_port}"
    try:
        state = SlotState("conversation-3.bin", url=url, slot=0)
        assert state.save() == 1234 and state.restore() == 1234
        state.erase()
        assert [p for p, _ in SEEN] == ["/slots/0?action=save", "/slots/0?action=restore", "/slots/0?action=erase"]
        assert all(b == {"filename": "conversation-3.bin"} for _, b in SEEN)
        t = turn([{"role": "user", "content": "who directed Ed Wood?"}], url=url, slot=0, max_new=8)
        path, body = SEEN[-1]
        assert path == "/completion" and body["id_slot"] == 0 and body["cache_prompt"] is True
        assert body["prompt"] == render([{"role": "user", "content": "who directed Ed Wood?"}])
        assert body["temperature"] == 0 and body["n_probs"] == 5 and body["n_predict"] == 8
        assert t["content"] == "Tim Burton" and t["prompt_n"] == 7 and t["prompt_ms"] == 12.5 and t["predicted_n"] == 3
        assert t["stop"] == "eos" and t["first_token"] == " Tim" and t["first_logprob"] == -0.25
        assert t["first_top"] == {" Tim": -0.25, " Ed": -1.5}
    finally:
        server.shutdown()


def test_the_comparison_names_the_same_reply_and_the_first_token_gap():
    a = {"content": "Tim Burton", "first_token": " Tim", "first_logprob": -0.25}
    assert same_state(a, dict(a)) == {"same_reply": True, "same_first_token": True, "logprob_gap": 0.0}
    b = {"content": "Ed Wood", "first_token": " Ed", "first_logprob": -0.75}
    assert same_state(a, b) == {"same_reply": False, "same_first_token": False, "logprob_gap": 0.5}
    assert same_state(a, {"content": "Tim Burton"})["logprob_gap"] is None
