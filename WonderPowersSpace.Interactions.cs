using Gamefreak130.WonderPowersSpace.Helpers;
using Sims3.Gameplay;
using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.UI;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
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
                Actor.BuffManager.AddElement(Buffs.BuffCryHavoc.kBuffCryHavocGuid, (Origin)HashString64("FromWonderPower"));
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
            Audio.StartSound("sting_curse");
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
                Actor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(Actor.IsFemale, "CurseTNS", Actor), StyledNotification.NotificationStyle.kGameMessageNegative);
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
            Audio.StartSound("sting_lifetime_opp_success");
            bool flag = base.Run();
            Actor.UpdateWalkStyle();
            return flag;
        }

        public override void Cleanup()
        {
            try
            {
                Actor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(Actor.IsFemale, "DivineInterventionTNS", Actor), StyledNotification.NotificationStyle.kGameMessagePositive);
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
            Audio.StartSound("sting_job_demote");
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
                Actor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(Actor.IsFemale, "DoomTNS", Actor), StyledNotification.NotificationStyle.kGameMessageNegative);
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
            Audio.StartObjectSound(Actor.ObjectId, "sting_ghost_appear", false);
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
                Actor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(Actor.IsFemale, "GhostifyTNS", Actor), StyledNotification.NotificationStyle.kGameMessagePositive);
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
        public class Definition : SoloSimInteractionDefinition<ActivateGoodMood>
        {
            public Definition()
            {
            }

            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

            public override string GetInteractionName(ref InteractionInstanceParameters parameters) => WonderPowerManager.LocalizeString("ActivateGoodMood");
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
                { IsFoal: true }         => "ch_trait_playful_x",
                { IsHorse: true }        => "ah_trait_playful_x",
                { IsPuppy: true }        => "cd_trait_playful_x",
                { IsFullSizeDog: true }  => "ad_trait_playful_x",
                { IsLittleDog: true }    => "al_trait_playful_x",
                { IsKitten: true }       => "cc_trait_playful_x",
                { IsCat: true }          => "ac_trait_playful_x",
                { Child: true }          => "a_trait_excitable_x",
                { TeenOrAbove: true }    => "c_trait_excitable_x",
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
            Audio.StartSound("sting_instantbeauty");

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
                Actor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString("InstantBeautyTNS"), StyledNotification.NotificationStyle.kGameMessagePositive);
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
            Audio.StartSound("sting_luckyfind");
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
                Actor.BuffManager.AddElement(Buffs.BuffLuckyFind.kBuffLuckyFindGuid, (Origin)HashString64("FromWonderPower"));
                Actor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString("LuckyFindTNS"), StyledNotification.NotificationStyle.kGameMessagePositive);
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
            Audio.StartSound("sting_luckybreak");
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
                Actor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString("LuckyBreakTNS"), StyledNotification.NotificationStyle.kGameMessagePositive);
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
            try
            {
                int amount = RandomUtil.GetInt(TunableSettings.kWealthMinAmount, TunableSettings.kWealthMaxAmount);
                Actor.Household.ModifyFamilyFunds(amount);
                Actor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(Actor.IsFemale, "WealthTNS", Actor, amount), StyledNotification.NotificationStyle.kGameMessagePositive);
                base.Cleanup();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }
    }
}