"""Generate a small tray icon at runtime."""

from __future__ import annotations

from PIL import Image, ImageDraw, ImageFont


def make_icon(size: int = 64, enabled: bool = True) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    bg = (37, 99, 235, 255) if enabled else (107, 114, 128, 255)
    draw.rounded_rectangle((1, 1, size - 2, size - 2), radius=size // 5, fill=bg)
    try:
        font = ImageFont.truetype("segoeui.ttf", size=int(size * 0.42))
    except OSError:
        font = ImageFont.load_default()
    text = "AЯ"
    bbox = draw.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    draw.text(((size - tw) / 2, (size - th) / 2 - bbox[1] * 0.1), text, fill="white", font=font)
    return img
