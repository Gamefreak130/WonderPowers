﻿using Sims3.SimIFace;

namespace Gamefreak130.WonderPowersSpace
{
    public static class TunableSettings
    {
        [Tunable, TunableComment("The minimum number of Sims that will be affected by the Cry Havoc karma power")]
        public static int kCryHavocMinSims = 8;

        [Tunable, TunableComment("Interactions Sims are compelled to perform when affected by the Cry Havoc karma power")]
        public static string[] kCryHavocSimInteractions = { "Fight!", "Slap", "Yell At" };

        [Tunable, TunableComment("Interactions pets are compelled to perform when affected by the Cry Havoc karma power")]
        public static string[] kCryHavocPetInteractions = { "Fight Pet", "Chase Mean" };
    }
}
