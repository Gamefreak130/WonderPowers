namespace Gamefreak130.WonderPowersSpace
{
    using Sims3.SimIFace;
    public static class TunableSettings
    {
        [Tunable, TunableComment("Amount of karma gained when the user fulfills a basic wish")]
        public static int kKarmaBasicWishAmount = 5;

        [Tunable, TunableComment("Amount of karma gained when the user fulfills a lifetime wish")]
        public static int kKarmaLifetimeWishAmount = 200;

        [Tunable, TunableComment("Amount of karma lost when the user cancels a wish")]
        public static int kKarmaCancelWishAmount = 1;

        [Tunable, TunableComment("The value that a Sim's needs are set to when affected by the Curse karma power (except for the bladder need, which will always be fully emptied")]
        public static int kCurseMotiveAmount = -95;

        [Tunable, TunableComment("The length of time in sim-minutes that the effects of the Cry Havoc karma power will last")]
        public static int kCryHavocLength = 180;

        [Tunable, TunableComment("The minimum number of Sims that will be affected by the Cry Havoc karma power")]
        public static int kCryHavocMinSims = 8;

        [Tunable, TunableComment("Interactions Sims are compelled to perform when affected by the Cry Havoc karma power")]
        public static string[] kCryHavocSimInteractions = { "Force Fight!", "Slap", "Yell At" };

        [Tunable, TunableComment("Interactions pets are compelled to perform when affected by the Cry Havoc karma power")]
        public static string[] kCryHavocPetInteractions = { "Fight Pet", "Chase Mean" };

        [Tunable, TunableComment("The minimum number of objects to break during the Quake Maker karma power")]
        public static int kEarthquakeMinBroken = 5;

        [Tunable, TunableComment("The maximum number of objects to break during the Quake Maker karma power")]
        public static int kEarthquakeMaxBroken = 10;

        [Tunable, TunableComment("The minimum number of trash spawned during the Quake Maker karma power")]
        public static int kEarthquakeMinTrash = 5;

        [Tunable, TunableComment("The maximum number of trash spawned during the Quake Maker karma power")]
        public static int kEarthquakeMaxTrash = 10;

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

        [Tunable, TunableComment("The amount by which a Sim's needs are boosted when affected by the A Little Sunshine karma power")]
        public static float kRayOfSunshineBoostAmount = 20;

        [Tunable, TunableComment("The multiplier applied to a Sim's motive decay when affected by the Sickness karma power")]
        public static float kSicknessMotiveDecay = 2;

        [Tunable, TunableComment("The minimum amount added to a Sim's household funds when affected by the Wealth karma power")]
        public static int kWealthMinAmount = 10000;

        [Tunable, TunableComment("The maximum amount added to a Sim's household funds when affected by the Wealth karma power")]
        public static int kWealthMaxAmount = 30000;
    }
}