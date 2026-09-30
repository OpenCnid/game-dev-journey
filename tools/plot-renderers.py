"""Plot committed renderer evidence. Requires matplotlib (analysis only, not Unity)."""
import json
from pathlib import Path
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

root = Path(__file__).resolve().parents[1]
evidence = root / "docs/playtests/evidence/renderers"
data = json.loads((evidence / "full-analysis.json").read_text(encoding="utf-8"))
modes = ["native", "indirect-cube", "characters", "combined"]
labels = ["Native-state\nbaseline", "Compact indirect\nprojectiles\n(cubes)", "Combined\ncharacter\nmeshes", "Combined\nconfiguration"]
colors = ["#ef6a54", "#3976b5", "#9864b7", "#168b83"]
plt.rcParams.update({"font.family": "DejaVu Sans", "font.size": 10, "axes.spines.top": False,
    "axes.spines.right": False, "axes.labelcolor": "#23344a", "text.color": "#23344a"})
fig, axes = plt.subplots(1, 2, figsize=(12, 5.5))
for axis, key, title in zip(axes, ["p99Ms", "fps"], ["Slow frames: p99 (lower is better)", "Average FPS (higher is better)"]):
    for i, mode in enumerate(modes):
        values = [row[key] for row in data if row["run"].split("-", 1)[1] == mode]
        axis.plot([i, i], [min(values), max(values)], color=colors[i], linewidth=3, alpha=.4)
        axis.scatter([i - .09, i, i + .09], values, s=48, color=colors[i], zorder=3)
        label = f"{min(values):.2f}–{max(values):.2f}" if key == "p99Ms" else f"{min(values):.0f}–{max(values):.0f}"
        axis.annotate(label, (i, max(values)), xytext=(0, 12), textcoords="offset points", ha="center", fontsize=10, fontweight="bold")
    axis.set_xticks(range(4), labels)
    axis.set_title(title, loc="left", fontsize=12, pad=15)
    axis.set_xlim(-.5, 3.5); axis.set_ylim(0, 14 if key == "p99Ms" else 530)
    axis.grid(axis="y", alpha=.18); axis.set_axisbelow(True)
    axis.set_ylabel("Milliseconds" if key == "p99Ms" else "Frames per second")
axes[0].axhline(1000 / 144, color="#168b83", linestyle="--", linewidth=1.2)
axes[0].text(3.42, 1000 / 144 + .2, "144 FPS budget · 6.94 ms", color="#168b83", fontsize=9, ha="right")
fig.suptitle("Cheaper rendering at 20,000 active bosses", x=.065, ha="left", fontsize=18, fontweight="bold", y=.97)
fig.text(.065, .89, "i7-10700K / RTX 3090 · 1920×1080 · uncapped · native-state Burst simulation", fontsize=11)
fig.text(.065, .035, "Each dot: one 60-second release capture after 30-second warm-up. Three repeats per configuration.\nSynchronized procedural boss workload; not a final-game or older-hardware guarantee.", fontsize=9)
fig.subplots_adjust(left=.065, right=.98, bottom=.23, top=.79, wspace=.22)
fig.savefig(evidence / "comparison.png", dpi=180, facecolor="white")
plt.close(fig)
print("Saved comparison.png with matplotlib", matplotlib.__version__)
