from PIL import Image, ImageDraw, ImageFont
from collections import deque
from pathlib import Path
import json, base64, io

NAMES=["auto-master-x","compressor-x","pitch-x","eq-x","reverb-x","delay-x","drive-x","stereo-x","transient-x","vocal-remover-x"]
ROOT=Path("Assets")
REPORT=Path(".logo-audit")
REPORT.mkdir(exist_ok=True)
canvas=Image.new("RGB",(1450,750),(21,28,55))
draw=ImageDraw.Draw(canvas)
font=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",19)
report=[]
for idx,name in enumerate(NAMES):
    im=Image.open(ROOT/(name+".png")).convert("RGBA")
    alpha=im.getchannel("A")
    mask=bytearray(1 if x>=32 else 0 for x in alpha.tobytes())
    w,h=im.size
    seen=bytearray(w*h)
    components=[]
    for start in range(w*h):
        if not mask[start] or seen[start]: continue
        q=[start];seen[start]=1;count=0;minx=w;miny=h;maxx=-1;maxy=-1
        for z in q:
            y,x=divmod(z,w);count+=1
            minx=min(minx,x);maxx=max(maxx,x);miny=min(miny,y);maxy=max(maxy,y)
            for dy in (-1,0,1):
                for dx in (-1,0,1):
                    if dx==0 and dy==0:continue
                    ny,nx=y+dy,x+dx
                    if 0<=ny<h and 0<=nx<w:
                        nz=ny*w+nx
                        if mask[nz] and not seen[nz]:seen[nz]=1;q.append(nz)
        components.append({"pixels":count,"box":[minx,miny,maxx+1,maxy+1]})
    components.sort(key=lambda x:x["pixels"],reverse=True)
    report.append({"name":name,"size":list(im.size),"alpha_bbox":alpha.getbbox(),
                   "components":components[:20],"n_components":len(components)})
    tile=(idx%5,idx//5)
    x0=tile[0]*290;y0=tile[1]*375
    draw.text((x0+14,y0+14),name,font=font,fill=(234,238,255))
    box=im.getbbox() or (0,0,w,h)
    thumb=im.crop(box);thumb.thumbnail((260,300),Image.Resampling.LANCZOS)
    region=Image.new("RGBA",(260,300),(31,39,78,255))
    region.alpha_composite(thumb,((260-thumb.width)//2,(300-thumb.height)//2))
    canvas.paste(region.convert("RGB"),(x0+15,y0+49))
data=io.BytesIO();canvas.save(data,format="JPEG",quality=89,optimize=True)
(REPORT/"contact.b64").write_text(base64.b64encode(data.getvalue()).decode("ascii"))
(REPORT/"report.json").write_text(json.dumps(report,ensure_ascii=False,indent=2))
print("Completed alpha audit for",len(report),"original files.")
for x in report:print(x["name"],x["size"],"components=",x["n_components"],"top=",x["components"][:8])
