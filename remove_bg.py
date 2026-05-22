"""
remove_bg.py
把按鈕圖片的背景變透明，使用四角取色 + 容差漫水填充（flood fill）。
用法：
    python remove_bg.py
需要安裝 Pillow：
    pip install Pillow
"""

from PIL import Image
import sys
from collections import deque

# ── 設定 ──────────────────────────────────────────────────
IMAGE_PATHS = [
    "Assets/Resources/BTN_PLAYAGAIN.png",
    "Assets/Resources/BTN_QUIT.png",
]
TOLERANCE = 30   # 顏色容差 (0-255)，越大去除範圍越寬，調小可保留邊緣
# ─────────────────────────────────────────────────────────


def color_diff(c1, c2):
    """計算兩個 RGB 顏色的最大通道差距。"""
    return max(abs(int(c1[i]) - int(c2[i])) for i in range(3))


def flood_fill_transparent(img, start_x, start_y, tolerance):
    """
    從 (start_x, start_y) 開始做漫水填充，
    把與起點顏色相近的像素 alpha 設為 0（透明）。
    """
    rgba = img.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size

    seed_color = pixels[start_x, start_y][:3]  # 取 RGB，忽略現有 alpha

    visited = [[False] * height for _ in range(width)]
    queue = deque()
    queue.append((start_x, start_y))
    visited[start_x][start_y] = True

    while queue:
        x, y = queue.popleft()
        px = pixels[x, y]

        # 如果已經是透明，跳過
        if px[3] == 0:
            continue

        # 顏色相近才填充
        if color_diff(px[:3], seed_color) <= tolerance:
            pixels[x, y] = (px[0], px[1], px[2], 0)  # 設為透明

            for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                nx, ny = x + dx, y + dy
                if 0 <= nx < width and 0 <= ny < height and not visited[nx][ny]:
                    visited[nx][ny] = True
                    queue.append((nx, ny))

    return rgba


def remove_background(path, tolerance=TOLERANCE):
    print(f"\n處理：{path}")
    try:
        img = Image.open(path).convert("RGBA")
    except FileNotFoundError:
        print(f"  ✗ 找不到檔案：{path}")
        return

    width, height = img.size
    print(f"  尺寸：{width} x {height}")

    # 從四個角落各做一次漫水填充
    corners = [(0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1)]
    result = img
    for cx, cy in corners:
        result = flood_fill_transparent(result, cx, cy, tolerance)
        print(f"  已處理角落 ({cx}, {cy})")

    result.save(path)
    print(f"  ✓ 已儲存（帶透明背景）：{path}")


if __name__ == "__main__":
    try:
        from PIL import Image
    except ImportError:
        print("錯誤：找不到 Pillow，請先執行：pip install Pillow")
        sys.exit(1)

    for p in IMAGE_PATHS:
        remove_background(p, tolerance=TOLERANCE)

    print("\n✅ 完成！請回到 Unity，圖片會自動重新載入。")
