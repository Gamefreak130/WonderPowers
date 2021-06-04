using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.SimIFace.ResourceUtils;

namespace Gamefreak130.WonderPowersSpace.Interactions
{
    public class GoToLotAndFight : GoToLot
    {
        new public class Definition : GoToLot.Definition
        {
            public override bool Test(Sim actor, Lot target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                InteractionInstance instance = new GoToLotAndFight() { CancellableByPlayer = parameters.CancellableByPlayer, Hidden = true };
                instance.Init(ref parameters);
                return instance;
            }

            public override string GetInteractionName(Sim actor, Lot target, InteractionObjectPair iop) => base.GetInteractionName(actor, target, new(Singleton, target));
        }

        public override bool Run()
        {
            bool flag = base.Run();
            if (flag)
            {
                Actor.BuffManager.AddElement(Buffs.BuffCryHavoc.kBuffCryHavocGuid, (Origin)HashString64("FromWonderPower"));
            }
            return flag;
        }
    }

    public class DivineInterventionResurrect : Urnstone.ResurrectSim
    {
        new public class Definition : Urnstone.ResurrectSim.Definition
        {
            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                InteractionInstance instance = new DivineInterventionResurrect() { ResetAge = true, MustRun = true, Hidden = true };
                instance.Init(ref parameters);
                return instance;
            }

            public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop) => base.GetInteractionName(actor, target, new(Singleton, target));
        }

        public override bool Run()
        {
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }
            Audio.StartSound("sting_lifetime_opp_success");
            bool flag = base.Run();
            Actor.UpdateWalkStyle();
            return flag;
        }

        public override void Cleanup()
        {
            Helpers.WonderPowerManager.TogglePowerRunning();
            base.Cleanup();
        }
    }

    public class EarthquakePanicReact : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<EarthquakePanicReact>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => Helpers.WonderPowerManager.LocalizeString("PanicReact");
        }

        public override bool Run()
        {
            EnterStateMachine("ReactToFire", "Enter", "x");
            AnimateSim("Panic");
            bool result = DoTimedLoop(FireFightingJob.kEarthquakeTimeUntilTNS);
            AnimateSim("Exit");
            return result;
        }
    }

    public class ReceiveMagicalCheck : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<ReceiveMagicalCheck>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => Helpers.WonderPowerManager.LocalizeString("ReceiveMagicalCheck");
        }

        public override bool Run()
        {
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }
            Audio.StartSound("sting_wealth");
            EnterStateMachine("ReceiveMagicalCheck", "WinLottoEnter", "x");
            AnimateSim("PullOutCheck");
            AnimateSim("VictoryDance");
            AnimateSim("WinLottoExit");
            Actor.BuffManager.AddElement((BuffNames)HashString64("Gamefreak130_WealthBuff"), (Origin)HashString64("FromWonderPower"));
            return true;
        }

        public override void Cleanup()
        {
            int amount = RandomUtil.GetInt(TunableSettings.kWealthMinAmount, TunableSettings.kWealthMaxAmount);
            Actor.Household.ModifyFamilyFunds(amount);
            Actor.ShowTNSIfSelectable(Helpers.WonderPowerManager.LocalizeString(Actor.IsFemale, "WealthTNS", Actor, amount), StyledNotification.NotificationStyle.kGameMessagePositive);
            Helpers.WonderPowerManager.TogglePowerRunning();
            base.Cleanup();
        }
    }
}