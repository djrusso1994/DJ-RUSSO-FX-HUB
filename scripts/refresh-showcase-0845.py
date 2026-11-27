from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageSequence

root = Path("screenshots")
target = root / "suite-showcase.gif"
with Image.open(target) as source:
    assert source.size == (1120, 760), source.size
    assert source.n_frames == 8, "Unexpected previous showcase; refusing to append duplicate slides"
    frames = [f.convert("RGB").copy() for f in ImageSequence.Iterator(source)]
    delay = source.info.get("duration", 1900)
    loop = source.info.get("loop", 0)

bg = frames[0].getpixel((0, 0))
accent = (111, 231, 245)
muted = (185, 181, 207)
white = (245, 246, 255)
rail = (62, 48, 101)
fontdir = Path("/usr/share/fonts/truetype/dejavu")
def font(size, bold=False):
    return ImageFont.truetype(str(fontdir / ("DejaVuSans-Bold.ttf" if bold else "DejaVuSans.ttf")), size)

slides = [
    ("stereo-x-phase-monitor.png", "Phase monitor · live spectrum"),
    ("stereo-x-vectorscope.png", "Vectorscope · stereo analysis"),
]
for filename, subtitle in slides:
    canvas = Image.new("RGB", (1120, 760), bg)
    d = ImageDraw.Draw(canvas)
    d.text((32, 18), "SPATIAL · VST3", font=font(13), fill=accent)
    d.text((32, 36), "STEREO X", font=font(29, True), fill=white)
    d.text((1088, 58), subtitle, font=font(16), fill=muted, anchor="rm")
    d.line((32, 79, 1088, 79), fill=(86, 61, 138), width=1)
    with Image.open(root / filename) as screenshot:
        # Keep the actual interface, including its proportions and text.
        shot = ImageOps.contain(screenshot.convert("RGB"), (1054, 610), Image.Resampling.LANCZOS)
    x = (1120 - shot.width) // 2
    y = 93 + (610 - shot.height) // 2
    d.rounded_rectangle((x-2, y-2, x+shot.width+1, y+shot.height+1), radius=5, outline=(160, 105, 226), width=1)
    canvas.paste(shot, (x, y))
    frames.append(canvas)

# Refresh only the original footer navigation, leaving the old screenshots intact.
total = len(frames)
for i, frame in enumerate(frames):
    d = ImageDraw.Draw(frame)
    d.rectangle((0, 721, 1120, 759), fill=bg)
    d.text((32, 737), "NEON GHOST · FX HUB", font=font(13), fill=muted, anchor="lm")
    d.text((1088, 737), f"{i+1:02d} / {total:02d}", font=font(13), fill=accent, anchor="rm")
    step, width = 22, 17
    start = (1120 - ((total-1)*step + width)) // 2
    for j in range(total):
        x = start + j*step
        d.rounded_rectangle((x, 733, x+width, 737), radius=2, fill=accent if j == i else rail)

# Keep the animated GIF compact and avoid modifying the source screenshot files.
paletted = [f.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG) for f in frames]
tmp = root / ".suite-showcase-new.gif"
paletted[0].save(tmp, save_all=True, append_images=paletted[1:], duration=delay, loop=loop, disposal=2, optimize=False)
with Image.open(tmp) as check:
    assert check.size == (1120, 760)
    assert check.n_frames == 10
    assert check.info["duration"] == delay
    print(f"Verified {check.n_frames} frames, {delay} ms each, {check.size}, {tmp.stat().st_size} bytes")
tmp.replace(target)
