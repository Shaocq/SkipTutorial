# -*- coding: utf-8 -*-
"""生成「跳过新手教程引导」的 mod 预览图（512×512）。

用法（在「跳过新手引导」工程目录下）：
    python tools/make_preview.py

设计：深蓝底 + 金色标题，中间用三个流程块表达
「序章/捏脸（保留） → 新手教程（跳过） → 基地 +3000」。
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

# 与法宝之力预览图一致的深蓝金配色
BG_TOP = (30, 48, 80)
BG_BOTTOM = (9, 15, 28)
GOLD = (214, 179, 104)
GOLD_BRIGHT = (241, 216, 156)
WHITE_WARM = (246, 234, 206)
SUB_COLOR = (150, 172, 208)
DIM = (120, 140, 172)
CHIP_BG = (26, 42, 70)
CHIP_BORDER = (86, 118, 158)
CHIP_DIM_BG = (22, 32, 48)
CHIP_DIM_BORDER = (70, 88, 112)

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "preview.png"

S = 2
W = H = 512 * S

FONT_BOLD = Path(r"C:\Windows\Fonts\msyhbd.ttc")
FONT_REG = Path(r"C:\Windows\Fonts\msyh.ttc")


def font(size: int, bold: bool = True) -> ImageFont.FreeTypeFont:
    path = FONT_BOLD if bold else FONT_REG
    try:
        return ImageFont.truetype(str(path), size)
    except OSError:
        return ImageFont.truetype(r"C:\Windows\Fonts\simhei.ttf", size)


def background() -> Image.Image:
    img = Image.new("RGBA", (W, H))
    draw = ImageDraw.Draw(img)
    for y in range(H):
        t = y / (H - 1)
        color = tuple(int(BG_TOP[i] + (BG_BOTTOM[i] - BG_TOP[i]) * t) for i in range(3))
        draw.line([(0, y), (W, y)], fill=color + (255,))

    # 中心柔光
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    cx, cy, r = W // 2, int(H * 0.42), int(W * 0.42)
    for i in range(18, 0, -1):
        alpha = int(4 + (18 - i) * 0.6)
        rr = int(r * i / 18)
        gd.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=(70, 110, 170, alpha))
    glow = glow.filter(ImageFilter.GaussianBlur(60 * S // 2))
    return Image.alpha_composite(img, glow)


def centered(draw: ImageDraw.ImageDraw, y: int, text: str, f: ImageFont.FreeTypeFont, fill):
    box = draw.textbbox((0, 0), text, font=f)
    draw.text(((W - (box[2] - box[0])) // 2, y), text, font=f, fill=fill)


def chip(draw: ImageDraw.ImageDraw, x: int, y: int, w: int, h: int,
         title: str, sub: str, dim: bool = False, strike: bool = False):
    bg = CHIP_DIM_BG if dim else CHIP_BG
    border = CHIP_DIM_BORDER if dim else CHIP_BORDER
    draw.rounded_rectangle([x, y, x + w, y + h], radius=14 * S, fill=bg + (255,),
                           outline=border + (255,), width=2 * S)

    f_title = font(22 * S, bold=True)
    f_sub = font(14 * S, bold=False)
    title_fill = DIM + (255,) if dim else WHITE_WARM + (255,)
    sub_fill = DIM + (255,) if dim else SUB_COLOR + (255,)

    tb = draw.textbbox((0, 0), title, font=f_title)
    draw.text((x + (w - (tb[2] - tb[0])) // 2, y + 12 * S), title, font=f_title, fill=title_fill)

    sb = draw.textbbox((0, 0), sub, font=f_sub)
    draw.text((x + (w - (sb[2] - sb[0])) // 2, y + h - 24 * S), sub, font=f_sub, fill=sub_fill)

    if strike:
        draw.line([x + 14 * S, y + h // 2 + 2 * S, x + w - 14 * S, y + h // 2 - 6 * S],
                  fill=GOLD + (255,), width=3 * S)


def arrow(draw: ImageDraw.ImageDraw, x1: int, x2: int, y: int, dim: bool = False):
    color = DIM + (255,) if dim else GOLD + (255,)
    draw.line([x1, y, x2 - 10 * S, y], fill=color, width=2 * S)
    draw.polygon([(x2, y), (x2 - 12 * S, y - 7 * S), (x2 - 12 * S, y + 7 * S)], fill=color)


def build() -> Image.Image:
    img = background()
    draw = ImageDraw.Draw(img)

    # 顶部小标签
    label_font = font(16 * S, bold=False)
    label = "S K I P   T U T O R I A L"
    lb = draw.textbbox((0, 0), label, font=label_font)
    draw.text(((W - (lb[2] - lb[0])) // 2, int(H * 0.10)), label, font=label_font, fill=DIM + (255,))

    # 主标题
    centered(draw, int(H * 0.16), "跳过新手教程引导", font(58 * S, bold=True), GOLD_BRIGHT + (255,))

    # 副标题
    centered(draw, int(H * 0.16) + 84 * S, "新建存档 · 捏脸之后直接进入基地",
             font(20 * S, bold=False), SUB_COLOR + (255,))

    # 流程三块
    y = int(H * 0.47)
    h = 96 * S
    w = 132 * S
    x1 = 30 * S
    x2 = x1 + w + 36 * S
    x3 = x2 + w + 36 * S

    chip(draw, x1, y, w, h, "序章 · 捏脸", "保留")
    arrow(draw, x1 + w + 8 * S, x2 - 8 * S, y + h // 2)

    chip(draw, x2, y, w, h, "新手教程", "跳过", dim=True, strike=True)
    arrow(draw, x2 + w + 8 * S, x3 - 8 * S, y + h // 2)

    chip(draw, x3, y, w, h, "基地", "+3000 启动资金")

    # 底部说明
    centered(draw, y + h + 54 * S, "只跳过新手引导关卡 · 不修改存档结构",
             font(16 * S, bold=False), SUB_COLOR + (255,))
    centered(draw, y + h + 88 * S, "序章剧情与捏脸完整保留 · 老存档不受影响",
             font(16 * S, bold=False), DIM + (255,))

    # 金色细边框
    draw.rounded_rectangle([8 * S, 8 * S, W - 8 * S, H - 8 * S], radius=22 * S,
                           outline=GOLD + (90,), width=2 * S)

    return img.resize((512, 512), Image.LANCZOS)


if __name__ == "__main__":
    image = build()
    image.save(OUT)
    print("已生成", OUT, image.size)
