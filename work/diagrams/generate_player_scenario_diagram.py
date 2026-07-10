from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "outputs" / "diagrams"
OUT_DIR.mkdir(parents=True, exist_ok=True)
OUT_FILE = OUT_DIR / "player_scenario_structure.png"

WIDTH, HEIGHT = 1920, 1080


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        Path("C:/Windows/Fonts/malgunbd.ttf" if bold else "C:/Windows/Fonts/malgun.ttf"),
        Path("C:/Windows/Fonts/NotoSansKR-Bold.ttf" if bold else "C:/Windows/Fonts/NotoSansKR-Regular.ttf"),
        Path("C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf"),
    ]
    for path in candidates:
        if path.exists():
            return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default()


TITLE = font(54, True)
SUBTITLE = font(25)
LABEL = font(24, True)
BODY = font(21)
NODE_BODY = font(19)
SMALL = font(18)
TINY = font(16)


COLORS = {
    "bg": "#F7F8FB",
    "ink": "#17212B",
    "muted": "#5A6775",
    "line": "#34495E",
    "white": "#FFFFFF",
    "pipe": "#DFF4EA",
    "pipe_border": "#1E9A63",
    "bit": "#FFF1D6",
    "bit_border": "#C47A10",
    "mirror": "#E9EEFF",
    "mirror_border": "#5066C9",
    "flow": "#EDF7FF",
    "flow_border": "#2B79A8",
    "clear": "#F3ECFF",
    "clear_border": "#7D4CB5",
    "decision": "#FFF6F7",
    "decision_border": "#BF4D61",
    "debug": "#ECEFF3",
    "debug_border": "#778392",
}


def text_size(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont) -> tuple[int, int]:
    bbox = draw.textbbox((0, 0), text, font=fnt)
    return bbox[2] - bbox[0], bbox[3] - bbox[1]


def wrap(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont, max_width: int) -> list[str]:
    lines = []
    for raw_line in text.split("\n"):
        words = raw_line.split(" ")
        current = ""
        for word in words:
            candidate = word if not current else f"{current} {word}"
            if text_size(draw, candidate, fnt)[0] <= max_width:
                current = candidate
                continue

            if current:
                lines.append(current)
                current = word
            else:
                chunk = ""
                for ch in word:
                    candidate_chunk = chunk + ch
                    if text_size(draw, candidate_chunk, fnt)[0] <= max_width:
                        chunk = candidate_chunk
                    else:
                        if chunk:
                            lines.append(chunk)
                        chunk = ch
                current = chunk
        if current:
            lines.append(current)
    return lines


def draw_multiline(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    lines: list[str],
    fnt: ImageFont.FreeTypeFont,
    fill: str,
    center: bool = True,
    line_gap: int = 8,
) -> None:
    x1, y1, x2, y2 = box
    heights = [text_size(draw, line, fnt)[1] for line in lines]
    total_h = sum(heights) + line_gap * max(0, len(lines) - 1)
    y = y1 + ((y2 - y1 - total_h) // 2 if center else 0)
    for line, h in zip(lines, heights):
        w, _ = text_size(draw, line, fnt)
        x = x1 + ((x2 - x1 - w) // 2 if center else 0)
        draw.text((x, y), line, font=fnt, fill=fill)
        y += h + line_gap


def round_box(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    fill: str,
    outline: str,
    width: int = 3,
    radius: int = 24,
) -> None:
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def node(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    title: str,
    body: str,
    fill: str,
    outline: str,
    title_fill: str | None = None,
) -> None:
    round_box(draw, box, fill, outline)
    x1, y1, x2, y2 = box
    draw_multiline(draw, (x1 + 24, y1 + 16, x2 - 24, y1 + 50), [title], LABEL, title_fill or COLORS["ink"])
    body_lines = wrap(draw, body, NODE_BODY, (x2 - x1) - 52)
    draw_multiline(draw, (x1 + 26, y1 + 62, x2 - 26, y2 - 16), body_lines, NODE_BODY, COLORS["ink"], center=True, line_gap=5)


def card(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    title: str,
    rows: list[tuple[str, str]],
    fill: str,
    outline: str,
) -> None:
    round_box(draw, box, fill, outline)
    x1, y1, x2, y2 = box
    draw_multiline(draw, (x1 + 22, y1 + 14, x2 - 22, y1 + 54), [title], LABEL, COLORS["ink"])
    y = y1 + 68
    for label, desc in rows:
        draw.rounded_rectangle((x1 + 24, y, x1 + 116, y + 35), radius=18, fill=outline)
        draw_multiline(draw, (x1 + 24, y + 3, x1 + 116, y + 32), [label], TINY, COLORS["white"])
        desc_lines = wrap(draw, desc, BODY, (x2 - x1) - 166)
        draw_multiline(draw, (x1 + 132, y - 3, x2 - 26, y + 44), desc_lines, BODY, COLORS["ink"], center=False, line_gap=4)
        y += 54


def arrow(
    draw: ImageDraw.ImageDraw,
    start: tuple[int, int],
    end: tuple[int, int],
    color: str = COLORS["line"],
    width: int = 4,
    label: str | None = None,
    label_offset: tuple[int, int] = (0, -30),
) -> None:
    draw.line([start, end], fill=color, width=width)
    sx, sy = start
    ex, ey = end
    dx, dy = ex - sx, ey - sy
    length = max((dx * dx + dy * dy) ** 0.5, 1)
    ux, uy = dx / length, dy / length
    px, py = -uy, ux
    head_len, head_w = 18, 11
    p1 = (ex, ey)
    p2 = (ex - ux * head_len + px * head_w, ey - uy * head_len + py * head_w)
    p3 = (ex - ux * head_len - px * head_w, ey - uy * head_len - py * head_w)
    draw.polygon([p1, p2, p3], fill=color)
    if label:
        mx, my = (sx + ex) // 2 + label_offset[0], (sy + ey) // 2 + label_offset[1]
        w, h = text_size(draw, label, SMALL)
        draw.rounded_rectangle((mx - w // 2 - 10, my - h // 2 - 6, mx + w // 2 + 10, my + h // 2 + 8), radius=10, fill=COLORS["bg"])
        draw.text((mx - w // 2, my - h // 2), label, font=SMALL, fill=COLORS["muted"])


def polyline_arrow(
    draw: ImageDraw.ImageDraw,
    points: list[tuple[int, int]],
    color: str = COLORS["line"],
    width: int = 4,
    label: str | None = None,
) -> None:
    for a, b in zip(points, points[1:]):
        draw.line([a, b], fill=color, width=width)
    arrow(draw, points[-2], points[-1], color=color, width=width)
    if label:
        px, py = points[len(points) // 2]
        w, h = text_size(draw, label, SMALL)
        draw.rounded_rectangle((px - w // 2 - 10, py - h // 2 - 6, px + w // 2 + 10, py + h // 2 + 8), radius=10, fill=COLORS["bg"])
        draw.text((px - w // 2, py - h // 2), label, font=SMALL, fill=COLORS["muted"])


def diamond(
    draw: ImageDraw.ImageDraw,
    center: tuple[int, int],
    size: tuple[int, int],
    title: str,
    body: str,
) -> tuple[int, int, int, int]:
    cx, cy = center
    w, h = size
    points = [(cx, cy - h // 2), (cx + w // 2, cy), (cx, cy + h // 2), (cx - w // 2, cy)]
    draw.polygon(points, fill=COLORS["decision"], outline=COLORS["decision_border"])
    draw.line(points + [points[0]], fill=COLORS["decision_border"], width=3)
    draw_multiline(draw, (cx - w // 2 + 36, cy - 46, cx + w // 2 - 36, cy - 10), [title], LABEL, COLORS["ink"])
    draw_multiline(draw, (cx - w // 2 + 42, cy + 2, cx + w // 2 - 42, cy + 52), wrap(draw, body, SMALL, w - 96), SMALL, COLORS["muted"])
    return (cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2)


def main() -> None:
    img = Image.new("RGB", (WIDTH, HEIGHT), COLORS["bg"])
    draw = ImageDraw.Draw(img)

    # Soft grid lines for a clean technical-sheet look.
    for x in range(80, WIDTH, 80):
        draw.line([(x, 122), (x, HEIGHT - 64)], fill="#EEF1F5", width=1)
    for y in range(140, HEIGHT - 40, 80):
        draw.line([(56, y), (WIDTH - 56, y)], fill="#EEF1F5", width=1)

    draw.text((78, 44), "플레이어 시나리오 구조", font=TITLE, fill=COLORS["ink"])
    draw.text(
        (82, 108),
        "MainScene에서 스테이지를 선택하고, 3개 퍼즐을 자유 순서로 클리어한 뒤 다음 스테이지로 진행",
        font=SUBTITLE,
        fill=COLORS["muted"],
    )

    # Main flow nodes.
    start = (80, 165, 365, 300)
    stage = (435, 165, 720, 300)
    select = (790, 165, 1120, 300)
    node(draw, start, "1. 게임 진입", "MainScene 로드\n진행 상태 유지", COLORS["flow"], COLORS["flow_border"])
    node(draw, stage, "2. 스테이지 선택", "Stage 1~3 선택\n선택 시 퍼즐 클리어 상태 초기화", COLORS["flow"], COLORS["flow_border"])
    node(draw, select, "3. 퍼즐 선택", "Pipe / BitMask / Mirror 중\n아직 클리어하지 않은 퍼즐 입장", COLORS["flow"], COLORS["flow_border"])

    arrow(draw, (365, 232), (435, 232))
    arrow(draw, (720, 232), (790, 232))

    # Puzzle cards.
    pipe = (80, 380, 585, 650)
    bit = (708, 380, 1213, 650)
    mirror = (1335, 380, 1840, 650)

    card(
        draw,
        pipe,
        "Pipe Puzzle",
        [
            ("행동", "파이프 블록을 클릭해 90도 회전"),
            ("피드백", "시작점에서 연결된 경로가 밝게 표시"),
            ("판정", "DFS 연결 탐색으로 모든 목표점 도달 확인"),
        ],
        COLORS["pipe"],
        COLORS["pipe_border"],
    )
    card(
        draw,
        bit,
        "BitMask Puzzle",
        [
            ("행동", "스위치 버튼을 누름"),
            ("피드백", "XOR / OR / AND 연산 후 램프 상태 갱신"),
            ("판정", "5비트 상태가 11111이면 클리어"),
        ],
        COLORS["bit"],
        COLORS["bit_border"],
    )
    card(
        draw,
        mirror,
        "Mirror Puzzle",
        [
            ("행동", "거울을 드래그해 회전"),
            ("피드백", "Raycast 경로와 프리즘 분기 광선 재계산"),
            ("판정", "모든 타깃이 지정 색상 광선에 맞으면 클리어"),
        ],
        COLORS["mirror"],
        COLORS["mirror_border"],
    )

    arrow(draw, (955, 300), (332, 380), label="선택", label_offset=(-18, -22))
    arrow(draw, (955, 300), (960, 380), label="선택", label_offset=(58, -20))
    arrow(draw, (955, 300), (1588, 380), label="선택", label_offset=(10, -24))

    clear = (700, 735, 1220, 850)
    node(
        draw,
        clear,
        "4. 퍼즐 클리어 처리",
        "각 퍼즐 매니저가 FlowManager.Clear() 호출\nClear 패널 표시 + 해당 퍼즐 완료 저장",
        COLORS["clear"],
        COLORS["clear_border"],
    )
    arrow(draw, (332, 650), (800, 735), label="성공", label_offset=(-20, -20))
    arrow(draw, (960, 650), (960, 735), label="성공", label_offset=(70, -25))
    arrow(draw, (1588, 650), (1120, 735), label="성공", label_offset=(20, -20))

    decision_box = diamond(draw, (1450, 793), (310, 170), "5. 전체 클리어?", "Pipe + BitMask + Mirror 모두 완료")
    arrow(draw, (1220, 793), (decision_box[0], 793))

    stage_clear = (1608, 716, 1848, 872)
    node(draw, stage_clear, "Stage Clear", "스테이지 완료 저장\n다음 스테이지 선택", COLORS["flow"], COLORS["flow_border"])
    arrow(draw, (decision_box[2], 793), (1608, 793), label="Yes")

    retry = (760, 925, 1160, 1010)
    round_box(draw, retry, COLORS["debug"], COLORS["debug_border"], width=3, radius=22)
    draw_multiline(
        draw,
        (retry[0] + 24, retry[1] + 14, retry[2] - 24, retry[3] - 14),
        wrap(draw, "No: 퍼즐 선택 화면으로 돌아가 남은 퍼즐을 계속 플레이", BODY, retry[2] - retry[0] - 60),
        BODY,
        COLORS["ink"],
    )
    polyline_arrow(draw, [(1450, 878), (1450, 968), (1160, 968)], label="No")
    polyline_arrow(draw, [(760, 968), (650, 968), (650, 330), (955, 330), (955, 300)], color="#657687", width=4)

    # Support/debug lane.
    debug = (80, 925, 610, 1010)
    round_box(draw, debug, COLORS["debug"], COLORS["debug_border"], width=3, radius=22)
    draw_multiline(
        draw,
        (debug[0] + 24, debug[1] + 14, debug[2] - 24, debug[3] - 14),
        wrap(draw, "시연 보조: Debug Log로 입력-연산-판정 과정을 확인하고, Skip 버튼으로 예외 상황을 검증", BODY, debug[2] - debug[0] - 60),
        BODY,
        COLORS["ink"],
    )

    # Footer.
    draw.text((80, HEIGHT - 42), "ChungKang_subject | Player Scenario Flow", font=TINY, fill=COLORS["muted"])
    draw.text((1465, HEIGHT - 42), "Source: FlowManager + puzzle managers", font=TINY, fill=COLORS["muted"])

    img.save(OUT_FILE, quality=95)
    print(OUT_FILE)


if __name__ == "__main__":
    main()
