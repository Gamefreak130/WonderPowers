using Gamefreak130.WonderPowersSpace.Booters;
using Sims3.Gameplay.Actors;

namespace Gamefreak130.SampleLightningPowerSpace.Booters
{
    public class LightningPowerBooter : PowerBooter
    {
        public LightningPowerBooter() : base("Gamefreak130_SampleLightningPower")
        {
            Common.Tunings.Inject(Sim.GetStruckByLightning.Singleton.GetType(), typeof(Sim), typeof(Interactions.LightningStrike.Definition), typeof(Sim), true);
        }
    }
}