"""The asking at inference: the trunk answers a question with one tool, `search(query)`, over a
store, in as many turns as it chooses up to a cap. `solve` drives any chat function that takes
OpenAI-style messages and a tool list and returns the assistant message (content and
`tool_calls`), which llama-server's chat endpoint does with `--jinja`, so the same loop reads
the asking on the served trunk with a trained adapter applied. The trunk is never told when to
search; the training reward taught it, or did not.
"""

from __future__ import annotations

import json

SEARCH_TOOL = {
    "type": "function",
    "function": {
        "name": "search",
        "description": "Search the collection for a passage. Returns the most relevant passage, with its title first.",
        "parameters": {
            "type": "object",
            "properties": {"query": {"type": "string",
                                     "description": "what to look for, a name or a short description of the fact needed"}},
            "required": ["query"],
        },
    },
}


def query_of(call: dict) -> str:
    """The query in a tool call, whatever shape the server returned its arguments in."""
    fn = call.get("function", call)
    args = fn.get("arguments", {})
    if isinstance(args, str):
        try:
            args = json.loads(args)
        except json.JSONDecodeError:
            return args
    return str(args.get("query", "")) if isinstance(args, dict) else str(args)


def solve(chat, system: str, question: str, search, max_turns: int = 4) -> dict:
    """The trunk's turns on one question. `chat(messages, tools)` returns the assistant
    message; `search(query)` is the store's answer. Returns {"reply": the final text,
    "searches": the queries asked, "turns": how many assistant turns, "messages": all}."""
    messages = [{"role": "system", "content": system}, {"role": "user", "content": question}]
    searches: list[str] = []
    reply = ""
    for turn in range(max_turns):
        msg = chat(messages, [SEARCH_TOOL])
        calls = msg.get("tool_calls") or []
        messages.append({"role": "assistant", "content": msg.get("content") or "", "tool_calls": calls} if calls
                        else {"role": "assistant", "content": msg.get("content") or ""})
        if not calls or turn == max_turns - 1:
            reply = (msg.get("content") or "").strip()
            break
        for call in calls:
            query = query_of(call)
            searches.append(query)
            messages.append({"role": "tool", "tool_call_id": call.get("id", f"call_{len(searches)}"),
                             "name": "search", "content": search(query)})
    return {"reply": reply, "searches": searches, "turns": sum(m["role"] == "assistant" for m in messages),
            "messages": messages}
