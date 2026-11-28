from pathlib import Path
from PIL import Image, ImageOps, ImageSequence

root = Path(__file__).resolve().parents[1]
screens = root / "screenshots"
source = screens / "suite-showcase-v0.8.45.gif"
target = screens / "suite-showcase-v0.8.46.gif"
with Image.open(source) as old:
    size = old.size
    frames = []
    durations = []
    for frame in ImageSequence.Iterator(old):
        frames.append(frame.convert("RGB").copy())
        durations.append(frame.info.get("duration", 2500))
    assert len(frames) == 9, f"Unexpected original frame count: {len(frames)}"
with Image.open(screens / "auto-master-x-showcase.gif") as new:
    slide = ImageOps.contain(new.convert("RGB"), size, Image.Resampling.LANCZOS)
canvas = Image.new("RGB", size, "#101321")
canvas.paste(slide, ((size[0] - slide.width)//2, (size[1] - slide.height)//2))
frames.append(canvas)
durations.append(3000)
frames[0].save(target, save_all=True, append_images=frames[1:], duration=durations, loop=0, disposal=2, optimize=True)
with Image.open(target) as check:
    assert check.n_frames == 10, f"Expected ten frames, got {check.n_frames}"
    assert check.size == size
readme = root / "README.md"
text = readme.read_text(encoding="utf-8")
text = text.replace("screenshots/suite-showcase-v0.8.45.gif", "screenshots/suite-showcase-v0.8.46.gif").replace("nine interface slides including STEREO X", "ten interface slides including AUTO MASTER X").replace("Nine real interface slides · Neon Ghost · HUB v0.8.43 and plugin captures including STEREO X v1.2.0", "Ten real interface slides · Neon Ghost · HUB v0.8.43 and plugin captures including STEREO X v1.2.0 and AUTO MASTER X v1.4.0")
readme.write_text(text, encoding="utf-8")
print(f"Verified suite GIF: {target}, ten frames, {target.stat().st_size} bytes")
