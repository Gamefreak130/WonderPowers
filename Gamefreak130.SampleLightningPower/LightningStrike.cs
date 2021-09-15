using Gamefreak130.WonderPowersSpace.Helpers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;

namespace Gamefreak130.SampleLightningPowerSpace.Interactions
{
    public class LightningStrike : Sim.GetStruckByLightning
    {
        new public class Definition : InteractionDefinition<Sim, Sim, LightningStrike>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }
        }

        public override bool Run()
        {
            try
            {
                return base.Run();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }
}
