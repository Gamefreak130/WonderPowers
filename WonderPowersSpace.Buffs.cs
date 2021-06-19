using Gamefreak130.Common.Buffs;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Sims3.UI.CAS;
using System.Collections.Generic;
using System.Linq;
using static Sims3.Gameplay.ActorSystems.BuffCommodityDecayModifier;
using static Sims3.SimIFace.ResourceUtils;
using Responder = Sims3.UI.Responder;

namespace Gamefreak130.WonderPowersSpace.Buffs
{
    public class BuffCryHavoc : Buff
    {
        public const ulong kBuffCryHavocGuid = 0x9DFC9F7522618833;

        public BuffCryHavoc(BuffData data) : base(data)
        {
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition) => CruiseForBruise(bm.Actor);

        private static void CruiseForBruise(Sim actor) => Simulator.AddObject(new OneShotFunctionWithParams(CruiseForBruiseInternal, actor));

        private static void CruiseForBruiseInternal(object arg)
        {
            Sim actor = arg as Sim;
            if (actor.BuffManager.HasElement(kBuffCryHavocGuid))
            {
                if (actor.CurrentInteraction is null || actor.CurrentInteraction.GetPriority().Level < InteractionPriorityLevel.CriticalNPCBehavior)
                {
                    Sim target = RandomUtil.GetRandomObjectFromList(actor.LotCurrent.GetAllActors());
                    if (CanFight(actor, target))
                    {
                        string social = RandomUtil.GetRandomStringFromList(actor.IsPet ? TunableSettings.kCryHavocPetInteractions : TunableSettings.kCryHavocSimInteractions);
                        Common.Helpers.ForceSocial(actor, target, social, InteractionPriorityLevel.CriticalNPCBehavior, false);
                    }
                }
                actor.AddAlarm(1f, TimeUnit.Seconds, delegate { CruiseForBruise(actor); }, "Gamefreak130 wuz here -- Cry Havoc alarm", AlarmType.DeleteOnReset);
            }
        }

        private static bool CanFight(Sim x, Sim y)
            => y is not null && (y.CurrentInteraction is null || y.CurrentInteraction.GetPriority().Level < InteractionPriorityLevel.CriticalNPCBehavior) && x != y 
            && x.BuffManager.HasElement(kBuffCryHavocGuid) && y.BuffManager.HasElement(kBuffCryHavocGuid)
            && x.IsPet == y.IsPet && ((x.SimDescription.Teen && y.SimDescription.Teen) || (x.SimDescription.YoungAdultOrAbove && y.SimDescription.YoungAdultOrAbove));
    }

    public class BuffGhostify : BuffTheUndead
    {
        public class BuffInstanceGhostify : BuffInstance
        {
            public BuffInstanceGhostify()
            {
            }

            public BuffInstanceGhostify(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount) : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            internal uint mGhostType;

            public override BuffInstance Clone() => new BuffInstanceGhostify(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);

            public override bool OnLoadFixup(Sim actor)
            {
                bool flag = base.OnLoadFixup(actor);
                World.ObjectSetGhostState(actor.ObjectId, mGhostType, (uint)actor.SimDescription.AgeGenderSpecies);
                return flag;
            }
        }

        public const ulong kBuffGhostifyGuid = 0xABECC1DBFB07E2B9;

        private static readonly SimDescription.DeathType[] sHumanDeathTypes =
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

        public BuffGhostify(BuffData data) : base(data)
        {
        }

        public override BuffInstance CreateBuffInstance() => new BuffInstanceGhostify(this, BuffGuid, EffectValue, TimeoutSimMinutes);

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            if (bi is BuffInstanceGhostify biGhostify)
            {
                Sim actor = bm.Actor;
                biGhostify.mGhostType = SelectGhostType(actor);
                string name = (actor.SimDescription.Age is not CASAgeGenderFlags.Child) ? "ep4PotionWearOff" : "ep4PotionWearOffChild";
                Audio.StartObjectSound(actor.ObjectId, "sting_ghost_appear", false);
                VisualEffect.FireOneShotEffect(name, actor, Sim.FXJoints.Spine0, VisualEffect.TransitionType.SoftTransition);
                World.ObjectSetGhostState(actor.ObjectId, biGhostify.mGhostType, (uint)actor.SimDescription.AgeGenderSpecies);
                actor.RequestWalkStyle(Sim.WalkStyle.GhostWalk);
            }
        }

        private static uint SelectGhostType(Sim sim)
        {
            SimDescription.DeathType ghostType = SimDescription.DeathType.None;
            if (sim is not null)
            {
                if (!sim.IsHuman)
                {
                    return (uint)Common.Helpers.CoinFlipSelect(SimDescription.DeathType.PetOldAgeGood, SimDescription.DeathType.PetOldAgeBad);
                }
                    
                List<ObjectPicker.HeaderInfo> list = new()
                {
                    new("Ui/Caption/ObjectPicker:Ghost", "Ui/Caption/ObjectPicker:Ghost", 300)
                };

                List<ObjectPicker.RowInfo> list2 = sHumanDeathTypes.Select((deathType, i) => {
                    return new ObjectPicker.RowInfo(deathType, new()
                    {
                        new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(ResourceKey.CreatePNGKey(CASBasics.mGhostDeathNames[i], 0u), ThumbnailSize.ExtraLarge), Urnstone.DeathTypeToLocalizedString(deathType))
                    });
                }).ToList();

                List<ObjectPicker.TabInfo> list3 = new()
                {
                    new("shop_all_r2", Helpers.WonderPowerManager.LocalizeString("SelectGhost"), list2)
                };

                while (ghostType is SimDescription.DeathType.None)
                {
                    List<ObjectPicker.RowInfo> selection = ObjectPickerDialog.Show(true, ModalDialog.PauseMode.PauseSimulator, Helpers.WonderPowerManager.LocalizeString("GhostifyDialogTitle"), Localization.LocalizeString("Ui/Caption/ObjectPicker:OK"), 
                                                                                    Localization.LocalizeString("Ui/Caption/ObjectPicker:Cancel"), list3, list, 1);
                    ghostType = selection is not null ? (SimDescription.DeathType)selection[0].Item : SimDescription.DeathType.None;
                }
            }
            return (uint)ghostType;
        }
    }

    public class BuffLuckyFind : BuffTemporaryTraitEx
    {
        public const ulong kBuffLuckyFindGuid = 0x2D42AC8E858A22A7;

        public BuffLuckyFind(BuffData data) : base(data)
        {
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            if (bi is BuffInstanceTemporaryTraitEx luckyFind)
            {
                luckyFind.AddTemporaryTrait(bm.Actor.IsPet ? TraitNames.HunterPet : TraitNames.GathererTrait);
            }
        }
    }

    public class BuffKarmicSickness : BuffPestilencePlague
    {
        public class BuffInstanceKarmicSickness : BuffInstancePestilencePlague
        {
            public BuffInstanceKarmicSickness()
            {
            }

            public BuffInstanceKarmicSickness(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount) : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            public override BuffInstance Clone() => new BuffInstanceKarmicSickness(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);

            new public void AdvancePlagueStage() => mCoughingFitAlarm = mPlaguedSim.AddAlarm(RandomUtil.GetFloat(MaxTimeBetweenCoughingFits - MinTimeBetweenCoughingFits) + MinTimeBetweenCoughingFits, TimeUnit.Minutes, DoCoughingFit, "Gamefreak130 wuz here -- Sickness cough alarm", AlarmType.DeleteOnReset);

            new private void DoCoughingFit()
            {
                mPlaguedSim.InteractionQueue.AddNext(CoughingFit.Singleton.CreateInstance(mPlaguedSim, mPlaguedSim, new(InteractionPriorityLevel.High), true, false));
                AdvancePlagueStage();
            }
        }

        new public class CoughingFit : Interaction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : SoloSimInteractionDefinition<CoughingFit>, ISoloInteractionDefinition
            {
                public static string LocalizeString(string name, params object[] parameters) => Localization.LocalizeString(sLocalizationKey + name, parameters);

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop) => LocalizeString("CoughingFit", new object[0]);

                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => a == target && isAutonomous;
            }

            public const string sLocalizationKey = "Gameplay/ActorSystems/BuffPestilencePlague/CoughingFit:";

            public static ISoloInteractionDefinition Singleton = new Definition();

            public static string LocalizeString(string name, params object[] parameters) => Localization.LocalizeString(sLocalizationKey + name, parameters);

            public override bool Run()
            {
                StandardEntry();
                ReactionBroadcaster reactionBroadcaster = new(Actor, BuffSickAndTired.DisgustSimsBroadcasterParams, DisgustSims);
                EnterStateMachine("CoughingFit", "Enter", "x");
                AnimateSim("Exit");
                reactionBroadcaster.Dispose();
                StandardExit();
                return true;
            }
        }

        public const ulong kBuffKarmicSicknessGuid = 0x89A067CE37714BDC;

        public BuffKarmicSickness(BuffData data) : base(data)
        {
        }

        public override BuffInstance CreateBuffInstance() => new BuffInstanceKarmicSickness(this, BuffGuid, EffectValue, TimeoutSimMinutes);

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            base.OnAddition(bm, bi, travelReaddition);
            BuffInstanceKarmicSickness karmicSickness = bi as BuffInstanceKarmicSickness;
            bm.Actor.RemoveAlarm(karmicSickness.mStageAdvanceAlarmHandle);
            karmicSickness.mStageAdvanceAlarmHandle = AlarmHandle.kInvalidHandle;
            if (GameUtils.IsInstalled(ProductVersion.EP7))
            {
                karmicSickness.AdvancePlagueStage();
            }
            karmicSickness.mEffect = VisualEffect.Create("ep7BuffSickandTired_main");
            karmicSickness.mEffect.ParentTo(bm.Actor, Sim.FXJoints.Pelvis);
            karmicSickness.mEffect.Start();
            karmicSickness.mDisgustBroadcaster = new(bm.Actor, BuffSickAndTired.DisgustSimsBroadcasterParams, DisgustSims);
            InteractionInstance interactionInstance = bm.Actor.Autonomy.FindBestActionForCommodityOnLot(CommodityKind.RelieveNausea, bm.Actor.LotCurrent, AutonomySearchType.BuffAutoSolve);
            if (interactionInstance is not null)
            {
                interactionInstance.CancellableByPlayer = false;
                interactionInstance.SetPriority(InteractionPriorityLevel.High);
                bm.Actor.InteractionQueue.AddNext(interactionInstance);
            }
            else
            {
                bm.Actor.InteractionQueue.AddNext(BuffNauseous.ThrowUpOutside.Singleton.CreateInstance(bm.Actor, bm.Actor, new(InteractionPriorityLevel.High), false, false));
            }
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            if (bi is BuffInstanceKarmicSickness karmicSickness && karmicSickness.mStageAdvanceAlarmHandle != AlarmHandle.kInvalidHandle)
            {
                bm.Actor.RemoveAlarm(karmicSickness.mStageAdvanceAlarmHandle);
                karmicSickness.mStageAdvanceAlarmHandle = AlarmHandle.kInvalidHandle;
            }
            bi.Dispose(bm);
            if (bm.GetElement(BuffNames.CommodityDecayModifier)?.mBuffName == "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DrainedBuff")
            {
                bm.RemoveElement(BuffNames.CommodityDecayModifier);
            }
        }

        public static void AddKarmicSickness(Sim sim)
        {
            if ((WonderPowers.IsKidsMagicInstalled ? sim.SimDescription.ChildOrAbove : sim.SimDescription.TeenOrAbove) && !sim.IsRobot)
            {
                sim.BuffManager.AddElement(kBuffKarmicSicknessGuid, (Origin)HashString64("FromWonderPower"));
                sim.BuffManager.AddBuff(BuffNames.CommodityDecayModifier, 0, 1440, false, MoodAxis.Uncomfortable, (Origin)HashString64("FromWonderPower"), true);
                BuffInstanceCommodityDecayModifier buff = sim.BuffManager.GetElement(BuffNames.CommodityDecayModifier) as BuffInstanceCommodityDecayModifier;
                foreach (CommodityKind motive in (Responder.Instance.HudModel as HudModel).GetMotives(sim))
                {
                    buff.AddCommodityMultiplier(motive, TunableSettings.kSicknessMotiveDecay);
                }
                buff.mBuffName = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DrainedBuff";
                buff.mDescription = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DrainedBuffDescription";
                // This will automatically trigger the BuffsChanged event, so the UI should refresh itself after this and we won't have to do it manually
                buff.SetThumbnail("moodlet_Gamefreak130_DrainedBuff", ProductVersion.BaseGame, sim);
            }
        }

        private static void DisgustSims(Sim sim, ReactionBroadcaster broadcaster)
        {
            if (RandomUtil.RandomChance(BuffSickAndTired.ChanceForDisgustedBuff))
            {
                sim.BuffManager.AddElement(BuffNames.Disgusted, (Origin)HashString64("FromWonderPower"));
            }
            if (RandomUtil.RandomChance(BuffSickAndTired.ChanceForNauseousBuff))
            {
                sim.BuffManager.AddElement(BuffNames.Nauseous, (Origin)HashString64("FromWonderPower"));
            }
            if (RandomUtil.RandomChance(BuffSickAndTired.ChanceForSickAndTiredBuff))
            {
                AddKarmicSickness(sim);
            }
            if (RandomUtil.RandomChance(BuffSickAndTired.ChanceForFlee))
            {
                sim.PlayReaction(ReactionTypes.Repel, new(InteractionPriorityLevel.UserDirected), broadcaster.BroadcastingObject as GameObject, ReactionSpeed.AfterInteraction);
                Sim.MakeSimGoHome(sim, false);
            }
        }
    }

    public class BuffStrokeOfGenius : BuffTemporaryTraitEx
    {
        public const ulong kBuffStrokeOfGeniusGuid = 0x5013C6DC21516CD8;

        public BuffStrokeOfGenius(BuffData data) : base(data)
        {
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            if (bi is BuffInstanceTemporaryTraitEx strokeOfGenius)
            {
                strokeOfGenius.AddTemporaryTrait(bm.Actor.IsPet ? TraitNames.GeniusPet : TraitNames.Genius);
                strokeOfGenius.AddTemporaryTrait(bm.Actor.IsPet ? TraitNames.SuperSmartPet : TraitNames.FastLearner, true);
            }
        }
    }

    public class BuffSuperLucky : Buff
    {
        public const ulong kBuffSuperLuckyGuid = 0xDCF517CDF276DA33;

        public class BuffInstanceSuperLucky : BuffInstance
        {
            public VisualEffect mEffect;

            public BuffInstanceSuperLucky()
            {
            }

            public BuffInstanceSuperLucky(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount) : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            public override BuffInstance Clone() => new BuffInstanceSuperLucky(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);

            public override void Dispose(BuffManager bm)
            {
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                base.Dispose(bm);
            }
        }

        public BuffSuperLucky(BuffData data) : base(data)
        {
        }

        public override BuffInstance CreateBuffInstance() => new BuffInstanceSuperLucky(this, BuffGuid, EffectValue, TimeoutSimMinutes);

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceSuperLucky superLucky = bi as BuffInstanceSuperLucky;
            superLucky.mEffect = VisualEffect.Create("ep1EyeCandy");
            superLucky.mEffect.ParentTo(bm.Actor, Sim.FXJoints.Spine1);
            superLucky.mEffect.Start();
            base.OnAddition(bm, bi, travelReaddition);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi) => bi.Dispose(bm);
    }
}