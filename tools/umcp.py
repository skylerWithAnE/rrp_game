"""Tiny client for the MCP for Unity HTTP server.

usage:
  py umcp.py list                      -> tool names + descriptions
  py umcp.py schema <tool>             -> input schema of one tool
  py umcp.py resources                 -> resource URIs
  py umcp.py read <uri>                -> read a resource
  py umcp.py call <tool> '<json args>' -> call a tool (args may be @file.json)
"""
import json
import os
import sys
import urllib.request

URL = "http://127.0.0.1:8080/mcp"
INSTANCES = {"rrp_game": "rrp_game@79b035feb8361b3d"}
HEAD = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}


def post(body, session=None, timeout=180):
    head = dict(HEAD)
    if session:
        head["mcp-session-id"] = session
    req = urllib.request.Request(URL, data=json.dumps(body).encode(), headers=head, method="POST")
    with urllib.request.urlopen(req, timeout=timeout) as r:
        sid = r.headers.get("mcp-session-id")
        text = r.read().decode("utf-8", "replace")
    out = None
    for line in text.splitlines():
        if line.startswith("data:"):
            try:
                msg = json.loads(line[5:].strip())
            except ValueError:
                continue
            if "id" in msg and ("result" in msg or "error" in msg):
                out = msg
    if out is None and text.strip().startswith("{"):
        out = json.loads(text)
    return out, sid


def session():
    _, sid = post({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
        "protocolVersion": "2025-03-26", "capabilities": {},
        "clientInfo": {"name": "claude-code-http", "version": "0"}}})
    try:
        post({"jsonrpc": "2.0", "method": "notifications/initialized"}, sid)
    except Exception:
        pass
    # several editors can be connected (Multiplayer Play Mode clones); pick one. Default: main editor.
    inst = os.environ.get("UMCP_INST", "rrp_game")
    if "@" not in inst and inst not in INSTANCES:
        r, _ = post({"jsonrpc": "2.0", "id": 8, "method": "resources/read", "params": {"uri": "mcpforunity://instances"}}, sid)
        for c in r["result"]["contents"]:
            for i in json.loads(c["text"])["instances"]:
                if i["name"] == inst or (inst.isdigit() and i["name"] != "rrp_game"):
                    INSTANCES.setdefault(i["name"], i["id"])
        if inst.isdigit():  # "1", "2", "3": the nth clone by name
            clones = sorted(k for k in INSTANCES if k != "rrp_game")
            inst = clones[int(inst) - 1]
    post({"jsonrpc": "2.0", "id": 9, "method": "tools/call", "params": {
        "name": "set_active_instance", "arguments": {"instance": INSTANCES.get(inst, inst)}}}, sid)
    return sid


def main():
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    cmd = sys.argv[1]
    sid = session()
    if cmd == "list":
        r, _ = post({"jsonrpc": "2.0", "id": 2, "method": "tools/list"}, sid)
        for t in r["result"]["tools"]:
            print(t["name"], "-", (t.get("description") or "").strip().splitlines()[0][:150])
    elif cmd == "schema":
        r, _ = post({"jsonrpc": "2.0", "id": 2, "method": "tools/list"}, sid)
        for t in r["result"]["tools"]:
            if t["name"] == sys.argv[2]:
                print(t.get("description"))
                print(json.dumps(t["inputSchema"], indent=1))
    elif cmd == "resources":
        r, _ = post({"jsonrpc": "2.0", "id": 2, "method": "resources/list"}, sid)
        for t in r["result"]["resources"]:
            print(t["uri"], "-", t.get("name"))
    elif cmd == "read":
        r, _ = post({"jsonrpc": "2.0", "id": 2, "method": "resources/read", "params": {"uri": sys.argv[2]}}, sid)
        for c in r.get("result", r).get("contents", [r]):
            print(c.get("text", c) if isinstance(c, dict) else c)
    elif cmd == "code":
        # py umcp.py code <file.cs | -> : run a C# method body in the editor (- reads stdin)
        src = sys.stdin.read() if sys.argv[2] == "-" else open(sys.argv[2], encoding="utf-8").read()
        r, _ = post({"jsonrpc": "2.0", "id": 2, "method": "tools/call", "params": {
            "name": "execute_code", "arguments": {"action": "execute", "code": src, "safety_checks": False}}}, sid)
        res = r.get("result", r)
        for c in res.get("content", [res]):
            print(c.get("text", c) if isinstance(c, dict) else c)
    elif cmd == "call":
        raw = sys.argv[3] if len(sys.argv) > 3 else "{}"
        if raw.startswith("@"):
            raw = open(raw[1:], encoding="utf-8").read()
        r, _ = post({"jsonrpc": "2.0", "id": 2, "method": "tools/call",
                     "params": {"name": sys.argv[2], "arguments": json.loads(raw)}}, sid)
        res = r.get("result", r)
        if isinstance(res, dict) and "content" in res:
            for c in res["content"]:
                print(c.get("text", c))
        else:
            print(json.dumps(res, indent=1))


main()
