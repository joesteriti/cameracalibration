namespace CameraCalibration
{
    public class ColorCard
    {
        public string Name { get; }
        public Dictionary<string, (int R, int G, int B)> ReferenceColors { get; }
        public int abovePurple;
        public int belowPurple;
        public int leftPurple;
        public int rightPurple;

        public ColorCard(string name, Dictionary<string, (int R, int G, int B)> colors, int abovePurple, int belowPurple, int leftPurple, int rightPurple)
        {
            Name = name;
            ReferenceColors = colors;
            this.abovePurple = abovePurple;
            this.belowPurple = belowPurple;
            this.leftPurple = leftPurple;
            this.rightPurple = rightPurple;
        }
        public string GetColorCardName()
    {
        return Name;
    }
        public static readonly ColorCard small24ColorCard = new ColorCard(
            "CameraTrax 24 ColorCard",
        new Dictionary<string, (int R, int G, int B)>
        {
            { "White", (243, 238, 243) },
            { "Light Grey", (200, 202, 202) },
            { "Grey", (161, 162, 161) },
            { "Dark Grey", (120, 121, 120) },
            { "Charcoal", (82, 83, 83) },
            { "Black", (49, 48, 51) },
            { "Blue", (34, 63, 147) },
            { "Green", (67, 149, 74) },
            { "Red", (180, 49, 47) },
            { "Yellow", (238, 198, 32) },
            { "Magenta", (193, 84, 151) },
            { "Cyan", (12, 136, 170) },
            { "Orange", (224, 124, 47) },
            { "Medium Blue", (68, 91, 170) },
            { "Light Red", (198, 82, 97) },
            { "Purple", (94, 58, 106) },
            { "Yellow Green", (159, 189, 63) },
            { "Orange Yellow", (230, 162, 39) },
            { "Dark Tone", (116, 81, 67) },
            { "Light Tone", (199, 147, 129) },
            { "Sky Blue", (91, 122, 156) },
            { "Tree Green", (90, 108, 64) },
            { "Light Blue", (130, 128, 176) },
            { "Blue Green", (92, 190, 172) },
        },
        4,
        1,
        0,
        3
        //INPUT HOW MANY GRIDS RELATIVE TO PURPLE SQUARE

    );

        // Additional ColorCards can be defined (hardcoded) here as needed
        //Colors need to be in order from top-left to bottom-right or else the analysis will not identify them correctly

    }
}