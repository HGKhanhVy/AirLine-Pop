"""The airline's own coral plane, drawn by generate_flat_art.py and generate_home.py.

Every player starts with it; its art keeps the plain names (airplane.png, plane_parked.png).
"""

LIVERIES = [
    ("coral", (240, 124, 108, 255), (206, 92, 82, 255)),
]

# The other planes in the shop are models of their own, not repaints: plane_models.py
# draws them (airplane_<id>.png and plane_parked_<id>.png), so they are not listed here.


def suffix(livery_id):
    return "" if livery_id == "coral" else "_" + livery_id
