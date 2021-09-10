using Gamefreak130.Common.Buffs;
using Gamefreak130.Common.Interactions;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.InteractionsShared;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
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

        private const int kMaxInteractions = 2;

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
                string[] validActionKeys = actor.IsPet ? TunableSettings.kCryHavocPetInteractions : TunableSettings.kCryHavocSimInteractions;
                if (actor.InteractionQueue.Count < kMaxInteractions)
                {
                    Sim target = RandomUtil.GetRandomObjectFromList(actor.LotCurrent.GetAllActors());
                    if (CanFight(actor, target))
                    {
                        string social = RandomUtil.GetRandomStringFromList(actor.IsPet ? TunableSettings.kCryHavocPetInteractions : TunableSettings.kCryHavocSimInteractions);
                        InteractionHelper.ForceSocialInteraction(actor, target, social, InteractionPriorityLevel.CriticalNPCBehavior, false);
                    }
                }
                actor.AddAlarm(1f, TimeUnit.Minutes, delegate { CruiseForBruise(actor); }, "Gamefreak130 wuz here -- Cry Havoc alarm", AlarmType.DeleteOnReset);
            }
        }

        private static bool CanFight(Sim x, Sim y)
            => y is not null && y.InteractionQueue.Count < kMaxInteractions && x != y 
            && x.BuffManager.HasElement(kBuffCryHavocGuid) && y.BuffManager.HasElement(kBuffCryHavocGuid)
            && x.IsPet == y.IsPet && ((x.SimDescription.Teen && y.SimDescription.Teen) || (x.SimDescription.YoungAdultOrAbove && y.SimDescription.YoungAdultOrAbove));
    }

    public class BuffKarmicPossession : Buff
    {
        public const ulong kBuffKarmicPossessionGuid = 0xF23046B315CBFB49;

        private const float kStaggerOffset = 3;

        private const float kAlarmTime = 5;

        private class BuffInstanceKarmicPossession : BuffInstance
        {
            private const int kMaxInteractions = 2;

            private VisualEffect mEffect;

            private AlarmHandle mAlarm;

            private ulong mActorId;

            private int mLastBehavior;

            public BuffInstanceKarmicPossession()
            {
            }

            public BuffInstanceKarmicPossession(Buff buff, BuffNames buffGuid, int effectValue, float timeoutSimMinutes) : base(buff, buffGuid, effectValue, timeoutSimMinutes)
            {
            }

            public override BuffInstance Clone() => new BuffInstanceKarmicPossession(mBuff, Guid, EffectValue, TimeoutCount);

            public override void Dispose(BuffManager bm)
            {
                if (mAlarm != AlarmHandle.kInvalidHandle)
                {
                    bm.Actor.RemoveAlarm(mAlarm);
                    mAlarm = AlarmHandle.kInvalidHandle;
                }
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                base.Dispose(bm);
            }

            public void StartVisualEffect(BuffManager bm)
            {
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                mEffect = VisualEffect.Create("ep6geniemakewishnegative_main");
                mEffect.ParentTo(bm.Actor, Sim.FXJoints.Head);
                mEffect.Start();
            }

            public void StartAlarm(BuffManager bm)
            {
                mActorId = bm.Actor.SimDescription.SimDescriptionId;
                StartAlarm(bm.Actor, sStaggerTime);
            }

            public void StartAlarm(Sim sim, float time)
            {
                if (mAlarm != AlarmHandle.kInvalidHandle)
                {
                    sim.RemoveAlarm(mAlarm);
                    mAlarm = AlarmHandle.kInvalidHandle;
                }
                mAlarm = sim.AddAlarm(time, TimeUnit.Minutes, TriggerBadBehavior, "Gamefreak130 wuz here -- Feral Possession bad behavior alarm", AlarmType.DeleteOnReset);
            }

            private int BehaviorCount() 
                => SimDescription.Find(mActorId)?.CreatedSim switch
            {
                { IsKitten: true }       => 3,
                { IsCat: true }          => 5,
                { IsPuppy: true }        => 3,
                { IsADogSpecies: true }  => 4,
                _                        => -1
            };

            private void TriggerBadBehavior()
            {
                float time = 1f;
                Sim actor = SimDescription.Find(mActorId)?.CreatedSim;
                int behaviorCount = BehaviorCount();
                if (actor is not null && actor.InteractionQueue.Count < kMaxInteractions && behaviorCount != -1)
                {
                    InteractionInstance interactionInstance = null;
                    bool flag = false;
                    mLastBehavior = (mLastBehavior + RandomUtil.GetInt(1, behaviorCount - 1)) % behaviorCount;
                    switch (mLastBehavior)
                    {
                        case 1:
                        {
                            List<GameObject> scratchableObjects = actor.LotCurrent.GetObjects<GameObject>(obj => obj.Repairable is ScratchableRepairable { Broken: false });
                            GameObject gameObject = RandomUtil.GetRandomObjectFromList(scratchableObjects);
                            if (gameObject is not null)
                            {
                                interactionInstance = ScratchObject.Singleton.CreateInstance(gameObject, actor, new(InteractionPriorityLevel.RequiredNPCBehavior), false, false);
                            }
                            break;
                        }
                        case 2:
                        {
                            Sim sim = RandomUtil.GetRandomObjectFromList(actor.LotCurrent.GetSims(sim => sim.SimDescription.ChildOrAbove));
                            if (sim is not null)
                            {
                                InteractionHelper.ForceSocialInteraction(actor, sim, actor.IsCat ? "Cat Hiss" : "Growl At", InteractionPriorityLevel.CriticalNPCBehavior, false);
                                flag = true;
                            }
                            break;
                        }
                        case 3:
                        {
                            Sim sim = RandomUtil.GetRandomObjectFromList(actor.LotCurrent.GetAnimalsOfType(CASAGSAvailabilityFlags.CatAdult | CASAGSAvailabilityFlags.CatElder | CASAGSAvailabilityFlags.DogAdult | CASAGSAvailabilityFlags.DogElder | 
                                                                                                           CASAGSAvailabilityFlags.LittleDogAdult | CASAGSAvailabilityFlags.LittleDogElder | CASAGSAvailabilityFlags.Raccoon));

                            if (sim is not null)
                            {
                                string actionKey = actor.SimDescription.Child
                                    ? actor.IsCat 
                                        ? "Cat Hiss" 
                                        : "Growl At"
                                    : "Fight Pet";

                                InteractionHelper.ForceSocialInteraction(actor, sim, actionKey, InteractionPriorityLevel.CriticalNPCBehavior, false);
                                flag = true;
                            }
                            break;
                        }
                        case 4:
                        {
                            Sim sim = RandomUtil.GetRandomObjectFromList(actor.LotCurrent.GetAnimals().ToList());
                            if (sim is not null)
                            {
                                string actionKey = sim.IsCat && sim.SimDescription.AdultOrAbove ? "Pounce Mean" : "Cat Hiss";
                                InteractionHelper.ForceSocialInteraction(actor, sim, actionKey, InteractionPriorityLevel.CriticalNPCBehavior, false);
                                flag = true;
                            }
                            break;
                        }
                        default:
                            InteractionDefinition definition = actor.IsADogSpecies ? Sim.DogPee.PeeIndoorSingleton : Sim.CatPee.PeeIndoorSingleton;
                            interactionInstance = definition.CreateInstance(actor, actor, new(InteractionPriorityLevel.RequiredNPCBehavior), false, false);
                            break;
                    }
                    if (flag || (interactionInstance != null && actor.InteractionQueue.AddNext(interactionInstance)))
                    {
                        time = kAlarmTime;
                    }
                }
                StartAlarm(actor, time);
            }
        }

        private static float sStaggerTime = 1;

        public BuffKarmicPossession(BuffData data) : base(data)
        {
        }

        public override BuffInstance CreateBuffInstance() => new BuffInstanceKarmicPossession(this, BuffGuid, EffectValue, TimeoutSimMinutes);

        public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue) => base.ShouldAdd(bm, axisEffected, moodValue) && bm.Actor.IsPet;

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            base.OnAddition(bm, bi, travelReaddition);
            bm.Actor.SimDescription.IsNeverSelectable = true;
            if (PlumbBob.SelectedActor == bm.Actor)
            {
                LotManager.SelectNextSim();
            }
            bm.Actor.Motives.SetValue(CommodityKind.Bladder, -40);
            BuffInstanceKarmicPossession karmicPossession = bi as BuffInstanceKarmicPossession;
            karmicPossession.StartVisualEffect(bm);
            karmicPossession.StartAlarm(bm);
            sStaggerTime = (sStaggerTime + kStaggerOffset) % kAlarmTime;
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            bi.Dispose(bm);
            // Pets should never be roommates, but I'm including the sanity check here just in case
            if (bm.Actor.Household is not null && !Household.RoommateManager.IsNPCRoommate(bm.Actor))
            {
                bm.Actor.SimDescription.IsNeverSelectable = false;
            }
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
                public static string LocalizeString(string name, params object[] parameters) => Localization.LocalizeString(kLocalizationKey + name, parameters);

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop) => LocalizeString("CoughingFit", new object[0]);

                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => a == target && isAutonomous;
            }

            public const string kLocalizationKey = "Gameplay/ActorSystems/BuffPestilencePlague/CoughingFit:";

            public static ISoloInteractionDefinition Singleton = new Definition();

            public static string LocalizeString(string name, params object[] parameters) => Localization.LocalizeString(kLocalizationKey + name, parameters);

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
            private VisualEffect mEffect;

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

            public void StartVisualEffect(BuffManager bm)
            {
                if (mEffect is not null)
                {
                    mEffect.Stop();
                    mEffect.Dispose();
                    mEffect = null;
                }
                mEffect = VisualEffect.Create("ep1EyeCandy");
                mEffect.ParentTo(bm.Actor, Sim.FXJoints.Spine1);
                mEffect.Start();
            }
        }

        public BuffSuperLucky(BuffData data) : base(data)
        {
        }

        public override BuffInstance CreateBuffInstance() => new BuffInstanceSuperLucky(this, BuffGuid, EffectValue, TimeoutSimMinutes);

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceSuperLucky superLucky = bi as BuffInstanceSuperLucky;
            superLucky.StartVisualEffect(bm);
            base.OnAddition(bm, bi, travelReaddition);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi) => bi.Dispose(bm);
    }

    public class BuffTransmogrify : Buff
    {
        public enum TransmogType : byte
        {
            ToHuman,
            ToDog,
            ToCat,
            ToHorse
        }

        public class BuffInstanceTransmogrify : BuffInstance
        {
            private TransmogType mTransmogType;

            public BuffInstanceTransmogrify()
            {
            }

            public BuffInstanceTransmogrify(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount) : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            public override BuffInstance Clone() => new BuffInstanceTransmogrify(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);

            public void SetTransmogType(TransmogType type, Sim sim)
            {
                if (mTransmogType != type)
                {
                    mTransmogType = type;
                    mBuffName = $"Gameplay/Excel/Buffs/BuffList:Gamefreak130_Transmogged{mTransmogType}";
                    mDescription = $"Gameplay/Excel/Buffs/BuffList:Gamefreak130_Transmogged{mTransmogType}Description";
                    string thumbName = mTransmogType switch
                    {
                        TransmogType.ToHuman  => "moodlet_Gamefreak130_TransmoggedToHuman",
                        TransmogType.ToDog    => "moodlet_dogplaytime",
                        TransmogType.ToCat    => "moodlet_catplaytime",
                        TransmogType.ToHorse  => "moodlet_horsieplaytime",
                        _                     => ""
                    };
                    // This will automatically trigger the BuffsChanged event, so the UI should refresh itself after this and we won't have to do it manually
                    SetThumbnail(thumbName, ProductVersion.EP5, sim);
                } 
            }
        }

        public const ulong kBuffTransmogrifyGuid = 0x90471A06B5F5F359;

        public BuffTransmogrify(BuffData data) : base(data)
        {
        }

        public override BuffInstance CreateBuffInstance() => new BuffInstanceTransmogrify(this, BuffGuid, EffectValue, TimeoutSimMinutes);
    }
}