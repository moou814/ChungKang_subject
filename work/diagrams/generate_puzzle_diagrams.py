from __future__ import annotations

import math
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "outputs" / "diagrams"

W, H = 1920, 1080

COLORS = {
    "bg": "#F7F9FC",
    "ink": "#172033",
    "muted": "#5B6575",
    "line": "#AEB7C4",
    "panel": "#FFFFFF",
    "panel2": "#EEF3F9",
    "blue": "#2B6CB0",
    "blue2": "#DCEBFA",
    "green": "#2F855A",
    "green2": "#DFF3E8",
    "orange": "#DD6B20",
    "orange2": "#FEEBC8",
    "red": "#C53030",
    "red2": "#FED7D7",
    "yellow": "#F6E05E",
    "dark": "#202938",
    "white": "#FFFFFF",
    "black": "#111827",
    "purple": "#6B46C1",
    "purple2": "#E9D8FD",
}


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        Path("C:/Windows/Fonts/malgunbd.ttf" if bold else "C:/Windows/Fonts/malgun.ttf"),
        Path("C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf"),
    ]
    for path in candidates:
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


F_TITLE = font(54, True)
F_SUB = font(28, True)
F_BODY = font(24)
F_BODY_B = font(24, True)
F_SMALL = font(19)
F_SMALL_B = font(19, True)
F_CODE = font(20)


def new_canvas(title: str, subtitle: str) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    img = Image.new("RGB", (W, H), COLORS["bg"])
    draw = ImageDraw.Draw(img)
    draw.text((70, 48), title, fill=COLORS["ink"], font=F_TITLE)
    draw.text((72, 118), subtitle, fill=COLORS["muted"], font=F_BODY)
    draw.line((70, 164, W - 70, 164), fill=COLORS["line"], width=2)
    return img, draw


def text_size(draw: ImageDraw.ImageDraw, text: str, fnt) -> tuple[int, int]:
    box = draw.textbbox((0, 0), text, font=fnt)
    return box[2] - box[0], box[3] - box[1]


def wrap_text(draw: ImageDraw.ImageDraw, text: str, fnt, max_width: int) -> list[str]:
    lines: list[str] = []
    for paragraph in text.split("\n"):
        current = ""
        tokens = paragraph.split(" ")
        for token in tokens:
            candidate = token if not current else current + " " + token
            if text_size(draw, candidate, fnt)[0] <= max_width:
                current = candidate
            else:
                if current:
                    lines.append(current)
                current = token
        if current:
            lines.append(current)
    return lines


def box(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int, int, int],
    title: str,
    body: str = "",
    fill: str = COLORS["panel"],
    outline: str = COLORS["line"],
    title_color: str = COLORS["ink"],
    radius: int = 24,
) -> None:
    x1, y1, x2, y2 = xy
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline, width=2)
    draw.text((x1 + 24, y1 + 20), title, fill=title_color, font=F_SUB)
    if body:
        yy = y1 + 70
        for line in wrap_text(draw, body, F_SMALL, x2 - x1 - 48):
            draw.text((x1 + 24, yy), line, fill=COLORS["ink"], font=F_SMALL)
            yy += 28


def label(
    draw: ImageDraw.ImageDraw,
    pos: tuple[int, int],
    text: str,
    fnt=F_SMALL,
    fill: str = COLORS["ink"],
    anchor: str | None = None,
) -> None:
    draw.text(pos, text, font=fnt, fill=fill, anchor=anchor)


def arrow(draw: ImageDraw.ImageDraw, start: tuple[int, int], end: tuple[int, int], color: str = COLORS["blue"], width: int = 4) -> None:
    draw.line((start, end), fill=color, width=width)
    sx, sy = start
    ex, ey = end
    angle = math.atan2(ey - sy, ex - sx)
    head = 18
    spread = math.radians(28)
    p1 = (ex - head * math.cos(angle - spread), ey - head * math.sin(angle - spread))
    p2 = (ex - head * math.cos(angle + spread), ey - head * math.sin(angle + spread))
    draw.polygon([end, p1, p2], fill=color)


def small_badge(draw: ImageDraw.ImageDraw, xy: tuple[int, int, int, int], text: str, fill: str, outline: str = COLORS["line"]) -> None:
    draw.rounded_rectangle(xy, radius=14, fill=fill, outline=outline, width=1)
    tw, th = text_size(draw, text, F_SMALL_B)
    x1, y1, x2, y2 = xy
    draw.text(((x1 + x2 - tw) / 2, (y1 + y2 - th) / 2 - 2), text, fill=COLORS["ink"], font=F_SMALL_B)


def draw_pipe_diagram() -> Path:
    img, draw = new_canvas(
        "파이프 퍼즐 알고리즘 다이어그램",
        "StageData의 2차원 그리드를 DFS로 탐색하여 시작점에서 모든 목적지까지 연결되는지 검증",
    )

    box(draw, (70, 210, 470, 420), "입력 데이터", "StageData\n- mapSize: 행 x 열\n- map: blockKind[]\n- start / end\n- fixedBlocks", COLORS["blue2"], COLORS["blue"])
    box(draw, (70, 470, 470, 760), "블록 규칙", "blockKind + angle\nGetCanGo()\n\nL / I / + / T 블록이 현재 회전값에 따라 열려 있는 방향을 반환", COLORS["panel"], COLORS["line"])

    grid_x, grid_y = 585, 315
    cell = 78
    rows, cols = 5, 6
    draw.rounded_rectangle((540, 210, 1115, 790), radius=26, fill=COLORS["panel"], outline=COLORS["line"], width=2)
    label(draw, (565, 225), "그리드 상태 예시", F_SUB)
    label(draw, (565, 265), "visited[,]로 도달 가능한 칸을 표시", F_SMALL, COLORS["muted"])

    path_cells = {(0, 0), (0, 1), (1, 1), (2, 1), (2, 2), (3, 2), (3, 3), (4, 3), (4, 4), (4, 5)}
    fixed_cells = {(1, 3), (2, 4), (3, 1)}
    end_cells = {(4, 5)}
    for r in range(rows):
        for c in range(cols):
            x = grid_x + c * cell
            y = grid_y + r * cell
            fill = COLORS["green2"] if (r, c) in path_cells else "#E8EDF4"
            if (r, c) in fixed_cells:
                fill = COLORS["orange2"]
            if (r, c) in end_cells:
                fill = COLORS["red2"]
            draw.rounded_rectangle((x, y, x + cell - 8, y + cell - 8), radius=12, fill=fill, outline=COLORS["line"], width=2)
            cx = x + (cell - 8) // 2
            cy = y + (cell - 8) // 2
            if (r, c) == (0, 0):
                label(draw, (cx, cy - 13), "S", F_SUB, COLORS["green"], "mm")
                label(draw, (cx, cy + 18), "start", F_SMALL, COLORS["green"], "mm")
            elif (r, c) in end_cells:
                label(draw, (cx, cy - 13), "E", F_SUB, COLORS["red"], "mm")
                label(draw, (cx, cy + 18), "end", F_SMALL, COLORS["red"], "mm")
            elif (r, c) in fixed_cells:
                label(draw, (cx, cy), "고정", F_SMALL_B, COLORS["orange"], "mm")
            elif (r, c) in path_cells:
                label(draw, (cx, cy), "ON", F_SMALL_B, COLORS["green"], "mm")
            else:
                label(draw, (cx, cy), "-", F_BODY_B, COLORS["muted"], "mm")

    box(draw, (1220, 210, 1810, 420), "DFS 검증 흐름", "stack에 시작 좌표 push → pop → 연결 가능한 방향 검사 → 반대 방향으로 이어진 이웃만 방문 처리", COLORS["green2"], COLORS["green"])
    box(draw, (1220, 475, 1810, 660), "클리어 조건", "모든 end 좌표가 visited == true이면 FlowManager.Clear() 호출", COLORS["red2"], COLORS["red"])
    box(draw, (1220, 715, 1810, 895), "시간 복잡도", "각 칸은 최대 한 번 방문하고 4방향만 검사하므로 O(행 x 열)", COLORS["panel"], COLORS["line"])

    arrow(draw, (470, 315), (540, 315))
    arrow(draw, (1115, 360), (1220, 320))
    arrow(draw, (1515, 420), (1515, 475), COLORS["green"])
    arrow(draw, (1515, 660), (1515, 715), COLORS["red"])

    small_badge(draw, (590, 740, 760, 780), "초록: 방문됨", COLORS["green2"])
    small_badge(draw, (780, 740, 950, 780), "주황: 고정", COLORS["orange2"])
    small_badge(draw, (970, 740, 1090, 780), "빨강: 목적지", COLORS["red2"])

    path = OUT_DIR / "pipe_puzzle_diagram.png"
    img.save(path)
    return path


def draw_bitmask_diagram() -> Path:
    img, draw = new_canvas(
        "비트마스크 퍼즐 알고리즘 다이어그램",
        "5개의 램프 상태를 uint 한 개에 압축하고 XOR / OR / AND 연산으로 상태를 갱신",
    )

    box(draw, (70, 210, 500, 420), "스테이지 데이터", "bitMaskData\n- switchType[]\n- switchInfo[]\n\n스위치마다 5비트 마스크와 연산 타입을 보유", COLORS["blue2"], COLORS["blue"])

    switch_y = 500
    switches = [("SW 0", "XOR", "10101"), ("SW 1", "OR", "01010"), ("SW 2", "AND", "11001")]
    for i, (name, op, mask) in enumerate(switches):
        x = 105 + i * 130
        draw.rounded_rectangle((x, switch_y, x + 105, switch_y + 130), radius=20, fill=COLORS["panel"], outline=COLORS["blue"], width=3)
        label(draw, (x + 52, switch_y + 30), name, F_SMALL_B, COLORS["ink"], "mm")
        label(draw, (x + 52, switch_y + 65), op, F_BODY_B, COLORS["blue"], "mm")
        label(draw, (x + 52, switch_y + 100), mask, F_SMALL_B, COLORS["muted"], "mm")

    box(draw, (635, 210, 1115, 420), "상태 갱신", "curState는 uint\n\nswitchMask 적용:\n- XOR: 토글\n- OR: 켜기\n- AND: 필터링", COLORS["panel"], COLORS["line"])

    state_x, state_y = 650, 520
    bits = ["1", "0", "1", "1", "0"]
    for i, bit in enumerate(bits):
        x = state_x + i * 78
        fill = COLORS["yellow"] if bit == "1" else "#CBD5E1"
        draw.rounded_rectangle((x, state_y, x + 64, state_y + 64), radius=14, fill=fill, outline=COLORS["line"], width=2)
        label(draw, (x + 32, state_y + 31), bit, F_SUB, COLORS["ink"], "mm")
        label(draw, (x + 32, state_y + 92), f"bit {i}", F_SMALL, COLORS["muted"], "mm")
    label(draw, (650, 470), "curState 비트 표현", F_SUB)

    lamp_x, lamp_y = 1230, 255
    draw.rounded_rectangle((1190, 210, 1810, 800), radius=26, fill=COLORS["panel"], outline=COLORS["line"], width=2)
    label(draw, (1220, 230), "lampUpdate()", F_SUB)
    label(draw, (1220, 270), "각 bit를 램프 색상으로 변환하고 모두 켜졌는지 검사", F_SMALL, COLORS["muted"])
    for i in range(5):
        cx = lamp_x + i * 105
        cy = lamp_y + 155
        on = i in {0, 2, 3}
        draw.ellipse((cx - 34, cy - 34, cx + 34, cy + 34), fill=COLORS["yellow"] if on else COLORS["dark"], outline=COLORS["line"], width=2)
        label(draw, (cx, cy + 65), f"L{i}", F_SMALL_B, COLORS["ink"], "mm")
    box(draw, (1250, 610, 1750, 735), "클리어 판정", "모든 램프 bit가 1이면 Clear", COLORS["green2"], COLORS["green"])

    code_x, code_y = 610, 730
    draw.rounded_rectangle((code_x, code_y, 1115, 895), radius=20, fill="#111827", outline="#111827")
    code_lines = [
        "on = (curState & (1 << i)) != 0",
        "lights[i].color = on ? yellow : black",
        "if all lamps are on: Clear()",
    ]
    for i, line in enumerate(code_lines):
        label(draw, (code_x + 24, code_y + 28 + i * 39), line, F_CODE, "#E5E7EB")

    arrow(draw, (500, 315), (635, 315))
    arrow(draw, (450, 565), (635, 565), COLORS["orange"])
    arrow(draw, (1035, 550), (1190, 420))
    arrow(draw, (1500, 535), (1500, 610), COLORS["green"])

    path = OUT_DIR / "bitmask_puzzle_diagram.png"
    img.save(path)
    return path


def reflect(direction: tuple[float, float], normal: tuple[float, float]) -> tuple[float, float]:
    dx, dy = direction
    nx, ny = normal
    dot = dx * nx + dy * ny
    rx = dx - 2 * dot * nx
    ry = dy - 2 * dot * ny
    length = math.hypot(rx, ry) or 1
    return rx / length, ry / length


def draw_mirror_diagram() -> Path:
    img, draw = new_canvas(
        "Mirror 광선 퍼즐 알고리즘 다이어그램",
        "Raycast2D로 충돌 지점을 찾고 벡터 반사 공식과 프리즘 분기로 광선 경로를 계산",
    )

    box(draw, (70, 210, 470, 410), "MirrorStageData", "map[]: Mirror / Target / Boundary / Prism\nrayStartPoint\nrayStartDir\ntargets[]", COLORS["blue2"], COLORS["blue"])
    box(draw, (70, 460, 470, 650), "플레이어 입력", "마우스 좌/우 버튼\n거울 회전\nPhysics2D.SyncTransforms()\nDrawRay() 재계산", COLORS["panel"], COLORS["line"])
    box(draw, (70, 700, 470, 890), "반사 공식", "reflect = dir - 2 * dot(dir, normal) * normal\n\n거울의 transform.up을 법선으로 사용", COLORS["purple2"], COLORS["purple"])

    area = (560, 220, 1180, 840)
    draw.rounded_rectangle(area, radius=28, fill=COLORS["panel"], outline=COLORS["line"], width=2)
    label(draw, (590, 240), "광선 경로 예시", F_SUB)
    label(draw, (590, 280), "Mirror / Prism / Target / Boundary 충돌 태그에 따라 분기", F_SMALL, COLORS["muted"])

    # Boundary frame
    draw.rectangle((610, 350, 1120, 760), outline=COLORS["dark"], width=5)
    # Mirrors
    draw.line((800, 430, 890, 520), fill=COLORS["blue"], width=10)
    label(draw, (845, 545), "Mirror", F_SMALL_B, COLORS["blue"], "mm")
    draw.line((980, 610, 1070, 520), fill=COLORS["blue"], width=10)
    label(draw, (1025, 645), "Mirror", F_SMALL_B, COLORS["blue"], "mm")
    # Prism and targets
    prism = [(930, 435), (970, 505), (890, 505)]
    draw.polygon(prism, fill=COLORS["purple2"], outline=COLORS["purple"])
    label(draw, (930, 535), "Prism", F_SMALL_B, COLORS["purple"], "mm")
    targets = [((1085, 390), COLORS["red"], "Red Target"), ((1085, 700), COLORS["green"], "Green Target"), ((675, 720), COLORS["blue"], "Blue Target")]
    for (cx, cy), col, txt in targets:
        draw.ellipse((cx - 24, cy - 24, cx + 24, cy + 24), fill=col, outline=COLORS["line"], width=2)
        label(draw, (cx, cy + 45), txt, F_SMALL, COLORS["ink"], "mm")

    # Rays
    arrow(draw, (630, 470), (820, 470), COLORS["yellow"], 6)
    arrow(draw, (820, 470), (930, 470), COLORS["yellow"], 6)
    arrow(draw, (930, 470), (1085, 390), COLORS["red"], 5)
    arrow(draw, (930, 470), (1085, 700), COLORS["green"], 5)
    arrow(draw, (930, 470), (675, 720), COLORS["blue"], 5)

    box(draw, (1260, 210, 1810, 420), "RayLine.CalculateWay()", "RaycastAll → 가장 가까운 유효 hit 선택\nignoreCollider와 0.01 이하 hit 제외", COLORS["panel"], COLORS["line"])
    box(draw, (1260, 470, 1810, 670), "충돌 처리", "Target: 색상 일치 시 clear\nBoundary: 종료\nMirror: 반사 방향 계산\nPrism: RGB 광선 추가", COLORS["green2"], COLORS["green"])
    box(draw, (1260, 720, 1810, 900), "안전 장치", "최대 20회까지만 추적하여 무한 반사 루프 방지\nLineRenderer로 경로 시각화", COLORS["orange2"], COLORS["orange"])

    arrow(draw, (470, 310), (560, 430))
    arrow(draw, (470, 555), (560, 500), COLORS["orange"])
    arrow(draw, (1180, 470), (1260, 315))
    arrow(draw, (1535, 420), (1535, 470), COLORS["green"])
    arrow(draw, (1535, 670), (1535, 720), COLORS["orange"])

    path = OUT_DIR / "mirror_puzzle_diagram.png"
    img.save(path)
    return path


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    paths = [draw_pipe_diagram(), draw_bitmask_diagram(), draw_mirror_diagram()]
    for path in paths:
        print(path)


if __name__ == "__main__":
    main()
