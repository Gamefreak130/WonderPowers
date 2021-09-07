using Gamefreak130.WonderPowersSpace.Buffs;
using Gamefreak130.WonderPowersSpace.Helpers;
using Sims3.Gameplay;
using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.TimeTravel;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Sims3.UI.Dialogs;
using Sims3.UI.Hud;
using System.Collections.Generic;
using System.Linq;
using static Sims3.SimIFace.ResourceUtils;
using Responder = Sims3.UI.Responder;

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
                Actor.BuffManager.AddElement(BuffCryHavoc.kBuffCryHavocGuid, (Origin)HashString64("FromWonderPower"));
            }
            return flag;
        }
    }

    public class BeCursed : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<BeCursed>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("BeCursed");
        }

        private VisualEffect mEffect;

        public override bool Run()
        {
            mEffect = VisualEffect.Create("ep7moodlampimpactred_main");
            mEffect.ParentTo(Actor, Sim.FXJoints.Pelvis);
            mEffect.Start();
            Actor.PlaySoloAnimation("a2o_handiness_fail_electrocution_x", true, ProductVersion.BaseGame);
            WonderPowerManager.PlayPowerSting("sting_curse");
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                foreach (CommodityKind motive in (Responder.Instance.HudModel as HudModel).GetMotives(Actor))
                {
                    Actor.Motives.SetValue(motive, motive is CommodityKind.Bladder ? -100 : TunableSettings.kCurseMotiveAmount);
                }
                if ((Actor.CurrentOccultType & OccultTypes.Fairy) is not OccultTypes.None)
                {
                    Actor.Motives.SetValue(CommodityKind.AuraPower, TunableSettings.kCurseMotiveAmount);
                }
                if ((Actor.CurrentOccultType & OccultTypes.Witch) is not OccultTypes.None)
                {
                    Actor.Motives.SetValue(CommodityKind.MagicFatigue, -TunableSettings.kCurseMotiveAmount);
                }
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "CurseTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessageNegative));
                Actor.BuffManager.AddElement(HashString64("Gamefreak130_CursedBuff"), (Origin)HashString64("FromWonderPower"));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
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
            WonderPowerManager.PlayPowerSting("sting_lifetime_opp_success");
            bool flag = base.Run();
            Actor.UpdateWalkStyle();
            return flag;
        }

        public override void Cleanup()
        {
            try
            {
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "DivineInterventionTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
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
            WonderPowerManager.PlayPowerSting("sting_job_demote");
            mEffect = VisualEffect.Create("ep7WandSpellHauntingHit_main");
            mEffect.ParentTo(Actor, Sim.FXJoints.Head);
            mEffect.Start();

            string animName = Actor.SimDescription switch 
            {
                { IsFoal: true }                        => "ch_whinny_x",
                { IsHorse: true }                       => "ah_whinny_x",
                { IsPuppy: true }                       => "cd_react_stand_whimper_x",
                { IsFullSizeDog: true }                 => "ad_react_stand_whimper_x",
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
            try
            {
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                Actor.BuffManager.AddBuff(BuffNames.UnicornsIre, -40, 1440, false, MoodAxis.None, (Origin)HashString64("FromWonderPower"), true);
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "DoomTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessageNegative));
                BuffInstance buff = Actor.BuffManager.GetElement(BuffNames.UnicornsIre);
                buff.mBuffName = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DoomBuff";
                buff.mDescription = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DoomBuffDescription";
                // This will automatically trigger the BuffsChanged event, so the UI should refresh itself after this and we won't have to do it manually
                buff.SetThumbnail("doom", ProductVersion.BaseGame, Actor);
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
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

    public class ReactToGhost : GhostHunter.ReactToAngryGhost
    {
        new public class Definition : GhostHunter.ReactToAngryGhost.Definition
        {
            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                InteractionInstance instance = new ReactToGhost();
                instance.Init(ref parameters);
                return instance;
            }

            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => ShouldReactToAngryGhost(a);

            public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop) => base.GetInteractionName(actor, target, new(GhostHunter.ReactToAngryGhost.Singleton, target));
        }

        new public static InteractionDefinition Singleton = new Definition();

        private static bool ShouldReactToAngryGhost(Sim actor) => actor is { Service: not GrimReaper, OccupationAsActiveCareer: not GhostHunter, IsGhostOrHasGhostBuff: false };

        public override bool Run()
        {
            BeginCommodityUpdates();
            ActiveTopic.AddToSim(Actor, "Tell Ghost Story");
            if ((!Actor.TraitManager.HasAnyElement(new TraitNames[] { TraitNames.Brave, TraitNames.Daredevil }) && !Actor.BuffManager.HasElement(BuffNames.Blizzard)) || !RandomUtil.RandomChance01(kNotScaredChance))
            {
                if ((Actor.TraitManager.HasAnyElement(new TraitNames[] { TraitNames.Childish, TraitNames.Daredevil }) || Actor.BuffManager.HasAnyElement(new BuffNames[] { BuffNames.OddlyPowerful, BuffNames.Blizzard })) && Actor.MoodManager.MoodValue >= kPositiveReactionMinimumMood && RandomUtil.RandomChance01(kPositiveReactionChance))
                {
                    Actor.GhostReactionPositive(Target);
                }
                else
                {
                    Actor.BuffManager.AddElement(BuffNames.Scared, Origin.FromGhost);
                    ((BuffScared.BuffInstanceScared)Actor.BuffManager.GetElement(BuffNames.Scared)).ScaryObject = Target;
                    float num = kFaintChance;
                    if (Actor.HasTrait(TraitNames.Coward))
                    {
                        num *= kFaintChanceMultiplierCoward;
                    }
                    if (RandomUtil.RandomChance01(num))
                    {
                        InteractionPriority priority = new(Actor.InheritedPriority().Level, Actor.InheritedPriority().Value + 1f);
                        Actor.InteractionQueue.AddNext(TraitFunctions.CowardTraitFaint.Singleton.CreateInstance(Actor, Actor, priority, false, false));
                    }
                    else
                    {
                        Actor.GhostReactionNegative(Target);
                    }
                }
            }
            EndCommodityUpdates(true);
            return true;
        }
    }

    public class Ghostify : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<Ghostify>
        {
            public Definition()
            {
            }

            public Definition(SimDescription.DeathType ghostType) => mGhostType = ghostType;

            public SimDescription.DeathType mGhostType;

            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("GhostifyDialogTitle");
        }

        public static readonly SimDescription.DeathType[] sHumanDeathTypes =
        {
            SimDescription.DeathType.OldAge,
            SimDescription.DeathType.Drown,
            SimDescription.DeathType.Starve,
            SimDescription.DeathType.Electrocution,
            SimDescription.DeathType.Burn,
            SimDescription.DeathType.MummyCurse,
            SimDescription.DeathType.Meteor,
            SimDescription.DeathType.WateryGrave,
            SimDescription.DeathType.HumanStatue,
            SimDescription.DeathType.Transmuted,
            SimDescription.DeathType.HauntingCurse,
            SimDescription.DeathType.JellyBeanDeath,
            SimDescription.DeathType.Freeze,
            SimDescription.DeathType.BluntForceTrauma,
            SimDescription.DeathType.Ranting,
            SimDescription.DeathType.Shark,
            SimDescription.DeathType.ScubaDrown,
            SimDescription.DeathType.MermaidDehydrated,
            SimDescription.DeathType.Causality,
            SimDescription.DeathType.Jetpack,
            SimDescription.DeathType.FutureUrnstoneHologram
        };

        public override bool Run()
        {
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }

            Actor.SimDescription.mDeathStyle = (InteractionDefinition as Definition).mGhostType;
            if (Actor.SimDescription.SupernaturalData is CASGhostData casghostData)
            {
                casghostData.DeathStyle = Actor.SimDescription.mDeathStyle;
            }
            string name = (Actor.SimDescription.Age is not CASAgeGenderFlags.Child) ? "ep4PotionWearOff" : "ep4PotionWearOffChild";
            WonderPowerManager.PlayPowerSting("sting_ghost_appear", Actor.ObjectId);
            VisualEffect.FireOneShotEffect(name, Actor, Sim.FXJoints.Spine0, VisualEffect.TransitionType.SoftTransition);
            Urnstone.SimToPlayableGhost(Actor);

            string animName = Actor.SimDescription switch
            {
                { IsFoal: true }                        => "ch_trait_nervous_x",
                { IsHorse: true }                       => "ah_trait_nervous_x",
                { IsPuppy: true }                       => "cd_trait_adventurous_x",
                { IsFullSizeDog: true }                 => "ad_trait_adventurous_x",
                { IsLittleDog: true }                   => "al_trait_adventurous_x",
                { IsKitten: true }                      => "cc_trait_hyper_x",
                { IsCat: true }                         => "ac_trait_hyper_x",
                { Child: true }                         => "c_cas_flavor_checkOutSelf_bottom_child_average_x",
                { Elder: true }                         => "a_cas_flavor_checkOutSelf_bottom_elder_average_x",
                { TeenOrAbove: true, IsMale: true }     => "a_cas_flavor_checkOutSelf_bottom_male_average_x",
                { TeenOrAbove: true, IsFemale: true }   => "a_cas_flavor_checkOutSelf_bottom_female_average_x",
                _                                       => null
            };

            string animName2 = Actor.SimDescription switch
            {
                { IsPet: false, Child: true }        => "c_ghostify_x",
                { IsPet: false, TeenOrAbove: true }  => "a_ghostify_x",
                _                                    => null
            };

            if (!string.IsNullOrEmpty(animName))
            {
                Actor.PlaySoloAnimation(animName, true, Actor.IsPet ? ProductVersion.EP5 : ProductVersion.BaseGame);
            }
            DoTimedLoop(0.1f);
            if (!string.IsNullOrEmpty(animName2))
            {
                Actor.PlaySoloAnimation(animName2, true, ProductVersion.BaseGame);
            }
            return true;            
        }

        public override void Cleanup()
        {
            try
            {
                Actor.BuffManager.AddElement(HashString64("Gamefreak130_GhostifyBuff"), (Origin)HashString64("FromWonderPower"));
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "GhostifyTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }

    public class ActivateGoodMood : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<ActivateGoodMood>, IOverridesAgeTests
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("ActivateGoodMood");

            // Allow babies to perform interaction
            public SpecialCaseAgeTests GetSpecialCaseAgeTests() => SpecialCaseAgeTests.Standard ^ SpecialCaseAgeTests.DisallowIfActorIsBaby;
        }

        private VisualEffect mEffect;

        public override bool Run()
        {
            mEffect = VisualEffect.Create("ep4imaginaryfriendtransform");
            mEffect.SetPosAndOrient(Actor.Position, Actor.ForwardVector, Actor.UpVector);
            mEffect.Start();
            foreach (BuffInstance bi in new List<BuffInstance>(Actor.BuffManager.Buffs
                                                                                .Where(bi => bi is { Guid: not (BuffNames.Singed or BuffNames.HavingAMidlifeCrisis or BuffNames.HavingAMidlifeCrisisWithPromise or BuffNames.MalePregnancy), EffectValue: < 0 })))
            {
                Actor.BuffManager.ForceRemoveBuff(bi.Guid);
            }

            string animName = Actor.SimDescription switch
            {
                { Baby: true }           => "b_idle_breathe_x",
                { Toddler: true }        => "p_idle_sitting_chewHand_y",
                { IsFoal: true }         => "ch_trait_playful_x",
                { IsHorse: true }        => "ah_trait_playful_x",
                { IsPuppy: true }        => "cd_trait_playful_x",
                { IsFullSizeDog: true }  => "ad_trait_playful_x",
                { IsLittleDog: true }    => "al_trait_playful_x",
                { IsKitten: true }       => "cc_trait_playful_x",
                { IsCat: true }          => "ac_trait_playful_x",
                { Child: true }          => "c_trait_excitable_x",
                { TeenOrAbove: true }    => "a_trait_excitable_x",
                _                        => null
            };

            if (!string.IsNullOrEmpty(animName))
            {
                Actor.PlaySoloAnimation(animName, true, Actor.IsPet ? ProductVersion.EP5 : ProductVersion.BaseGame);
            }
            Actor.BuffManager.AddElement(HashString64("Gamefreak130_GoodMoodBuff"), (Origin)HashString64("FromWonderPower"));
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
            base.Cleanup();
        }
    }

    public class Beautify : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<Beautify>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("InstantBeautyDialogTitle");
        }

        private VisualEffect mEffect;

        public override bool Run()
        {
            GameStates.sSingleton.mInWorldState.GotoCASMode((InWorldState.SubState)HashString32("CASInstantBeautyState"));
            CASLogic singleton = CASLogic.GetSingleton();
            singleton.LoadSim(Actor.SimDescription, Actor.CurrentOutfitCategory, 0);
            singleton.UseTempSimDesc = true;
            while (GameStates.NextInWorldStateId is not InWorldState.SubState.LiveMode)
            {
                Simulator.Sleep(1U);
            }
            mEffect = VisualEffect.Create("ep4imaginaryfriendtransformthrow");
            mEffect.ParentTo(Actor, Actor.IsPet ? Sim.FXJoints.Head : Sim.FXJoints.Neck);
            mEffect.Start();
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }
            WonderPowerManager.PlayPowerSting("sting_instantbeauty");

            string animName = Actor.SimDescription switch
            {
                { Toddler: true }                      => "p_idle_sitting_pickNose_y",
                { IsFoal: true }                       => "ch_trait_nervous_x",
                { IsHorse: true }                      => "ah_trait_nervous_x",
                { IsPuppy: true }                      => "cd_trait_adventurous_x",
                { IsFullSizeDog: true }                => "ad_trait_adventurous_x",
                { IsLittleDog: true }                  => "al_trait_adventurous_x",
                { IsKitten: true }                     => "cc_trait_hyper_x",
                { IsCat: true }                        => "ac_trait_hyper_x",
                { Child: true }                        => "c_cas_flavor_checkOutSelf_top_child_average_x",
                { Elder: true }                        => "a_cas_flavor_checkOutSelf_top_elder_average_x",
                { TeenOrAbove: true, IsMale: true }    => "a_cas_flavor_checkOutSelf_top_male_average_x",
                { TeenOrAbove: true, IsFemale: true }  => "a_cas_flavor_checkOutSelf_top_female_average_x",
                _                                      => null
            };

            if (!string.IsNullOrEmpty(animName))
            {
                Actor.PlaySoloAnimation(animName, true, Actor.IsPet ? ProductVersion.EP5 : ProductVersion.BaseGame);
            }
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                Actor.BuffManager.AddElement(HashString64("Gamefreak130_InstantBeautyBuff"), (Origin)HashString64("FromWonderPower"));
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "InstantBeautyTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }

    public class ActivateLuckyBreak : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<ActivateLuckyBreak>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("LuckyBreakDialogTitle");
        }

        private VisualEffect mEffect;

        public override bool Run()
        {
            mEffect = VisualEffect.Create("ep7moodlampimpactgreen_main");
            mEffect.ParentTo(Actor, Actor.IsPet ? Sim.FXJoints.Head : Sim.FXJoints.Pelvis);
            mEffect.Start();
            WonderPowerManager.PlayPowerSting("sting_luckybreak");
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }

            string animName = Actor.SimDescription switch
            {
                { IsFoal: true }         => "ch_trait_brave_x",
                { IsHorse: true }        => "ah_trait_brave_x",
                { IsPuppy: true }        => "cd_trait_proud_x",
                { IsFullSizeDog: true }  => "ad_trait_proud_x",
                { IsLittleDog: true }    => "al_trait_proud_x",
                { IsKitten: true }       => "cc_trait_proud_x",
                { IsCat: true }          => "ac_trait_proud_x",
                _                        => null
            };

            if (animName is null)
            {
                Sim.CustomIdle customIdle = Sim.CustomIdle.Singleton.CreateInstance(Actor, Actor, GetPriority(), true, true) as Sim.CustomIdle;
                customIdle.Hidden = true;
                customIdle.JazzGraphName = "Trait_Lucky";
                customIdle.LoopTimes = 1;
                customIdle.ExtraWaitTime = 180;
                customIdle.RunInteraction();
            }
            else
            {
                Actor.PlaySoloAnimation(animName, true, ProductVersion.EP5);
            }
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                Actor.BuffManager.AddBuff(BuffNames.UnicornsBlessing, 40, 1440, false, MoodAxis.Happy, (Origin)HashString64("FromWonderPower"), true);
                BuffInstance buff = Actor.BuffManager.GetElement(BuffNames.UnicornsBlessing);
                buff.mBuffName = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_LuckyBreakBuff";
                buff.mDescription = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_LuckyBreakBuffDescription";
                // This will automatically trigger the BuffsChanged event, so the UI should refresh itself after this and we won't have to do it manually
                buff.SetThumbnail("moodlet_feelinglucky", ProductVersion.BaseGame, Actor);
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "LuckyBreakTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }

    public class ActivateLuckyFind : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<ActivateLuckyFind>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("LuckyFindDialogTitle");
        }

        private VisualEffect mEffect;

        public override bool Run()
        {
            mEffect = VisualEffect.Create("ep7ghostgoldmed_ghost");
            mEffect.ParentTo(Actor, Actor.IsPet ? Sim.FXJoints.Head : Sim.FXJoints.Neck);
            mEffect.Start();
            WonderPowerManager.PlayPowerSting("sting_luckyfind");
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }

            string animName = Actor.SimDescription switch
            {
                { IsFullSizeDog: true }  => "ad_trait_hunter_x",
                { IsLittleDog: true }    => "al_trait_hunter_x",
                { IsCat: true }          => "ac_trait_hunter_x",
                _                        => null
            };

            if (animName is null)
            {
                Sim.CustomIdle customIdle = Sim.CustomIdle.Singleton.CreateInstance(Actor, Actor, GetPriority(), true, true) as Sim.CustomIdle;
                customIdle.Hidden = true;
                customIdle.JazzGraphName = "TraitGatherer";
                customIdle.LoopTimes = 1;
                customIdle.RunInteraction();
            }
            else
            {
                Actor.PlaySoloAnimation(animName, true, ProductVersion.EP5);
            }
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                Actor.BuffManager.AddElement(BuffLuckyFind.kBuffLuckyFindGuid, (Origin)HashString64("FromWonderPower"));
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "LuckyFindTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }

    public class ActivateRayOfSunshine : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<ActivateRayOfSunshine>, IOverridesAgeTests
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("RayOfSunshineDialogTitle");

            // Allow babies to perform interaction
            public SpecialCaseAgeTests GetSpecialCaseAgeTests() => SpecialCaseAgeTests.Standard ^ SpecialCaseAgeTests.DisallowIfActorIsBaby;
        }

        private VisualEffect mEffect;

        public override bool Run()
        {
            mEffect = VisualEffect.Create("ep11robotsunrays_main");
            mEffect.ParentTo(Actor, Sim.FXJoints.Head);
            mEffect.Start();
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }
            WonderPowerManager.PlayPowerSting("sting_rayofsunshine");
            string animName = Actor.SimDescription switch
            {
                { Baby: true }           => "b_idle_breathe_x",
                { Toddler: true }        => "p_idle_sitting_grabFeet_y",
                { IsFoal: true }         => "ch_trait_playful_x",
                { IsHorse: true }        => "ah_trait_playful_x",
                { IsPuppy: true }        => "cd_trait_playful_x",
                { IsFullSizeDog: true }  => "ad_trait_playful_x",
                { IsLittleDog: true }    => "al_trait_playful_x",
                { IsKitten: true }       => "cc_trait_playful_x",
                { IsCat: true }          => "ac_trait_playful_x",
                { Child: true }          => "c_rayofsunshine_x",
                { TeenOrAbove: true }    => "a_rayofsunshine_x",
                _                        => null
            };

            if (animName is not null)
            {
                Actor.PlaySoloAnimation(animName, true, Actor.IsPet ? ProductVersion.EP5 : ProductVersion.BaseGame);
            }
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                foreach (CommodityKind motive in (Responder.Instance.HudModel as HudModel).GetMotives(Actor))
                {
                    Actor.Motives.ChangeValue(motive, TunableSettings.kRayOfSunshineBoostAmount);
                }
                if ((Actor.CurrentOccultType & OccultTypes.Fairy) is not OccultTypes.None)
                {
                    Actor.Motives.ChangeValue(CommodityKind.AuraPower, TunableSettings.kRayOfSunshineBoostAmount);
                }
                if ((Actor.CurrentOccultType & OccultTypes.Witch) is not OccultTypes.None)
                {
                    Actor.Motives.ChangeValue(CommodityKind.MagicFatigue, -TunableSettings.kRayOfSunshineBoostAmount);
                }
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                Actor.BuffManager.AddElement((BuffNames)HashString64("Gamefreak130_BoostedBuff"), (Origin)HashString64("FromWonderPower"));
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "RayOfSunshineTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }

    public class SuperSatisfy : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<SuperSatisfy>, IOverridesAgeTests
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("SatisfactionDialogTitle");

            // Allow babies to perform interaction
            public SpecialCaseAgeTests GetSpecialCaseAgeTests() => SpecialCaseAgeTests.Standard ^ SpecialCaseAgeTests.DisallowIfActorIsBaby;
        }

        private VisualEffect mEffect;

        public override bool Run()
        {
            mEffect = VisualEffect.Create("ep7moodlampimpactyellow_main");
            mEffect.ParentTo(Actor, Actor.IsPet || Actor.SimDescription.Baby ? Sim.FXJoints.Head : Sim.FXJoints.Pelvis);
            mEffect.Start();
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }
            // This sting typo physically pains me
            WonderPowerManager.PlayPowerSting("sting_dream_fullfill");
            string animName = Actor.SimDescription switch
            {
                { Baby: true }           => "b_idle_breathe_x",
                { Toddler: true }        => "p_react_laugh1_y",
                { IsFoal: true }         => "ch_trait_playful_x",
                { IsHorse: true }        => "ah_trait_playful_x",
                { IsPuppy: true }        => "cd_trait_playful_x",
                { IsFullSizeDog: true }  => "ad_trait_playful_x",
                { IsLittleDog: true }    => "al_trait_playful_x",
                { IsKitten: true }       => "cc_trait_playful_x",
                { IsCat: true }          => "ac_trait_playful_x",
                { Child: true }          => "c_standing_whew2",
                { TeenOrAbove: true }    => "a_standing_whew2",
                _                        => null
            };

            if (animName is not null)
            {
                Actor.PlaySoloAnimation(animName, true, Actor.IsPet ? ProductVersion.EP5 : ProductVersion.BaseGame);
            }
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                foreach (CommodityKind motive in (Responder.Instance.HudModel as HudModel).GetMotives(Actor))
                {
                    Actor.Motives.SetMax(motive);
                }
                if ((Actor.CurrentOccultType & OccultTypes.Fairy) is not OccultTypes.None)
                {
                    Actor.Motives.SetMax(CommodityKind.AuraPower);
                }
                if ((Actor.CurrentOccultType & OccultTypes.Witch) is not OccultTypes.None)
                {
                    Actor.Motives.SetValue(CommodityKind.MagicFatigue, Actor.Motives.GetMin(CommodityKind.MagicFatigue));
                }
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                Actor.BuffManager.AddElement((BuffNames)HashString64("Gamefreak130_SatisfiedBuff"), (Origin)HashString64("FromWonderPower"));
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "SatisfactionTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }

    public class ActivateStrokeOfGenius : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<ActivateStrokeOfGenius>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("StrokeOfGeniusDialogTitle");
        }

        private VisualEffect mEffect;

        public override bool Run()
        {
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }
            mEffect = VisualEffect.Create("ep2inventiondiscovery");
            mEffect.ParentTo(Actor, Sim.FXJoints.Head);
            mEffect.Start();
            WonderPowerManager.PlayPowerSting("sting_strokeofgenius");
            string animName = Actor.SimDescription switch
            {
                { IsFoal: true }         => "ch_trait_genius_x",
                { IsHorse: true }        => "ah_trait_genius_x",
                { IsPuppy: true }        => "cd_trait_genius_x",
                { IsFullSizeDog: true }  => "ad_trait_genius_x",
                { IsLittleDog: true }    => "al_trait_genius_x",
                { IsKitten: true }       => "cc_trait_genius_x",
                { IsCat: true }          => "ac_trait_genius_x",
                { Child: true }          => "c_strokeofgenius_x",
                { TeenOrAbove: true }    => "a_trait_genius_x",
                _                        => null
            };

            if (animName is not null)
            {
                Actor.PlaySoloAnimation(animName, true, Actor.IsPet ? ProductVersion.EP5 : ProductVersion.BaseGame);
            }
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                Actor.BuffManager.AddElement(BuffStrokeOfGenius.kBuffStrokeOfGeniusGuid, (Origin)HashString64("FromWonderPower"));
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "StrokeOfGeniusTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }

    public class ActivateSuperLucky : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<ActivateSuperLucky>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("SuperLuckyDialogTitle");
        }

        private VisualEffect mEffect;

        public override bool Run()
        {
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }
            mEffect = VisualEffect.Create("ep7wandspellluck_main");
            mEffect.ParentTo(Actor, Actor.IsPet ? Sim.FXJoints.Head : Sim.FXJoints.Pelvis);
            mEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
            AlarmManager.Global.AddAlarm(1f, TimeUnit.Minutes, () => WonderPowerManager.PlayPowerSting("sting_superlucky"), "Gamefreak130 wuz here -- Super Lucky sting alarm", AlarmType.NeverPersisted, null);

            string animName = Actor.SimDescription switch
            {
                { IsFoal: true }         => "ch_trait_friendly_x",
                { IsHorse: true }        => "ah_trait_friendly_x",
                { IsPuppy: true }        => "cd_trait_friendly_x",
                { IsFullSizeDog: true }  => "ad_trait_friendly_x",
                { IsLittleDog: true }    => "al_trait_friendly_x",
                { IsKitten: true }       => "cc_trait_friendly_x",
                { IsCat: true }          => "ac_trait_friendly_x",
                { Child: true }          => "c_superlucky_x",
                { TeenOrAbove: true}     => "a_superlucky_x",
                _                        => null
            };

            if (animName is not null)
            {
                Actor.PlaySoloAnimation(animName, true, Actor.IsPet ? ProductVersion.EP5 : ProductVersion.BaseGame);
            }
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                Actor.BuffManager.AddElement(BuffSuperLucky.kBuffSuperLuckyGuid, (Origin)HashString64("FromWonderPower"));
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "SuperLuckyTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }

    public class StartTransmogrify : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<StartTransmogrify>
        {
            public Definition()
            {
            }

            public Definition(CASAgeGenderFlags newSpecies) => mNewSpecies = newSpecies;

            public CASAgeGenderFlags mNewSpecies;

            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("TransmogrifyDialogTitle");
        }

        private Sim mNewSim;

        public override bool Run()
        {
            SimDescription oldDescription = Actor.SimDescription;
            CASAgeGenderFlags newSpecies = (InteractionDefinition as Definition).mNewSpecies;
            CASAgeGenderFlags newAge = oldDescription.Age switch
            {
                CASAgeGenderFlags.Toddler or CASAgeGenderFlags.Child or CASAgeGenderFlags.Teen  => CASAgeGenderFlags.Child,
                CASAgeGenderFlags.YoungAdult or CASAgeGenderFlags.Adult                         => CASAgeGenderFlags.Adult,
                CASAgeGenderFlags.Elder                                                         => CASAgeGenderFlags.Elder,
                _                                                                               => CASAgeGenderFlags.None
            };
            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }
            WonderPowerManager.PlayPowerSting(newSpecies is CASAgeGenderFlags.Human ? "sting_transmogrifytohuman" : "sting_transmogrifytopet", Actor.Position);
            VisualEffect effect = VisualEffect.Create("ep11portalspawn_main");
            effect.SetPosAndOrient(Actor.Position, Actor.ForwardVector, Actor.UpVector);
            effect.Start();

            DoTimedLoop(0.5f, ExitReason.Default, 0);

            string animName = oldDescription switch
            {
                { Toddler: true }        => "p_idle_sitting_grabFeet_y",
                { IsFoal: true }         => "ch_whinny_x",
                { IsHorse: true }        => "ah_whinny_x",
                { IsPuppy: true }        => "cd_react_stand_whimper_x",
                { IsFullSizeDog: true }  => "ad_react_stand_whimper_x",
                { IsLittleDog: true }    => "al_react_stand_whimper_x",
                { IsKitten: true }       => "cc_petNeeds_standing_hunger_whinyMeow_x",
                { IsCat: true }          => "ac_petNeeds_standing_hunger_whinyMeow_x",
                { Child: true }          => "c_buff_wallFlower_x",
                { TeenOrAbove: true }    => "a_buff_wallFlower_x",
                _                        => null
            };

            if (!string.IsNullOrEmpty(animName))
            {
                Actor.PlaySoloAnimation(animName, false, Actor.IsPet ? ProductVersion.EP5 : ProductVersion.BaseGame);
            }

            DoTimedLoop(2f, ExitReason.Default, 0);

            bool turnIntoUnicorn = (oldDescription.IsGenie || oldDescription.IsWitch || oldDescription.IsFairy) && newSpecies is CASAgeGenderFlags.Horse;
            SimDescription newDescription = newSpecies is CASAgeGenderFlags.Human
                                          ? Genetics.MakeSim(newAge, oldDescription.Gender, oldDescription.HomeWorld, uint.MaxValue)
                                          : turnIntoUnicorn
                                          ? GeneticsPet.MakePet(newAge, oldDescription.Gender, newSpecies, OccultUnicorn.NPCOutfit(oldDescription.IsFemale, newAge))
                                          : GeneticsPet.MakeRandomPet(newAge, oldDescription.Gender, newSpecies);

            Relationship.sAllRelationships[newDescription] = new();
            foreach (Relationship oldRelationship in Relationship.GetRelationships(oldDescription))
            {
                if (oldRelationship is not null)
                {
                    Sim otherSim = oldRelationship.GetOtherSim(Actor);
                    if (otherSim is not null)
                    {
                        Relationship newRelationship = new(newDescription, otherSim.SimDescription);
                        Relationship.sAllRelationships[newDescription].Add(otherSim.SimDescription, newRelationship);
                        // If game is using asymmetric relationships, copy each half of the pair
                        // Otherwise, both Sims will share a common relationship
                        Relationship oldRelationship2 = Relationship.Get(otherSim, Actor, false);
                        if (oldRelationship != oldRelationship2)
                        {
                            Relationship newRelationship2 = new(otherSim.SimDescription, newDescription);
                            Relationship.sAllRelationships[otherSim.SimDescription].Add(newDescription, newRelationship2);
                            newRelationship2.LTR.CopyLtr(oldRelationship2.LTR);
                            newRelationship2.LTR.UpdateLTR();
                        }
                        else
                        {
                            Relationship.sAllRelationships[otherSim.SimDescription].Add(newDescription, newRelationship);
                        }
                        newRelationship.LTR.CopyLtr(oldRelationship.LTR);
                        newRelationship.LTR.UpdateLTR();
                    }
                }
            }
            newDescription.FirstName = oldDescription.FirstNameUnlocalized;
            newDescription.LastName = oldDescription.LastNameUnlocalized;
            newDescription.Bio = oldDescription.BioUnlocalized;
            newDescription.VoicePitchModifier = oldDescription.VoicePitchModifier;
            if (newSpecies is CASAgeGenderFlags.Human)
            {
                newDescription.VoiceVariation = (VoiceVariationType)RandomUtil.GetInt(2);
            }
            newDescription.mLifetimeHappiness = oldDescription.mLifetimeHappiness;
            newDescription.mSpendableHappiness = oldDescription.mSpendableHappiness;

            // Pick traits for new Sim based on old Sim's traits
            IEnumerable<TraitNames> mappingTraits = oldDescription.TraitManager.List
                                                                               .Where(trait => trait.IsVisible)
                                                                               .SelectMany(trait => TransmogrifyTraitMapping.sInstance.GetMappedTraits(newDescription, trait.Guid));
            List<TraitNames> traitsToAdd = mappingTraits.Distinct().ToList();
            List<float> weightsToAdd = Enumerable.Repeat(0f, traitsToAdd.Count).ToList();
            foreach (TraitNames mappingTrait in mappingTraits)
            {
                weightsToAdd[traitsToAdd.IndexOf(mappingTrait)]++;
            }

            // Manually tune the weight of the unstable trait, since it is mapped to every pet trait
            int unstableIndex = traitsToAdd.IndexOf(TraitNames.Unstable);
            if (unstableIndex > -1)
            {
                weightsToAdd[unstableIndex] = 0.75f;
            }

            int numToAdd = newSpecies is CASAgeGenderFlags.Human ? newDescription.TraitManager.NumTraitsForAge() : 3;
            while (traitsToAdd.Count > 0 && newDescription.CountVisibleTraits() < numToAdd)
            {
                TraitNames traitToAdd = RandomUtil.GetWeightedRandomObjectFromList(weightsToAdd, traitsToAdd);
                newDescription.TraitManager.AddElement(traitToAdd);
                int indexAdded = traitsToAdd.IndexOf(traitToAdd);
                traitsToAdd.RemoveAt(indexAdded);
                weightsToAdd.RemoveAt(indexAdded);
            }
            // If not all trait slots are filled out, fill the remaining slots out at random
            newDescription.TraitManager.AddRandomTrait(numToAdd - newDescription.CountVisibleTraits());

            // CONSIDER copy over stattrackers and/or VisaManager
            LifeEventManager newManager = newDescription.LifeEventManager, oldManager = oldDescription.LifeEventManager;
            newManager.ProcessPendingLifeEvents(true);
            newManager.mActiveNodes = oldManager.mActiveNodes;
            newManager.mCurrentNumberOfVisibleLifeEvents = oldManager.mCurrentNumberOfVisibleLifeEvents;
            newManager.mHasShownWarningDialog = oldManager.mHasShownWarningDialog;
            newManager.mLifeEvents = oldManager.mLifeEvents;
            newManager.mTimeOfDeath = oldManager.mTimeOfDeath;

            if (turnIntoUnicorn)
            {
                newDescription.OccultManager.AddOccultType(OccultTypes.Unicorn, true, false, false);
                SkillManager skillManager = newDescription.SkillManager;
                Racing skill = skillManager.AddElement(SkillNames.Racing) as Racing;
                skill.ForceSkillLevelUp(10);
                Jumping skill2 = skillManager.AddElement(SkillNames.Jumping) as Jumping;
                skill2.ForceSkillLevelUp(10);
            }
            if (oldDescription.IsUnicorn && newSpecies is CASAgeGenderFlags.Human)
            {
                newDescription.OccultManager.AddOccultType(RandomUtil.GetRandomObjectFromList(OccultTypes.Fairy, OccultTypes.Witch, OccultTypes.Genie), true, false, false);
            }
            if (oldDescription.IsGhost)
            {
                newDescription.IsGhost = true;
                newDescription.mDeathStyle = oldDescription.mDeathStyle;
            }
            Actor.Household.Add(newDescription);
            oldDescription.Genealogy.ClearAllGenealogyInformation();

            if (Actor.IsActiveSim)
            {
                UserToolUtils.OnClose();
                LotManager.SelectNextSim();
            }
            mNewSim = newDescription.Instantiate(Actor.Position);

            // Stop the effect before we go into CAS
            // Otherwise it'll restart from the beginning
            if (!(newDescription.IsPet && newDescription.Child))
            {
                effect.Stop();
                effect.Dispose();
            }
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                FinishTransmogrify finishTransmogrify = new FinishTransmogrify.Definition(Actor).CreateInstance(mNewSim, mNewSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as FinishTransmogrify;
                mNewSim.InteractionQueue.AddNext(finishTransmogrify);
            }
            catch
            {
                WonderPowerManager.TogglePowerRunning();
                throw;
            }
        }
    }

    public class FinishTransmogrify : Interaction<Sim, Sim>
    {
        [DoesntRequireTuning]
        public class Definition : SoloSimInteractionDefinition<FinishTransmogrify>
        {
            public Definition()
            {
            }

            public Definition(Sim oldSim) => mOldSim = oldSim;

            public Sim mOldSim;

            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => actor == target;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("TransmogrifyDialogTitle");
        }

        public override bool Run()
        {
            Sim oldSim = (InteractionDefinition as Definition).mOldSim;
            oldSim.Destroy();
            oldSim.SimDescription.Dispose();

            if (Actor.IsInActiveHousehold)
            {
                foreach (INotTransferableOnDeath notTransferableOnDeath in oldSim.Inventory.FindAll<INotTransferableOnDeath>(false))
                {
                    notTransferableOnDeath.Destroy();
                }
                List<IGameObject> dolls = new();
                if (GameUtils.IsInstalled(ProductVersion.EP4))
                {
                    ulong simDescriptionId = oldSim.SimDescription.SimDescriptionId;
                    foreach (IImaginaryDoll imaginaryDoll in oldSim.Inventory.FindAll<IImaginaryDoll>(false))
                    {
                        if (imaginaryDoll.GetOwnerSimDescriptionId() == simDescriptionId)
                        {
                            if (oldSim.Inventory.TryToRemove(imaginaryDoll))
                            {
                                dolls.Add(imaginaryDoll);
                            }
                            else
                            {
                                imaginaryDoll.Destroy();
                            }
                        }
                    }
                }
                if (!oldSim.Inventory.IsEmpty)
                {
                    oldSim.Inventory.MoveObjectsTo(Actor.Inventory);
                }
                foreach (IGameObject gameObject in dolls)
                {
                    if (!oldSim.Inventory.TryToAdd(gameObject))
                    {
                        gameObject.Destroy();
                    }
                }
            }
            oldSim.Dispose();
            foreach (LifeEventManager.LifeEventActiveNode node in Actor.LifeEventManager.mActiveNodes.SelectMany(kvp => kvp.Value))
            {
                node.mOwner = Actor;
            }

            if (Actor.SimDescription.IsGhost)
            {
                Urnstone.SimToPlayableGhost(Actor);
            }

            if (CauseEffectService.GetInstance() is CauseEffectService service && service.GetTimeAlmanacTimeStatueData() is List<ITimeStatueUiData> timeAlmanacTimeStatueData)
            {
                foreach (ITimeStatueUiData timeStatueUiData in timeAlmanacTimeStatueData)
                {
                    if (timeStatueUiData is TimeStatueRecordData timeStatueRecordData && timeStatueRecordData.mRecordHolderId == oldSim.SimDescription.SimDescriptionId)
                    {
                        timeStatueRecordData.mRecordHolderId = 0UL;
                    }
                }
            }

            if (!(Actor.IsPet && Actor.SimDescription.Child))
            {
                GameStates.sSingleton.mInWorldState.GotoCASMode((InWorldState.SubState)HashString32("CASTransmogrifyState"));
                CASLogic singleton = CASLogic.GetSingleton();
                singleton.LoadSim(Actor.SimDescription, Actor.CurrentOutfitCategory, 0);
                singleton.UseTempSimDesc = true;
                while (GameStates.NextInWorldStateId is not InWorldState.SubState.LiveMode)
                {
                    Simulator.Sleep(1U);
                }
            }

            Camera.FocusOnSim(Actor);
            if (Actor.IsSelectable)
            {
                PlumbBob.SelectActor(Actor);
            }

            string animName = Actor.SimDescription switch
            {
                { IsFoal: true }                       => "ch_trait_nervous_x",
                { IsHorse: true }                      => "ah_trait_nervous_x",
                { IsPuppy: true }                      => "cd_trait_adventurous_x",
                { IsFullSizeDog: true }                => "ad_trait_adventurous_x",
                { IsLittleDog: true }                  => "al_trait_adventurous_x",
                { IsKitten: true }                     => "cc_trait_hyper_x",
                { IsCat: true }                        => "ac_trait_hyper_x",
                { Child: true }                        => "c_cas_flavor_checkOutSelf_top_child_average_x",
                { Elder: true }                        => "a_cas_flavor_checkOutSelf_top_elder_average_x",
                { TeenOrAbove: true, IsMale: true }    => "a_cas_flavor_checkOutSelf_top_male_average_x",
                { TeenOrAbove: true, IsFemale: true }  => "a_cas_flavor_checkOutSelf_top_female_average_x",
                _                                      => null
            };

            if (animName is not null)
            {
                Actor.PlaySoloAnimation(animName, true, Actor.IsPet ? ProductVersion.EP5 : ProductVersion.BaseGame);
            }

            if (Actor.BuffManager.AddElement((BuffNames)BuffTransmogrify.kBuffTransmogrifyGuid, (Origin)HashString64("FromWonderPower")))
            {
                BuffTransmogrify.BuffInstanceTransmogrify transmogrifyBuffInstance = Actor.BuffManager.GetElement(BuffTransmogrify.kBuffTransmogrifyGuid) as BuffTransmogrify.BuffInstanceTransmogrify;
                BuffTransmogrify.TransmogType transmogType = Actor switch
                {
                    { IsADogSpecies: true }  => BuffTransmogrify.TransmogType.ToDog,
                    { IsCat: true }          => BuffTransmogrify.TransmogType.ToCat,
                    { IsHorse: true }        => BuffTransmogrify.TransmogType.ToHorse,
                    _                        => BuffTransmogrify.TransmogType.ToHuman
                };
                transmogrifyBuffInstance.SetTransmogType(transmogType, Actor);
            }
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "TransmogrifyTNS", Actor), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
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
            WonderPowerManager.PlayPowerSting("sting_wealth");
            EnterStateMachine("ReceiveMagicalCheck", "WinLottoEnter", "x");
            AnimateSim("PullOutCheck");
            AnimateSim("VictoryDance");
            AnimateSim("WinLottoExit");
            Actor.BuffManager.AddElement((BuffNames)HashString64("Gamefreak130_WealthBuff"), (Origin)HashString64("FromWonderPower"));
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                int amount = RandomUtil.GetInt(TunableSettings.kWealthMinAmount, TunableSettings.kWealthMaxAmount);
                Actor.Household.ModifyFamilyFunds(amount);
                StyledNotification.Show(new(WonderPowerManager.LocalizeString(Actor.IsFemale, "WealthTNS", Actor, amount), Actor.ObjectId, StyledNotification.NotificationStyle.kGameMessagePositive));
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }
}