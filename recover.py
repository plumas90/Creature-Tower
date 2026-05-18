import re

with open('scratch_normalstage.txt', 'r', encoding='utf-8') as f:
    data = f.read()

lines = []
capturing = False
for line in data.split('\n'):
    if 'The following code has been modified' in line:
        capturing = True
        continue
    if capturing and ('The above content shows the entire' in line or 'The above content does NOT show' in line):
        capturing = False
        break
    if capturing:
        # Some lines might have > before them if they were part of context
        line = line.replace('>', '', 1).strip() if line.startswith('>') else line.strip()
        m = re.match(r'^\s*(\d+):\s(.*)', line)
        if m:
            lines.append(m.group(2))
        elif len(line.strip()) == 0:
            pass # ignore empty
        else:
            # Handle lines that might have wrapped
            if len(lines) > 0:
                pass

if lines:
    with open('Assets/Script/Stage/NormalStage.cs', 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines))
    print(f'Recovered {len(lines)} lines.')
else:
    print('Failed to recover lines.')
