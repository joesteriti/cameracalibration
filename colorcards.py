class Colorcard:
    def __init__(self, name, colors):
        self.name = name
        self.reference_colors = colors
      
    def get_color_card_name(self) -> str:
        return self.name
    
#Input colors in RGB format read from top-left to bottom-right of the color card
small24_color_card = Colorcard(
    "CameraTrax 24 ColorCard",
    {
        "White": (243, 238, 243),
        "Blue": (34, 63, 147),
        "Orange": (224, 124, 47),
        "Dark Tone": (116, 81, 67),
        "Light Grey": (200, 202, 202),
        "Green": (67, 149, 74),
        "Medium Blue": (68, 91, 170),
        "Light Tone": (199, 147, 129),
        "Grey": (161, 162, 161),
        "Red": (180, 49, 47),
        "Light Red": (198, 82, 97),
        "Sky Blue": (91, 122, 156),
        "Dark Grey": (120, 121, 120),
        "Yellow": (238, 198, 32),
        "Purple": (94, 58, 106),
        "Tree Green": (90, 108, 64),
        "Charcoal": (82, 83, 83),
        "Magenta": (193, 84, 151),
        "Yellow Green": (159, 189, 63),
        "Light Blue": (130, 128, 176),
        "Black": (49, 48, 51),
        "Cyan": (12, 136, 170),
        "Orange Yellow": (230, 162, 39),
        "Blue Green": (92, 190, 172),
    })