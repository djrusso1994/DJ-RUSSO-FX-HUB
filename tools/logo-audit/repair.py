"""Conservative cleanup of detached PNG fragments; leave existing artwork untouched."""
from PIL import Image, ImageDraw, ImageFilter, ImageChops, ImageFont
from pathlib import Path
import json, base64, io, subprocess

NAMES=["auto-master-x","compressor-x","pitch-x","eq-x","reverb-x","delay-x","drive-x","stereo-x","transient-x","vocal-remover-x"]
assets=Path("Assets")
audit=Path(".logo-audit");audit.mkdir(exist_ok=True)
sheet=Image.new("RGB",(1450,750),(21,28,55))
draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",19)
summary=[]
for i,name in enumerate(NAMES):
    path=assets/f"{name}.png"
    # Always regenerate from the verified release: repeated runs are byte-for-byte deterministic.
    original_png=subprocess.check_output(["git","show",f"v0.8.37:Assets/{name}.png"])
    im=Image.open(io.BytesIO(original_png)).convert("RGBA")
    w,h=im.size
    original_alpha=im.getchannel("A")
    alpha=original_alpha.tobytes()
    threshold=bytearray(1 if z>=32 else 0 for z in alpha)
    visited=bytearray(w*h)
    components=[]
    for start in range(w*h):
        if not threshold[start] or visited[start]:continue
        comp=[start];visited[start]=1
        x0=w;y0=h;x1=-1;y1=-1
        for pos in comp:
            y,x=divmod(pos,w)
            x0=min(x0,x);x1=max(x1,x);y0=min(y0,y);y1=max(y1,y)
            for dy in (-1,0,1):
                ny=y+dy
                if ny<0 or ny>=h:continue
                for dx in (-1,0,1):
                    nx=x+dx
                    if nx<0 or nx>=w or (dx==0 and dy==0):continue
                    nxt=ny*w+nx
                    if threshold[nxt] and not visited[nxt]:
                        visited[nxt]=1;comp.append(nxt)
        components.append((comp,(x0,y0,x1+1,y1+1)))
    components.sort(key=lambda c:len(c[0]),reverse=True)
    if not components:raise RuntimeError(f"Empty transparency data: {name}")
    largest,bbox=components[0]
    keep=bytearray(w*h)
    for pos in largest:keep[pos]=255
    removed=[]
    remove_mask=bytearray(w*h)
    for comp,box in components[1:]:
        x0,y0,x1,y1=box
        # Keep little internal highlights; exclude detached side scraps.
        interior=(x0>=bbox[0]+5 and x1<=bbox[2]-5
                  and y0>=bbox[1]+5 and y1<=bbox[3]-5)
        if interior:
            for pos in comp:keep[pos]=255
        else:
            for pos in comp:remove_mask[pos]=255
            removed.append({"pixels":len(comp),"box":list(box)})
    main_mask=Image.frombytes("L",(w,h),bytes(keep))
    # Keep original antialiasing around intact artwork, but exclude the faint
    # halos of discarded fragments too. Never erase a true main-logo pixel.
    soft_keep=main_mask.filter(ImageFilter.MaxFilter(9))
    discarded_mask=Image.frombytes("L",(w,h),bytes(remove_mask))
    discarded_halos=discarded_mask.filter(ImageFilter.MaxFilter(9))
    keep_im=ImageChops.lighter(ImageChops.subtract(soft_keep,discarded_halos),main_mask)
    new_alpha=ImageChops.multiply(original_alpha,keep_im)
    # Protect the original drawing and lettering: only alpha changes.
    new=im.copy();new.putalpha(new_alpha)
    cut_before=sum(z>=32 for z in alpha)
    cut_after=sum(z>=32 for z in new_alpha.tobytes())
    if cut_after < cut_before*.94:
        raise RuntimeError(f"Unsafe cleanup: {name}, lost >6% of opaque pixels")
    if new.size!=im.size:raise RuntimeError("Image size changed")
    new.save(path,format="PNG",optimize=True)
    summary.append({"name":name,"size":[w,h],"retained_opaque_ratio":round(cut_after/cut_before,6),
                    "detached_components_removed":removed})
    x0=(i%5)*290;y0=(i//5)*375
    draw.text((x0+15,y0+14),name,font=font,fill=(232,239,255))
    bbox2=new.getbbox() or (0,0,w,h)
    thumb=new.crop(bbox2);thumb.thumbnail((260,300),Image.Resampling.LANCZOS)
    tile=Image.new("RGBA",(260,300),(31,39,78,255))
    tile.alpha_composite(thumb,((260-thumb.width)//2,(300-thumb.height)//2))
    sheet.paste(tile.convert("RGB"),(x0+15,y0+49))
buf=io.BytesIO()
sheet.save(buf,format="JPEG",quality=87,optimize=True)
(audit/"contact-after.b64").write_text(base64.b64encode(buf.getvalue()).decode("ascii"))
(audit/"cleanup-report.json").write_text(json.dumps(summary,indent=2))
print("Restored ten 800x300 PNG source assets without changing any RGB pixels.")
for x in summary:
    print(f"{x['name']}: retained={x['retained_opaque_ratio']:.3%} removed={x['detached_components_removed']}")
