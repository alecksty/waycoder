import os, subprocess, sys, time
REPO = "/Users/shitanyu/Desktop/source/mycoder/my-coder"
sys.path.insert(0, REPO + "/scripts/maui-vml-verify")
from driver import Driver
HERE = REPO + "/.scratch/landscape-check"
d = Driver(serial="emulator-5554")

def rot(n, w=4.0):
    d.sh("shell", "settings", "put", "system", "accelerometer_rotation", "0")
    d.sh("shell", "settings", "put", "system", "user_rotation", str(n))
    time.sleep(w)

def probe(label):
    time.sleep(4)
    out = d.sh("logcat", "-d", "-s", "WC-DRAW")
    lines = [l for l in out.splitlines() if "host=" in l]
    print("\n===== %s =====" % label)
    for l in lines[-1:]:
        print("  " + l.split("WC-DRAW : ", 1)[-1])
    p = os.path.join(HERE, "final-%s.png" % label)
    open(p, "wb").write(subprocess.run(["adb", "-s", "emulator-5554", "exec-out", "screencap", "-p"],
                                       capture_output=True).stdout)
    print("  截图", os.path.basename(p))

probe("0-landscape-first")
rot(0); probe("1-portrait")
rot(1); probe("2-landscape-back")
# 收起手柄
for n in d.nodes():
    if n["text"] == "▲ 收起手柄":
        d.sh("shell", "input", "tap", *d._center(n)); break
probe("3-landscape-collapsed")
