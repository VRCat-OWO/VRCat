import io

p = r"C:\Users\Administrator\Desktop\源码\VRChat改模教程\app\Content5.cs"
s = io.open(p, encoding="utf-8-sig").read()

depth = 0
i = 0
line = 1
in_str = False
esc = False
line_depth_at_start = {}
marks = []
n = len(s)
while i < n:
    c = s[i]
    if c == "\n":
        line_depth_at_start[line + 1] = depth
        line += 1
        i += 1
        continue
    if in_str:
        if esc:
            esc = False
        elif c == "\\":
            esc = True
        elif c == '"':
            in_str = False
        i += 1
        continue
    if c == '"':
        in_str = True
    elif c == "/" and i + 1 < n and s[i + 1] == "/":
        # 行注释
        j = s.find("\n", i)
        i = n if j < 0 else j
        continue
    elif c == "(":
        depth += 1
    elif c == ")":
        depth -= 1
        if depth < 0:
            marks.append(("NEGATIVE", line))
    i += 1

lines = s.split("\n")
for idx, text in enumerate(lines, 1):
    t = text.strip()
    if t.startswith("static Category Chapter") or t.startswith("static Category Resources") or t.startswith("internal static Category") or t.startswith("namespace") or t.startswith("internal static class"):
        print(f"line {idx:5} depth_at_start={line_depth_at_start.get(idx, '?')}  {t[:60]}")
print("final depth:", depth)
print("negative marks:", marks[:5])
