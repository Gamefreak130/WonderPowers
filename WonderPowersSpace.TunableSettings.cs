using Sims3.SimIFace;

namespace Gamefreak130.WonderPowersSpace
{
    public static class TunableSettings
    {
        [Tunable, TunableComment("The length of time in sim-minutes that the effects of the Cry Havoc karma power will last")]
        public static int kCryHavocLength = 180;

        [Tunable, TunableComment("The minimum number of Sims that will be affected by the Cry Havoc karma power")]
        public static int kCryHavocMinSims = 8;

        [Tunable, TunableComment("Interactions Sims are compelled to perform when affected by the Cry Havoc karma power")]
        public static string[] kCryHavocSimInteractions = { "Force Fight!", "Slap", "Yell At" };

        [Tunable, TunableComment("Interactions pets are compelled to perform when affected by the Cry Havoc karma power")]
        public static string[] kCryHavocPetInteractions = { "Fight Pet", "Chase Mean" };

        [Tunable, TunableComment("The maximum number of objects to break during the Quake Maker karma power")]
        public static int kEarthquakeMaxBroken = 5;

        [Tunable, TunableComment("The maximum number of trash spawned during the Quake Maker karma power")]
        public static int kEarthquakeMaxTrash = 5;

        [Tunable, TunableComment("The minimum number of fires spawned during the Fire Storm karma power")]
        public static int kFireMin = 3;

        [Tunable, TunableComment("The maximum number of fires spawned during the Fire Storm karma power")]
        public static int kFireMax = 7;

        [Tunable, TunableComment("The length of time in sim-minutes that the effects of the Ghost Invasion karma power will last")]
        public static int kGhostInvasionLength = 180;

        [Tunable, TunableComment("The minimum number of ghosts spawned during the Ghost Invasion karma power")]
        public static int kGhostsMin = 3;

        [Tunable, TunableComment("The maximum number of ghosts spawned during the Ghost Invasion karma power")]
        public static int kGhostsMax = 5;
    }
}