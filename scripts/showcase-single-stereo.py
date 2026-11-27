from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageSequence
root = Path("screenshots")
source = root / "suite-showcase.gif"
with Image.open(source) as gif:
    assert gif.size == (1120,760) and gif.n_frames == 10
    delay = gif.info["duration"]
    loop = gif.info.get("loop",0)
    frames = [f.convert("RGB").copy() for i,f in enumerate(ImageSequence.Iterator(gif)) if i != 8]
assert len(frames) == 9
bg = frames[0].getpixel((0,0))
accent, muted, rail = (111,231,245), (185,181,207), (62,48,101)
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",13)
for i, frame in enumerate(frames):
    d = ImageDraw.Draw(frame)
    d.rectangle((0,721,1119,759), fill=bg)
    d.text((32,737),"NEON GHOST · FX HUB",font=font,fill=muted,anchor="lm")
    d.text((1088,737),f"{i+1:02d} / 09",font=font,fill=accent,anchor="rm")
    start = (1120 - (8*22+17))//2
    for j in range(9):
        x=start+j*22
        d.rounded_rectangle((x,733,x+17,737),radius=2,fill=accent if i==j else rail)
out=root / "suite-showcase-v0.8.45.gif"
pal=[f.quantize(colors=256,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.FLOYDSTEINBERG) for f in frames]
pal[0].save(out,save_all=True,append_images=pal[1:],duration=delay,loop=loop,disposal=2,optimize=False)
with Image.open(out) as check:
    assert check.n_frames==9 and check.size==(1120,760)
    assert all(f.info["duration"]==1900 for f in ImageSequence.Iterator(check))
print("Verified 9 frames, 1900 ms each; kept Vectorscope, removed Phase Monitor.")
