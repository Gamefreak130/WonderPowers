using Gamefreak130.Common.Helpers;
using Gamefreak130.Common.Situations;
using Gamefreak130.WonderPowersSpace.Helpers;
using Gamefreak130.WonderPowersSpace.Interactions;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Controllers;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using static Sims3.Gameplay.GlobalFunctions;
using static Sims3.SimIFace.ResourceUtils;
using Queries = Sims3.Gameplay.Queries;

namespace Gamefreak130.WonderPowersSpace.Situations
{
    public abstract class KarmaSituationBase : CommonRootSituation
    {
        private readonly List<GameObject> mFogEmitters = new();

        protected readonly List<ObjectGuid> mSimulatorObjects = new();

        protected readonly List<Sim> mParticipants = new();

        protected AlarmHandle mExitHandle;

        protected uint mSoundHandle;

        private bool mInitialized;

        public KarmaSituationBase()
        {
        }

        public KarmaSituationBase(Lot lot) : base(lot)
        {
        }

        protected override void Init()
        {
            try
            {
                mFogEmitters.AddRange(HelperMethods.CreateFogEmittersOnLot(Lot));
                mInitialized = true;
            }
            catch
            {
                foreach (ObjectGuid guid in mSimulatorObjects)
                {
                    Simulator.DestroyObject(guid);
                }
                throw;
            }
        }

        public override void CleanUp()
        {
            try
            {
                foreach (Sim sim in mParticipants.OfType<Sim>())
                {
                    sim.RemoveRole(this);
                }
                foreach (GameObject emitter in mFogEmitters.OfType<GameObject>())
                {
                    emitter.Destroy();
                    emitter.Dispose();
                }
                if (mSoundHandle != 0U)
                {
                    Audio.StopSound(mSoundHandle);
                    mSoundHandle = 0U;
                }
                AlarmManager.RemoveAlarm(mExitHandle);
                mExitHandle = AlarmHandle.kInvalidHandle;
                base.CleanUp();
            }
            finally
            {
                if (mInitialized)
                {
                    WonderPowerManager.TogglePowerRunning();
                }
            }
        }

        public override void OnReset(Sim sim)
        {
            mParticipants.Remove(sim);
            sim.RemoveRole(this);
            if (mParticipants.Count == 0)
            {
                Exit();
            }
        }

        public override void OnParticipantDeleted(Sim participant)
        {
            mParticipants.Remove(participant);
            participant.RemoveRole(this);
            if (mParticipants.Count == 0)
            {
                Exit();
            }
        }
    }

    public class CryHavocSituation : KarmaSituationBase
    {
        public CryHavocSituation()
        {
        }

        public CryHavocSituation(Lot lot) : base(lot)
        {
        }

        protected override void Init()
        {
            SetState(new StartSituation(this));
            base.Init();
        }

        private class StartSituation : ChildSituation<CryHavocSituation>
        {
            public StartSituation()
            {
            }

            public StartSituation(CryHavocSituation parent) : base(parent)
            {
            }

            public override void Init(CryHavocSituation parent)
            {
                // CONSIDER reaction broadcast?

                // This sting is handled separately from the WonderPowerManager
                // So that we can stop it once the situation is finished, even if there is no backlash
                Parent.mSoundHandle = Audio.StartSound("sting_cryhavoc", Lot.Position);
                Parent.mExitHandle = AlarmManager.AddAlarm(TunableSettings.kCryHavocLength, TimeUnit.Minutes, Parent.Exit, "Gamefreak130 wuz here -- CryHavoc Situation Alarm", AlarmType.AlwaysPersisted, null);
                Camera.FocusOnLot(Lot.LotId, 2f); //2f is standard lerpTime
                Parent.mParticipants.AddRange(Lot.GetAllActors().FindAll(IsValidFighter));

                if (Parent.mParticipants.Count < TunableSettings.kCryHavocMinSims)
                {
                    List<Sim> otherSims = Queries.GetObjects<Sim>()
                                                 .Where(sim => !Parent.mParticipants.Contains(sim) && IsValidFighter(sim))
                                                 .ToList();
                    while (Parent.mParticipants.Count < TunableSettings.kCryHavocMinSims && otherSims.Count != 0)
                    {
                        Sim closestSim = GetClosestObject(otherSims, Lot);
                        if (closestSim is not null)
                        {
                            Parent.mParticipants.Add(closestSim);
                            otherSims.Remove(closestSim);
                        }
                        else
                        {
                            otherSims.RemoveAt(0);
                        }
                    }
                }
                PruneFighters();
                foreach (Sim sim in Parent.mParticipants.OfType<Sim>())
                {
                    sim.AssignRole(Parent);
                    GoToLotAndFight visitLot = new GoToLotAndFight.Definition().CreateInstance(Lot, sim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as GoToLotAndFight;
                    ForceSituationSpecificInteraction(sim, visitLot);
                }
                PlumbBob.SelectedActor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(PlumbBob.SelectedActor.IsFemale, "CryHavocTNS", PlumbBob.SelectedActor), StyledNotification.NotificationStyle.kGameMessageNegative);
            }

            private bool IsValidFighter(Sim sim) => sim is { IsHorse: false, CanBeSocializedWith: true, SimDescription: { TeenOrAbove: true } };

            // Two Sims can fight each other only if they are both pets, both teens, or both adults or older
            // This method ensures that there are an even number of fighters in each of these three groups
            // So that any Sim always has someone to fight with
            private void PruneFighters()
            {
                Predicate<Sim>[] predicates = {
                    (sim) => sim.IsPet,
                    (sim) => sim.SimDescription.Teen,
                    (sim) => sim.IsHuman && sim.SimDescription.YoungAdultOrAbove
                };
                
                foreach (Predicate<Sim> predicate in predicates)
                {
                    int predicateCount = Parent.mParticipants.FindAll(predicate).Count;
                    if (predicateCount > 0 && predicateCount % 2 != 0)
                    {
                        Parent.mParticipants.RemoveAt(Parent.mParticipants.FindIndex(predicate));
                    }
                }
            }
        }

        public override void CleanUp() 
        {
            try
            {
                foreach (Sim sim in mParticipants.OfType<Sim>())
                {
                    sim.InteractionQueue.CancelAllInteractions();
                    sim.OverlayComponent.PlayReaction(ReactionTypes.MotiveFailEnergy, sim, false);
                    sim.BuffManager.RemoveElement(Buffs.BuffCryHavoc.kBuffCryHavocGuid);
                    if (!sim.IsAtHome)
                    {
                        Sim.MakeSimGoHome(sim, false, new(InteractionPriorityLevel.CriticalNPCBehavior));
                    }
                }
            }
            finally
            {
                base.CleanUp();
            }
        }

        public override void OnReset(Sim sim)
        {
            sim.BuffManager.RemoveElement(Buffs.BuffCryHavoc.kBuffCryHavocGuid);
            base.OnReset(sim);
        }
    }

    public class FeralPossessionSituation : KarmaSituationBase
    {
        public FeralPossessionSituation()
        {
        }

        public FeralPossessionSituation(Lot lot) : base(lot)
        {
        }

        protected override void Init()
        {
            SetState(new StartSituation(this));
            base.Init();
        }

        private List<Sim> mCreatedPets;

        private class StartSituation : ChildSituation<FeralPossessionSituation>
        {
            public StartSituation()
            {
            }

            public StartSituation(FeralPossessionSituation parent) : base(parent)
            {
            }

            public override void Init(FeralPossessionSituation parent)
            {
                // CONSIDER reaction broadcast?

                // This sting is handled separately from the WonderPowerManager
                // So that we can stop it once the situation is finished, even if there is no backlash

                Parent.mSoundHandle = Audio.StartSound("sting_feralpossession", Lot.Position);
                Parent.mExitHandle = AlarmManager.AddAlarm(TunableSettings.kFeralPossessionLength, TimeUnit.Minutes, Parent.Exit, "Gamefreak130 wuz here -- FeralPossession Situation Alarm", AlarmType.AlwaysPersisted, null);
                Camera.FocusOnLot(Lot.LotId, 2f); //2f is standard lerpTime
                Parent.mParticipants.AddRange(Lot.GetAnimalsOfType(CASAGSAvailabilityFlags.CatChild | CASAGSAvailabilityFlags.CatAdult | CASAGSAvailabilityFlags.CatElder | CASAGSAvailabilityFlags.DogChild | CASAGSAvailabilityFlags.DogAdult | CASAGSAvailabilityFlags.DogElder));
                Parent.mCreatedPets = new();

                if (Parent.mParticipants.Count < TunableSettings.kFeralPossessionMinPets)
                {
                    List<Sim> otherPets = Queries.GetObjects<Sim>()
                                                 .Where(sim => !Parent.mParticipants.Contains(sim) && (sim.IsCat || sim.IsADogSpecies))
                                                 .ToList();

                    while (Parent.mParticipants.Count < TunableSettings.kFeralPossessionMinPets && otherPets.Count != 0)
                    {
                        Sim closestSim = GetClosestObject(otherPets, Lot);
                        if (closestSim is not null)
                        {
                            Parent.mParticipants.Add(closestSim);
                            otherPets.Remove(closestSim);
                        }
                        else
                        {
                            otherPets.RemoveAt(0);
                        }
                    }

                    for (int i = 0; i < TunableSettings.kFeralPossessionMinPets - Parent.mParticipants.Count; i++)
                    {
                        Parent.mSimulatorObjects.Add(Simulator.AddObject(new OneShotFunction(CreatePossessedPet)));
                    }
                }

                foreach (Sim pet in Parent.mParticipants.OfType<Sim>())
                {
                    pet.AssignRole(Parent);
                    pet.GreetSimOnLot(Lot);
                    if (pet.LotCurrent != Lot)
                    {
                        ForceSituationSpecificInteraction(pet, pet.CreateTeleportInstanceToPositionOnLot(Lot, null, null));
                    }
                    pet.BuffManager.AddElement(Buffs.BuffKarmicPossession.kBuffKarmicPossessionGuid, (Origin)HashString64("FromWonderPower"));
                }
                PlumbBob.SelectedActor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(PlumbBob.SelectedActor.IsFemale, "FeralPossessionTNS", PlumbBob.SelectedActor), StyledNotification.NotificationStyle.kGameMessageNegative);
            }

            private void CreatePossessedPet()
            {
                SimDescription createdPet = GeneticsPet.MakeRandomPet(CASAgeGenderFlags.Adult, RandomUtilEx.CoinFlipSelect(CASAgeGenderFlags.Male, CASAgeGenderFlags.Female), RandomUtilEx.CoinFlipSelect(CASAgeGenderFlags.Dog, CASAgeGenderFlags.Cat));
                GeneticsPet.AssignRandomTraits(createdPet);
                Household.PetHousehold.AddSilent(createdPet);
                createdPet.OnHouseholdChanged(Household.PetHousehold, false);
                Sim instantiatedPet = createdPet.Instantiate(Service.GetPositionNearLotCorners(Lot));

                VisualEffect vfx = VisualEffect.Create(instantiatedPet.IsFullSizeDog ? "ep5TeleportDog" : "ep5TeleportSmall");
                vfx.SetPosAndOrient(instantiatedPet.Position, instantiatedPet.ForwardVector, instantiatedPet.UpVector);
                vfx.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);

                instantiatedPet.AssignRole(Parent);
                instantiatedPet.GreetSimOnLot(Lot);
                Parent.mCreatedPets.Add(instantiatedPet);
                Parent.mParticipants.Add(instantiatedPet);
                instantiatedPet.BuffManager.AddElement(Buffs.BuffKarmicPossession.kBuffKarmicPossessionGuid, (Origin)HashString64("FromWonderPower"));
            }
        }

        public override void CleanUp()
        {
            try
            {
                List<Sim> createdPets = mCreatedPets.OfType<Sim>().ToList();
                List<Sim> possessed = mParticipants.OfType<Sim>().ToList();
                mCreatedPets.Clear();
                mParticipants.Clear();

                foreach (Sim pet in createdPets)
                {
                    pet.BuffManager.RemoveElement(Buffs.BuffKarmicPossession.kBuffKarmicPossessionGuid);
                    VisualEffect visualEffect = VisualEffect.Create("ep6GenieTargetSimDisappear_main");
                    visualEffect.ParentTo(pet, Sim.FXJoints.Spine0);
                    visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
                    pet.FadeOut(false, true);
                    pet.SimDescription.Dispose();
                }

                foreach (Sim pet in possessed)
                {
                    pet.RemoveRole(this);
                    pet.InteractionQueue.CancelAllInteractions();
                    pet.RemoveSimGreetedOnLot(Lot);
                    pet.BuffManager.RemoveElement(Buffs.BuffKarmicPossession.kBuffKarmicPossessionGuid);
                    if (!pet.IsAtHome)
                    {
                        Sim.MakeSimGoHome(pet, false, new(InteractionPriorityLevel.CriticalNPCBehavior));
                    }
                }
            }
            finally
            {
                base.CleanUp();
            }
        }

        public override void OnReset(Sim sim)
        {
            if (mCreatedPets.Contains(sim))
            {
                sim.BuffManager.RemoveElement(Buffs.BuffKarmicPossession.kBuffKarmicPossessionGuid);
                sim.FadeOut(false, true);
                sim.SimDescription.Dispose();
            }
            else if (mParticipants.Contains(sim))
            {
                sim.BuffManager.RemoveElement(Buffs.BuffKarmicPossession.kBuffKarmicPossessionGuid);
                base.OnReset(sim);
            }
        }

        public override void OnParticipantDeleted(Sim participant)
        {
            participant.RemoveRole(this);
            mCreatedPets.Remove(participant);
            if (mParticipants.Contains(participant))
            {
                base.OnParticipantDeleted(participant);
            }
        }
    }

    public class FireSituation : KarmaSituationBase
    {
        public FireSituation()
        {
        }

        public FireSituation(Lot lot) : base(lot)
        {
        }

        protected override void Init()
        {
            SetState(new StartSituation(this));
            base.Init();
        }

        private class StartSituation : ChildSituation<FireSituation>
        {
            public StartSituation()
            {
            }

            public StartSituation(FireSituation parent) : base(parent)
            {
            }

            public override void Init(FireSituation parent)
            {
                WonderPowerManager.PlayPowerSting("sting_firestorm");
                Lot.AddAlarm(30f, TimeUnit.Seconds, () => Camera.FocusOnLot(Lot.LotId, 2f), "Gamefreak130 wuz here -- Activation focus alarm", AlarmType.NeverPersisted); //2f is standard lerptime

                // For each fire spawned, there is a 25% chance it will ignite a burnable object,
                // A 25% chance it will ignite a valid sim on the lot,
                // And a 50% chance it will spawn directly on the ground
                List<GameObject> burnableObjects = Lot.GetObjects<GameObject>(@object => @object is not Sim and not PlumbBob && @object.GetFireType() is not FireType.DoesNotBurn && !@object.Charred);
                List<Sim> burnableSims = Lot.GetSims(sim => sim.IsHuman && sim.SimDescription.ChildOrAbove);
                int numFires = RandomUtil.GetInt(TunableSettings.kFireMin, TunableSettings.kFireMax);
                for (int i = 0; i < numFires; i++)
                {
                    if (RandomUtil.CoinFlip())
                    {
                        if (RandomUtil.CoinFlip() && burnableObjects.Count != 0)
                        {
                            GameObject @object = RandomUtil.GetRandomObjectFromList(burnableObjects);
                            FireManager.AddFire(@object.PositionOnFloor, true);
                            AlarmManager.AddAlarm(30f, TimeUnit.Seconds, delegate {
                                VisualEffect effect = VisualEffect.Create("ep2DetonateMedium");
                                effect.SetPosAndOrient(@object.Position, @object.ForwardVector, @object.UpVector);
                                effect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
                            }, "Gamefreak130 wuz here -- visual effect alarm", AlarmType.NeverPersisted, null);
                            burnableObjects.Remove(@object);
                        }
                        else if (burnableSims.Count != 0)
                        {
                            Sim sim = RandomUtil.GetRandomObjectFromList(burnableSims);
                            sim.BuffManager.AddElement(BuffNames.OnFire, (Origin)HashString64("FromWonderPower"));
                            burnableSims.Remove(sim);
                        }
                        continue;
                    }
                    Vector3 pos = Lot.GetRandomPosition(true, true);
                    FireManager.AddFire(pos, true);
                }
                StyledNotification.Show(new(WonderPowerManager.LocalizeString("FireTNS"), StyledNotification.NotificationStyle.kGameMessageNegative));
                Parent.CheckForExit();
            }
        }

        public override void OnReset(Sim _)
        {
        }

        public override void OnParticipantDeleted(Sim _)
        {
        }

        private void CheckForExit()
        {
            if (Lot is null || ((Lot.FireManager is null or { NoFire: true }) && Lot.GetSims(sim => FirefighterSituation.IsSimOnFire(sim)).Count == 0))
            {
                Exit();
            }
            else
            {
                mExitHandle = AlarmManager.AddAlarm(1f, TimeUnit.Minutes, CheckForExit, "Gamefreak130 wuz here -- Fire situation alarm", AlarmType.AlwaysPersisted, null);
            }
        }
    }

    public class GhostsSituation : KarmaSituationBase
    {
        public GhostsSituation()
        {
        }

        public GhostsSituation(Lot lot) : base(lot)
        {
        }

        protected override void Init()
        {
            SetState(new StartSituation(this));
            base.Init();
        }

        private readonly List<GameObject> mUrnstones = new();

        private ReactionBroadcaster mPanicBroadcaster;

        private readonly Dictionary<LightGameObject, ColorInfo> mPrevColorInfo = new();

        private struct ColorInfo
        {
            public LightGameObject.LightColor Color;

            public Vector3 CustomColorVec;

            public ColorInfo(LightGameObject.LightColor color, Vector3 colorVec)
            {
                Color = color;
                CustomColorVec = colorVec;
            }
        }

        private class StartSituation : ChildSituation<GhostsSituation>
        {
            public StartSituation()
            {
            }

            public StartSituation(GhostsSituation parent) : base(parent)
            {
            }

            private static readonly SimDescription.DeathType[] sValidDeathTypes =
            {
                SimDescription.DeathType.OldAge,
                SimDescription.DeathType.Drown,
                SimDescription.DeathType.Starve,
                SimDescription.DeathType.Electrocution,
                SimDescription.DeathType.Burn,
                SimDescription.DeathType.MummyCurse,
                SimDescription.DeathType.Meteor,
                SimDescription.DeathType.Thirst,
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
                SimDescription.DeathType.Jetpack
            };

            public override void Init(GhostsSituation parent)
            {
                // This sting is handled separately from the WonderPowerManager
                // So that we can stop it once the situation is finished, even if there is no backlash
                Parent.mSoundHandle = Audio.StartSound("sting_ghosts", Lot.Position);
                Parent.mExitHandle = AlarmManager.AddAlarm(TunableSettings.kGhostInvasionLength, TimeUnit.Minutes, Parent.Exit, "Gamefreak130 wuz here -- GhostInvasion Situation Alarm", AlarmType.AlwaysPersisted, null);
                Lot.AddAlarm(30f, TimeUnit.Seconds, () => Camera.FocusOnLot(Lot.LotId, 2f), "Gamefreak130 wuz here -- Activation focus alarm", AlarmType.NeverPersisted);
                foreach (Sim sim in Lot.GetSims(sim => sim.IsSleeping))
                {
                    sim.InteractionQueue.CancelAllInteractions();
                }

                int @int = RandomUtil.GetInt(GhostHunter.kSpiritLightingRed.Length - 1);
                float r = GhostHunter.kSpiritLightingRed[@int] / 255f;
                float g = GhostHunter.kSpiritLightingGreen[@int] / 255f;
                float b = GhostHunter.kSpiritLightingBlue[@int] / 255f;
                foreach (LightGameObject lightGameObject in Lot.GetObjects<LightGameObject>())
                {
                    Parent.mPrevColorInfo[lightGameObject] = new(lightGameObject.Color, new(lightGameObject.CustomColorRed, lightGameObject.CustomColorGreen, lightGameObject.CustomColorBlue));
                    lightGameObject.SwitchLight(true, false);
                    World.LightSetColor(lightGameObject.Proxy.ObjectId, r, g, b);
                    lightGameObject.DisableInteractions();
                }
                Parent.mPanicBroadcaster = new(Lot, GhostHunter.kReactionParametersGhostlyPresence, OnPanicStart);

                for (int i = 0; i < RandomUtil.GetInt(TunableSettings.kGhostsMin, TunableSettings.kGhostsMax); i++)
                {
                    Parent.mSimulatorObjects.Add(Simulator.AddObject(new OneShotFunction(CreateAngryGhost)));
                }
                StyledNotification.Show(new(WonderPowerManager.LocalizeString("GhostsTNS"), StyledNotification.NotificationStyle.kGameMessageNegative));
            }

            private void OnPanicStart(Sim actor, ReactionBroadcaster broadcaster)
            {
                if (GhostHunter.GhostHunterJob.ShouldPanic(actor, broadcaster.BroadcastingObject))
                {
                    InteractionInstance interactionInstance = ReactToGhost.Singleton.CreateInstance(actor, actor, new(InteractionPriorityLevel.Autonomous), false, true);
                    interactionInstance.Hidden = true;
                    actor.InteractionQueue.Add(interactionInstance);
                }
            }

            private void CreateAngryGhost()
            {
                IGameObject gameObject = CreateGhostJig();
                if (gameObject is null)
                {
                    return;
                }
                Vector3 position = gameObject.Position;
                gameObject.Destroy();
                WorldName randomObjectFromList = RandomUtil.GetRandomObjectFromList(GhostHunter.GhostHunterJob.sValidHomeWorlds);
                IUrnstone urnstone = CreateObjectOutOfWorld("UrnstoneHuman") as IUrnstone;
                SimDescription ghost;
                if (GameUtils.IsFutureWorld() && RandomUtil.CoinFlip())
                {
                    ghost = OccultRobot.MakeRobot(CASAgeGenderFlags.Adult, RandomUtilEx.CoinFlipSelect(CASAgeGenderFlags.Male, CASAgeGenderFlags.Female), RandomUtilEx.CoinFlipSelect(RobotForms.Hovering, RobotForms.Humanoid));
                    ghost.SetDeathStyle(SimDescription.DeathType.Robot, false);
                }
                else
                {
                    ghost = Genetics.MakeSim(RandomUtilEx.CoinFlipSelect(CASAgeGenderFlags.Adult, CASAgeGenderFlags.Elder), RandomUtilEx.CoinFlipSelect(CASAgeGenderFlags.Male, CASAgeGenderFlags.Female), randomObjectFromList, 4294967295u);
                    ghost.FirstName = SimUtils.GetRandomGivenName(ghost.IsMale, randomObjectFromList);
                    ghost.LastName = SimUtils.GetRandomFamilyName(randomObjectFromList);
                    ghost.SetDeathStyle(RandomUtil.GetRandomObjectFromList(sValidDeathTypes), false);
                    TraitNames trait = ghost.DeathStyle switch
                    {
                        SimDescription.DeathType.Drown          => TraitNames.Hydrophobic,
                        SimDescription.DeathType.Electrocution  => TraitNames.AntiTV,
                        SimDescription.DeathType.Burn           => TraitNames.Pyromaniac,
                        _                                       => TraitNames.Unknown
                    };
                    ghost.TraitManager.AddHiddenElement(trait);
                }
                List<TraitNames> list = new(GhostHunter.kAngryGhostTraits);
                while (!ghost.TraitManager.TraitsMaxed() && list.Count > 0)
                {
                    TraitNames randomObjectFromList2 = RandomUtil.GetRandomObjectFromList(list);
                    list.Remove(randomObjectFromList2);
                    if (ghost.TraitManager.CanAddTrait((ulong)randomObjectFromList2))
                    {
                        ghost.TraitManager.AddElement(randomObjectFromList2);
                    }
                }
                urnstone.SetDeadSimDescription(ghost);
                if (!urnstone.GhostSpawn(false, position, Lot, true))
                {
                    urnstone.Destroy();
                    return;
                }
                ghost.CreatedSim.Autonomy.AllowedToRunMetaAutonomy = false;
                ghost.CreatedSim.Autonomy.Motives.CreateMotive(CommodityKind.BeAngryGhost);
                ghost.CreatedSim.AddSoloInteraction(GhostHunter.AngryHaunt.Singleton);
                ActiveTopic.AddToSim(ghost.CreatedSim, "Angry Ghost");
                if (Lot.Household is not null)
                {
                    foreach (SimDescription current in Lot.Household.AllSimDescriptions)
                    {
                        Relationship relationship = ghost.GetRelationship(current, true);
                        relationship.LTR.UpdateLiking(GhostHunter.kAngryGhostRelationshipLevelWithHousehold);
                    }
                }
                if (ghost.IsHuman && RandomUtil.RandomChance01(GhostHunter.kAngryGhostAncientOutfitChance))
                {
                    string name = $"{OutfitUtils.GetAgePrefix(ghost.Age)}{OutfitUtils.GetGenderPrefix(ghost.Gender)}{RandomUtil.GetRandomStringFromList(GhostHunter.GhostHunterJob.sAncientCasOutfits)}";
                    SimOutfit uniform = new(ResourceKey.CreateOutfitKeyFromProductVersion(name, ProductVersion.EP2));
                    if (OutfitUtils.TryApplyUniformToOutfit(ghost.GetOutfit(OutfitCategories.Everyday, 0), uniform, ghost, "GhostsSituation.CreateAngryGhost", out SimOutfit outfit))
                    {
                        ghost.AddOutfit(outfit, OutfitCategories.Everyday, true);
                        ghost.CreatedSim.SwitchToOutfitWithoutSpin(OutfitCategories.Everyday);
                    }
                }
                Parent.mParticipants.Add(ghost.CreatedSim);
                Parent.mUrnstones.Add((GameObject)urnstone);
                return;
            }

            private IGameObject CreateGhostJig()
            {
                IGameObject gameObject = CreateObjectOutOfWorld("SocialJigOnePerson");
                World.FindGoodLocationParams @params;
                int randomRoomWeightedBySize = GetRandomRoomWeightedBySize();
                @params = randomRoomWeightedBySize == 36863
                    ? (new(Lot.Position)
                       {
                           BooleanConstraints = FindGoodLocationBooleans.PreferEmptyTiles | FindGoodLocationBooleans.Routable
                       })
                    : (new(GetRoomEntrance(randomRoomWeightedBySize))
                       {
                           BooleanConstraints = FindGoodLocationBooleans.PreferEmptyTiles | FindGoodLocationBooleans.Routable,
                           RequiredRoomID = randomRoomWeightedBySize
                       });

                if (!PlaceAtGoodLocation(gameObject, @params, true))
                {
                    return null;
                }
                gameObject.AddToWorld();
                gameObject.SetOpacity(0f, 0f);
                return gameObject;
            }

            private Vector3 GetRoomEntrance(int roomId)
            {
                List<IPortalConnectionObject> objectsInRoom = Lot.GetObjectsInRoom<IPortalConnectionObject>(roomId);
                return objectsInRoom.Count > 0 ? objectsInRoom[0].Position : Vector3.Invalid;
            }

            private int GetRandomRoomWeightedBySize()
            {
                List<GhostHunter.GhostHunterJob.RoomSizeInfo> list = new();
                LotDisplayLevelInfo levelInfo = World.LotGetDisplayLevelInfo(Lot.LotId);
                for (int i = levelInfo.mMin; i <= levelInfo.mMax; i++)
                {
                    list.AddRange(World.GetInsideRoomsAtLevel(Lot.LotId, i, eRoomDefinition.LightBlocking)
                                       .Where(roomNum => roomNum != 0 && !base.Lot.IsRoomHidden(roomNum))
                                       .Select(roomNum => new GhostHunter.GhostHunterJob.RoomSizeInfo(base.Lot, roomNum)));
                }
                return list.Count > 0 ? RandomUtil.GetWeightedRandomObjectFromList(list).RoomId : 36863;
            }
        }

        public override void CleanUp()
        {
            try
            {
                if (mPanicBroadcaster is not null)
                {
                    mPanicBroadcaster.Dispose();
                    mPanicBroadcaster = null;
                }
                List<Sim> ghosts = mParticipants.ToList();
                mParticipants.Clear();
                foreach (Sim sim in ghosts)
                {
                    sim.RemoveRole(this);
                    VisualEffect visualEffect = VisualEffect.Create("ep6GenieTargetSimDisappear_main");
                    visualEffect.ParentTo(sim, Sim.FXJoints.Spine0);
                    visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
                    sim.FadeOut(false, true);
                }
                foreach (GameObject @object in mUrnstones)
                {
                    if (@object is IUrnstone urnstone)
                    {
                        urnstone.SetDeadSimDescription(null);
                    }
                    @object.Destroy();
                    @object.Dispose();
                }
                foreach (LightGameObject lightGameObject in mPrevColorInfo.Keys)
                {
                    if (lightGameObject.InteractionsDisabled)
                    {
                        lightGameObject.EnableInteractions();
                    }
                    ColorInfo color = mPrevColorInfo[lightGameObject];
                    lightGameObject.Color = color.Color;
                    World.LightSetColor(lightGameObject.Proxy.ObjectId, color.CustomColorVec.x, color.CustomColorVec.y, color.CustomColorVec.z);
                }
                mPrevColorInfo.Clear();
            }
            finally
            {
                base.CleanUp();
            }
        }

        public override void OnReset(Sim sim)
        {
            if (mParticipants.Contains(sim))
            {
                mParticipants.Remove(sim);
                sim.RemoveRole(this);
                sim.Destroy();
                sim.Dispose();
                if (Urnstone.FindGhostsGrave(sim) is Urnstone urnstone)
                {
                    urnstone.SetDeadSimDescription(null);
                    urnstone.Destroy();
                    urnstone.Dispose();
                }
            }
        }

        public override void OnParticipantDeleted(Sim participant)
        {
            if (mParticipants.Contains(participant))
            {
                base.OnParticipantDeleted(participant);
            }
        }
    }
}