using Gamefreak130.SampleLightningPowerSpace.Interactions;
using Gamefreak130.WonderPowersSpace.Helpers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using System.Collections.Generic;

namespace Gamefreak130
{
    public static class SampleLightningPower
    {
        public static bool Activate(bool isBacklash)
        {
            Sim selectedSim = null;
            if (isBacklash)
            {
                List<Sim> validSims = Household.ActiveHousehold.Sims.FindAll(sim => sim.SimDescription.TeenOrAbove && sim.CanBeKilled() && !sim.IsInRidingPosture);
                if (validSims.Count > 0)
                {
                    selectedSim = RandomUtil.GetRandomObjectFromList(validSims);
                }
            }
            else
            {
                List<SimDescription> targets = PlumbBob.SelectedActor.LotCurrent.GetSims(sim => sim.SimDescription.TeenOrAbove && sim.CanBeKilled() && !sim.IsInRidingPosture)
                                                                                .ConvertAll(sim => sim.SimDescription);

                SimDescription selectedDescription = HelperMethods.SelectTarget(targets, "Lightning Strike");
                if (selectedDescription != null)
                {
                    selectedSim = selectedDescription.CreatedSim;
                }
            }

            if (selectedSim == null)
            {
                return false;
            }

            Camera.FocusOnSim(selectedSim);
            if (selectedSim.IsSelectable)
            {
                PlumbBob.SelectActor(selectedSim);
            }
            InteractionInstance instance = new LightningStrike.Definition().CreateInstance(selectedSim, selectedSim, new InteractionPriority(InteractionPriorityLevel.CriticalNPCBehavior), false, false);
            if (!instance.Test())
            {
                return false;
            }
            selectedSim.InteractionQueue.AddNext(instance);
            return true;
        }
    }
}
