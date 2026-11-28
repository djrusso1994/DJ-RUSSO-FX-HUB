from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageSequence

root = Path(__file__).resolve().parents[1]
screens = root / "screenshots"
source = screens / "suite-showcase-v0.8.45.gif"
target = screens / "suite-showcase-v0.8.46-10slides.gif"
with Image.open(source) as old:
    assert old.size == (1120, 760) and old.n_frames == 9
    frames = [f.convert("RGB").copy() for f in ImageSequence.Iterator(old)]
bg = frames[0].getpixel((0, 0))
accent, muted, rail = (111,231,245), (185,181,207), (62,48,101)
fontpath = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
font = ImageFont.truetype(fontpath,13)
title = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",25)
sub = ImageFont.truetype(fontpath,15)
# The final slide uses the actual developer screenshot, not a rendered mock-up.
canvas = Image.new("RGB", (1120,760), bg)
with Image.open(screens / "auto-master-x-main.png") as new:
    slide = ImageOps.contain(new.convert("RGB"), (1056,605), Image.Resampling.LANCZOS)
canvas.paste(slide, ((1120-slide.width)//2, 90+(605-slide.height)//2))
draw = ImageDraw.Draw(canvas)
draw.text((32,28), "AUTO MASTER X  v1.4.0",font=title,fill=accent)
draw.text((32,62), "INTELLIGENT MASTERING ENGINE 2.0",font=sub,fill=muted)
frames.append(canvas)
# Repaint the complete original footer on every frame, including the tenth.
for i, frame in enumerate(frames):
    d = ImageDraw.Draw(frame)
    d.rectangle((0,721,1119,759), fill=bg)
    d.text((32,737),"NEON GHOST · FX HUB",font=font,fill=muted,anchor="lm")
    d.text((1088,737),f"{i+1:02d} / 10",font=font,fill=accent,anchor="rm")
    start = (1120 - (9*22+17))//2
    for j in range(10):
        x=start+j*22
        d.rounded_rectangle((x,733,x+17,737),radius=2,fill=accent if i==j else rail)
pal=[f.quantize(colors=256,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.FLOYDSTEINBERG) for f in frames]
pal[0].save(target,save_all=True,append_images=pal[1:],duration=[1900]*9+[3000],loop=0,disposal=2,optimize=False)
with Image.open(target) as check:
    assert check.n_frames==10 and check.size==(1120,760)
    for i, frame in enumerate(ImageSequence.Iterator(check)):
        rgb=frame.convert("RGB")
        start=(1120-(9*22+17))//2
        for j in range(10):
            actual=rgb.getpixel((start+j*22+8,735))
            expected=accent if j==i else rail
            assert sum(abs(a-b) for a,b in zip(actual,expected)) < 70, (i,j,actual,expected)
    assert all(f.info["duration"] == (1900 if i<9 else 3000) for i,f in enumerate(ImageSequence.Iterator(check)))
readme=root / "README.md"
text=readme.read_text(encoding="utf-8")
text=text.replace("screenshots/suite-showcase-v0.8.46.gif","screenshots/suite-showcase-v0.8.46-10slides.gif")
readme.write_text(text,encoding="utf-8")
print("Verified 10 slides, corrected 01 / 10 through 10 / 10 and ten progress segments.")
