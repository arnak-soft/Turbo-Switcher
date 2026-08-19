"""Export Typo Switcher brand PNGs and a multi-size Windows .ico."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent
PNG = ROOT / "png"
C1 = (107, 166, 255)
C2 = (37, 99, 235)
C3 = (30, 64, 175)


def font(size: int, bold: bool = True) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    names = ["segoeuib.ttf" if bold else "segoeui.ttf", "arialbd.ttf" if bold else "arial.ttf"]
    for name in names:
        path = Path(r"C:\Windows\Fonts") / name
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def lerp(a: tuple[int, int, int], b: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return tuple(int(x + (y - x) * t) for x, y in zip(a, b))  # type: ignore[return-value]


def squircle(size: int) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    for y in range(size):
        for x in range(size):
            t = (x * 0.55 + y * 0.45) / max(size - 1, 1)
            mid = lerp(C1, C2, min(t * 1.15, 1))
            color = lerp(mid, C3, max(t - 0.45, 0) / 0.55)
            px[x, y] = (*color, 255)
    radius = int(size * 0.22)
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    shine = Image.new("L", (size, size), 0)
    ImageDraw.Draw(shine).ellipse((-size * 0.1, -size * 0.55, size * 1.1, size * 0.42), fill=42)
    shine = shine.filter(ImageFilter.GaussianBlur(radius=max(size // 28, 1)))
    overlay = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    overlay.putalpha(shine)
    img = Image.alpha_composite(img, overlay)
    img.putalpha(mask)
    return img


def draw_arrows(draw: ImageDraw.ImageDraw, cx: float, cy: float, scale: float) -> None:
    w = max(int(3.2 * scale), 2)
    r = 18 * scale
    bbox = [cx - r, cy - r, cx + r, cy + r]
    draw.arc(bbox, start=210, end=40, fill="white", width=w)
    draw.arc(bbox, start=30, end=220, fill="white", width=w)
    ah = 6.2 * scale
    draw.polygon(
        [(cx + r * 0.55, cy - r * 0.55), (cx + r * 0.55 + ah, cy - r * 0.05), (cx + r * 0.12, cy - r * 0.18)],
        fill="white",
    )
    draw.polygon(
        [(cx - r * 0.55, cy + r * 0.55), (cx - r * 0.55 - ah, cy + r * 0.05), (cx - r * 0.12, cy + r * 0.18)],
        fill="white",
    )


def icon(size: int) -> Image.Image:
    img = squircle(size)
    draw = ImageDraw.Draw(img)
    show_arrows = size >= 96
    letter_size = int(size * (0.32 if show_arrows else 0.44))
    f = font(letter_size)
    text = "AЯ"
    bbox = draw.textbbox((0, 0), text, font=f)
    tw = bbox[2] - bbox[0]
    x = (size - tw) / 2 - bbox[0]
    y = size * (0.26 if show_arrows else 0.28) - bbox[1] * 0.12
    if size >= 48:
        draw.text((x + size * 0.01, y + size * 0.012), text, font=f, fill=(15, 23, 42, 64))
    draw.text((x, y), text, font=f, fill="white")
    if show_arrows:
        draw_arrows(draw, size / 2, size * 0.78, size / 95)
    return img


def wordmark(*, dark: bool = False, opaque_light: bool = False, scale: int = 4) -> Image.Image:
    h = 220 * scale
    w = 980 * scale
    if dark:
        bg = (15, 23, 42, 255)
    elif opaque_light:
        bg = (244, 247, 251, 255)
    else:
        bg = (0, 0, 0, 0)
    img = Image.new("RGBA", (w, h), bg)
    mark = icon(188 * scale)
    img.alpha_composite(mark, (16 * scale, 16 * scale))
    draw = ImageDraw.Draw(img)
    f = font(72 * scale)
    color = (248, 250, 252) if dark else (15, 23, 42)
    draw.text((236 * scale, 78 * scale), "Typo Switcher", font=f, fill=color)
    return img


def banner() -> Image.Image:
    img = Image.new("RGBA", (1280, 640), (11, 18, 32, 255))
    draw = ImageDraw.Draw(img)
    draw.ellipse((760, -320, 1320, 240), fill=(37, 99, 235, 46))
    draw.ellipse((960, 420, 1400, 860), fill=(29, 78, 216, 40))
    mark = icon(320)
    img.alpha_composite(mark, (96, 160))
    title = font(72)
    sub = font(28, bold=False)
    draw.text((480, 236), "Typo Switcher", font=title, fill=(248, 250, 252))
    draw.text((480, 328), "Исправляет текст, набранный не в той раскладке", font=sub, fill=(147, 197, 253))
    return img


def save_ico() -> None:
    sizes = [16, 24, 32, 48, 64, 128, 256]
    images = [icon(s) for s in sizes]
    images[0].save(ROOT / "app.ico", format="ICO", sizes=[(s, s) for s in sizes], append_images=images[1:])


def main() -> None:
    PNG.mkdir(exist_ok=True)
    for size in (1024, 512, 256, 180, 128, 64, 48, 32, 16):
        icon(size).save(PNG / f"icon-{size}.png")
    wordmark().save(PNG / "wordmark.png")
    wordmark(opaque_light=True).save(PNG / "wordmark-light.png")
    wordmark(dark=True).save(PNG / "wordmark-dark.png")
    banner().save(PNG / "banner.png")
    save_ico()
    print(f"Wrote icons to {PNG}")
    print(f"Wrote {ROOT / 'app.ico'}")


if __name__ == "__main__":
    main()
