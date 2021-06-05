using Gamefreak130.WonderPowersSpace.Helpers;
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
            Audio.StartSound("sting_lifetime_opp_success");
            bool flag = base.Run();
            Actor.UpdateWalkStyle();
            return flag;
        }

        public override void Cleanup()
        {
            WonderPowerManager.TogglePowerRunning();
            base.Cleanup();
        }
    }

    public class EarthquakePanicReact : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<EarthquakePanicReact>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("PanicReact");
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

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("ReceiveMagicalCheck");
        }

        public override bool Run()
        {
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
            Actor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(Actor.IsFemale, "WealthTNS", Actor, amount), StyledNotification.NotificationStyle.kGameMessagePositive);
            WonderPowerManager.TogglePowerRunning();
            base.Cleanup();
        }
    }

    public class BeDoomed : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<BeDoomed>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("BeDoomed");
        }

        private VisualEffect mEffect;

        public override bool Run()
        {
            Audio.StartSound("sting_job_demote");
            VisualEffect mEffect = VisualEffect.Create("ep7WandSpellHauntingHit_main");
            mEffect.ParentTo(Actor, Sim.FXJoints.Pelvis);
            mEffect.Start();

            string animName = Actor.SimDescription switch 
            {
                { IsFoal: true }                        => "ch_whinny_x",
                { IsHorse: true }                       => "ah_whinny_x",
                { IsFullSizeDog: true, IsPuppy: true }  => "cd_react_stand_whimper_x",
                { IsFullSizeDog: true }                 => "ad_react_stand_whimper_x",
                { IsLittleDog: true, IsPuppy: true }    => "cl_react_stand_whimper_x",
                { IsLittleDog: true }                   => "al_react_stand_whimper_x",
                { IsKitten: true }                      => "cc_petNeeds_standing_hunger_whinyMeow_x",
                { IsCat: true }                         => "ac_petNeeds_standing_hunger_whinyMeow_x",
                { Child: true }                         => "c_motDistress_sleepy_x", 
                { TeenOrAbove: true }                   => "a_motDistress_sleepy_x",
                _                                       => null
            };

            if (!string.IsNullOrEmpty(animName))
            {
                Actor.PlaySoloAnimation(animName, true, Actor.IsPet ? ProductVersion.EP5 : ProductVersion.BaseGame);
            }
            return true;
        }

        public override void Cleanup()
        {
            if (mEffect is not null)
            {
                mEffect.Stop();
                mEffect.Dispose();
                mEffect = null;
            }
            Actor.BuffManager.AddBuff(BuffNames.UnicornsIre, -40, 1440, false, MoodAxis.None, (Origin)HashString64("FromWonderPower"), true);
            Actor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(Actor.IsFemale, "DoomTNS", Actor), StyledNotification.NotificationStyle.kGameMessageNegative);
            BuffInstance buff = Actor.BuffManager.GetElement(BuffNames.UnicornsIre);
            buff.mBuffName = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DoomBuff";
            buff.mDescription = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DoomBuffDescription";
            // This will automatically trigger the BuffsChanged event, so the UI should refresh itself after this and we won't have to do it manually
            buff.SetThumbnail("doom", ProductVersion.BaseGame, Actor);
            WonderPowerManager.TogglePowerRunning();
            base.Cleanup();
        }
    }
}