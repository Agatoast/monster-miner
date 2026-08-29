from PIL import Image
import os

src = r"Assets/ithappy/Casino_Free/Textures/Screens_1.png"
dst_dir = r"Assets/Resources/Textures/SlotMachine"
dst = os.path.join(dst_dir, "screens_1_monster.png")

title_panel_paths = [
    os.path.join(dst_dir, "title_panel_512x256.png"),
    os.path.join(dst_dir, "title_panel_source.png"),
    os.path.join(dst_dir, "guide_01_title_top_nameplate_512x256.png"),
]

jackpot_panel_paths = [
    os.path.join(dst_dir, "jackpot_panel_512x256.png"),
    os.path.join(dst_dir, "jackpot_panel_source.jpg"),
    os.path.join(dst_dir, "jackpot_panel_source.png"),
    os.path.join(dst_dir, "guide_02_jackpot_panel_top_512x256.png"),
]

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src_path = os.path.join(root, src)
dst_path = os.path.join(root, dst)
os.makedirs(os.path.dirname(dst_path), exist_ok=True)

img = Image.open(src_path).convert("RGBA")

PANEL_WIDTH = 512
PANEL_HEIGHT = 256
TILE_SIZE = 512
JACKPOT_Y_OFFSET = -5
JACKPOT_EXTRA_HEIGHT = 4
TITLE_Y_OFFSET = 44


def tile_origin(col, row):
    return col * 512, row * 512


def load_first_panel(paths):
    for panel_path in paths:
        full_panel_path = os.path.join(root, panel_path)
        if os.path.exists(full_panel_path):
            return Image.open(full_panel_path).convert("RGBA")
    return None


def prepare_panel(panel):
    if panel is None:
        return None

    if panel.size == (PANEL_WIDTH, PANEL_HEIGHT):
        return panel

    # Keep the native 2:1 screen aspect; never squash to a square tile.
    return panel.resize((PANEL_WIDTH, PANEL_HEIGHT), Image.Resampling.LANCZOS)


def paste_panel_region(col, row, panel, half="top", y_offset=0, anchor_bottom=False, height=None):
    if panel is None:
        return False

    ox, oy = tile_origin(col, row)
    if anchor_bottom:
        oy += TILE_SIZE - PANEL_HEIGHT
    elif half == "bottom":
        oy += PANEL_HEIGHT
    oy += y_offset

    paste_height = height if height is not None else PANEL_HEIGHT
    prepared = prepare_panel(panel)
    if prepared.size != (PANEL_WIDTH, paste_height):
        prepared = prepared.resize((PANEL_WIDTH, paste_height), Image.Resampling.LANCZOS)

    img.paste(prepared, (ox, oy), prepared)
    return True


title_panel = load_first_panel(title_panel_paths)
if not paste_panel_region(0, 0, title_panel, half="top", y_offset=TITLE_Y_OFFSET):
    raise FileNotFoundError("Missing title panel art in Assets/Resources/Textures/SlotMachine/")

prepared_title_path = os.path.join(root, dst_dir, "title_panel_512x256_prepared.png")
prepare_panel(title_panel).save(prepared_title_path)

jackpot_panel = load_first_panel(jackpot_panel_paths)
jackpot_height = PANEL_HEIGHT + JACKPOT_EXTRA_HEIGHT
if not paste_panel_region(
    0,
    1,
    jackpot_panel,
    half="bottom",
    y_offset=JACKPOT_Y_OFFSET,
    height=jackpot_height,
):
    raise FileNotFoundError("Missing jackpot panel art in Assets/Resources/Textures/SlotMachine/")

prepared_jackpot_path = os.path.join(root, dst_dir, "jackpot_panel_512x256_prepared.png")
prepare_panel(jackpot_panel).save(prepared_jackpot_path)

img.save(dst_path)
print(f"Wrote {dst_path} ({img.size[0]}x{img.size[1]})")
