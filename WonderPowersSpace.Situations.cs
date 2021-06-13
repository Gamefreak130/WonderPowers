using static Sims3.Gameplay.GlobalFunctions;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.SimIFace;
using System;
using System.Collections.Generic;
using Sims3.Gameplay;
using Queries = Sims3.Gameplay.Queries;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Utilities;
using Gamefreak130.WonderPowersSpace.Interactions;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Controllers;
using static Sims3.SimIFace.ResourceUtils;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace.CAS;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.ObjectComponents;
using Sims3.UI;
using Gamefreak130.WonderPowersSpace.Helpers;

namespace Gamefreak130.WonderPowersSpace.Situations
{
    public class CryHavocSituation : RootSituation
    {
        public CryHavocSituation()
        {
        }

        public CryHavocSituation(Lot lot) : base(lot) => SetState(new StartSituation(this));

        private List<Sim> mFighters;

        private List<GameObject> mFogEmitters;

        private AlarmHandle mExitHandle;

        private uint mSoundHandle;

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
                //CONSIDER reaction broadcast?
                Parent.mSoundHandle = Audio.StartSound("sting_cryhavoc", Lot.Position);
                Parent.mExitHandle = AlarmManager.Global.AddAlarm(TunableSettings.kCryHavocLength, TimeUnit.Minutes, Parent.Exit, "Gamefreak130 wuz here -- CryHavoc Situation Alarm", AlarmType.AlwaysPersisted, null);
                Camera.FocusOnLot(Lot.LotId, 2f); //2f is standard lerpTime
                Parent.mFighters = Lot.GetAllActors().FindAll(IsValidFighter);
                Parent.mFogEmitters = HelperMethods.CreateFogEmittersOnLot(Lot);

                if (Parent.mFighters.Count < TunableSettings.kCryHavocMinSims)
                {
                    List<Sim> otherSims = new List<Sim>(Queries.GetObjects<Sim>()).FindAll((sim) => !Parent.mFighters.Contains(sim) && IsValidFighter(sim));
                    while (Parent.mFighters.Count < TunableSettings.kCryHavocMinSims && otherSims.Count != 0)
                    {
                        Sim closestSim = GetClosestObject(otherSims, Lot);
                        if (closestSim is not null)
                        {
                            Parent.mFighters.Add(closestSim);
                            otherSims.Remove(closestSim);
                        }
                        else
                        {
                            otherSims.RemoveAt(0);
                        }
                    }
                }
                PruneFighters();
                foreach (Sim sim in Parent.mFighters)
                {
                    if (sim is not null)
                    {
                        sim.AssignRole(Parent);
                        GoToLotAndFight visitLot = new GoToLotAndFight.Definition().CreateInstance(Lot, sim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as GoToLotAndFight;
                        ForceSituationSpecificInteraction(sim, visitLot);
                    }
                }
                StyledNotification.Show(new(WonderPowerManager.LocalizeString("CryHavocTNS"), StyledNotification.NotificationStyle.kGameMessageNegative));
            }

            private bool IsValidFighter(Sim sim) => sim is { IsHorse: false, CanBeSocializedWith: true, SimDescription: { TeenOrAbove: true } };

            // Two Sims can fight each other only if they are both pets, both teens, or both adults or older
            // This method ensures that there are at least two fighters in each of these three groups
            // So that any Sim always has someone to fight with
            private void PruneFighters()
            {
                Predicate<Sim>[] predicates = {
                    (sim) => sim.IsPet,
                    (sim) => sim.SimDescription.Teen,
                    (sim) => sim.IsHuman && sim.SimDescription.AdultOrAbove
                };
                
                foreach (Predicate<Sim> predicate in predicates)
                {
                    if (Parent.mFighters.FindAll(predicate).Count == 1)
                    {
                        Parent.mFighters.RemoveAt(Parent.mFighters.FindIndex(predicate));
                    }
                }
            }
        }

        public override void CleanUp() 
        {
            try
            {
                foreach (Sim sim in mFighters)
                {
                    if (sim is not null)
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
                foreach (GameObject emitter in mFogEmitters)
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
                WonderPowerManager.TogglePowerRunning();
            }
        }

        public override void OnReset(Sim sim)
        {
            sim.BuffManager.RemoveElement(Buffs.BuffCryHavoc.kBuffCryHavocGuid);
            mFighters.Remove(sim);
            sim.RemoveRole(this);
            if (mFighters.Count == 0)
            {
                Exit();
            }
        }

        public override void OnParticipantDeleted(Sim participant)
        {
            mFighters.Remove(participant);
            participant.RemoveRole(this);
            if (mFighters.Count == 0)
            {
                Exit();
            }
        }
    }

    public class FireSituation : RootSituation
    {
        public FireSituation()
        {
        }

        public FireSituation(Lot lot) : base(lot) => SetState(new StartSituation(this));

        private AlarmHandle mExitHandle;

        public override void OnParticipantDeleted(Sim participant)
        {
        }

        private class StartSituation : ChildSituation<FireSituation>
        {
            public StartSituation()
            {
            }

            public StartSituation(FireSituation parent) : base(parent)
            {
            }

            private List<GameObject> mBurnableObjects;

            private List<Sim> mBurnableSims;

            public override void Init(FireSituation parent)
            {
                mBurnableObjects = Lot.GetObjects<GameObject>((@object) => @object is not Sim && @object.GetFireType() is not FireType.DoesNotBurn && !@object.Charred);
                mBurnableSims = Lot.GetSims((sim) => sim.IsHuman && sim.SimDescription.ChildOrAbove);
                Parent.mExitHandle = AlarmManager.Global.AddAlarm(1f, TimeUnit.Seconds, StartFires, "Gamefreak130 wuz here -- Fire situation alarm", AlarmType.AlwaysPersisted, null);
            }

            private void StartFires()
            {
                try
                {
                    Audio.StartSound("sting_firestorm");
                    Lot.AddAlarm(30f, TimeUnit.Seconds, () => Camera.FocusOnLot(Lot.LotId, 2f), "Gamefreak130 wuz here -- Activation focus alarm", AlarmType.NeverPersisted); //2f is standard lerptime

                    // For each fire spawned, there is a 25% chance it will ignite a burnable object,
                    // A 25% chance it will ignite a valid sim on the lot,
                    // And a 50% chance it will spawn directly on the ground
                    int numFires = RandomUtil.GetInt(TunableSettings.kFireMin, TunableSettings.kFireMax);
                    for (int i = 0; i < numFires; i++)
                    {
                        VisualEffect effect;
                        if (RandomUtil.CoinFlip())
                        {
                            if (RandomUtil.CoinFlip() && mBurnableObjects.Count != 0)
                            {
                                GameObject @object = RandomUtil.GetRandomObjectFromList(mBurnableObjects);
                                FireManager.AddFire(@object.PositionOnFloor, true);
                                effect = VisualEffect.Create("ep2DetonateMedium");
                                effect.SetPosAndOrient(@object.Position, @object.ForwardVector, @object.UpVector);
                                effect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
                                mBurnableObjects.Remove(@object);
                                continue;
                            }
                            else if (mBurnableSims.Count != 0)
                            {
                                Sim sim = RandomUtil.GetRandomObjectFromList(mBurnableSims);
                                sim.BuffManager.AddElement(BuffNames.OnFire, (Origin)HashString64("FromWonderPower"));
                                mBurnableSims.Remove(sim);
                                continue;
                            }
                        }
                        Vector3 pos = Lot.GetRandomPosition(true, true);
                        FireManager.AddFire(pos, true);
                    }
                }
                finally
                {
                    Parent.CheckForExit();
                }
            }
        }

        public override void CleanUp() 
        {
            AlarmManager.RemoveAlarm(mExitHandle);
            mExitHandle = AlarmHandle.kInvalidHandle;
            WonderPowerManager.TogglePowerRunning();
            base.CleanUp(); 
        }

        private void CheckForExit()
        {
            if ((Lot.FireManager is null or { NoFire: true }) && Lot.GetSims((sim) => FirefighterSituation.IsSimOnFire(sim)).Count == 0)
            {
                Exit();
            }
            else
            {
                mExitHandle = AlarmManager.Global.AddAlarm(1f, TimeUnit.Minutes, CheckForExit, "Gamefreak130 wuz here -- Fire situation alarm", AlarmType.AlwaysPersisted, null);
            }
        }
    }

    public class GhostsSituation : RootSituation
    {
        public GhostsSituation()
        {
        }

        public GhostsSituation(Lot lot) : base(lot) => SetState(new StartSituation(this));

        private readonly List<GameObject> mCreatedObjects = new();

        private List<Sim> mGhosts = new();

        private readonly Dictionary<LightGameObject, ColorInfo> mPrevColorInfo = new();

        private AlarmHandle mExitHandle;

        private uint mMusicHandle;

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
                try
                {
                    //TODO extract file out to avoid EP2 dependency
                    //CONSIDER reaction broadcast?
                    Parent.mMusicHandle = Audio.StartSound("sting_ghost_invasion", Lot.Position);
                    Parent.mExitHandle = AlarmManager.Global.AddAlarm(TunableSettings.kGhostInvasionLength, TimeUnit.Minutes, Parent.Exit, "Gamefreak130 wuz here -- GhostInvasion Situation Alarm", AlarmType.AlwaysPersisted, null);
                    Lot.AddAlarm(30f, TimeUnit.Seconds, () => Camera.FocusOnLot(Lot.LotId, 2f), "Gamefreak130 wuz here -- Activation focus alarm", AlarmType.NeverPersisted);
                    foreach (Sim sim in Lot.GetSims())
                    {
                        if (sim.IsSleeping)
                        {
                            sim.InteractionQueue.CancelAllInteractions();
                        }
                    }
                    Parent.mCreatedObjects.AddRange(HelperMethods.CreateFogEmittersOnLot(Lot));

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

                    
                    for (int i = 0; i < RandomUtil.GetInt(TunableSettings.kFireMin, TunableSettings.kFireMax); i++)
                    {
                        Simulator.AddObject(new OneShotFunction(CreateAngryGhost));
                    }
                    
                }
                catch (Exception e)
                {
                    PowerExceptionLogger.sInstance.Log(e);
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
                    ghost = OccultRobot.MakeRobot(CASAgeGenderFlags.Adult, (CASAgeGenderFlags)Common.Helpers.CoinFlipSelect(CASAgeGenderFlags.Male, CASAgeGenderFlags.Female), (RobotForms)Common.Helpers.CoinFlipSelect(RobotForms.Hovering, RobotForms.Humanoid));
                    ghost.SetDeathStyle(SimDescription.DeathType.Robot, false);
                }
                else
                {
                    ghost = Genetics.MakeSim((CASAgeGenderFlags)Common.Helpers.CoinFlipSelect(CASAgeGenderFlags.Adult, CASAgeGenderFlags.Elder), (CASAgeGenderFlags)Common.Helpers.CoinFlipSelect(CASAgeGenderFlags.Male, CASAgeGenderFlags.Female), randomObjectFromList, 4294967295u);
                    ghost.FirstName = SimUtils.GetRandomGivenName(ghost.IsMale, randomObjectFromList);
                    ghost.LastName = SimUtils.GetRandomFamilyName(randomObjectFromList);
                    ghost.SetDeathStyle(RandomUtil.GetRandomObjectFromList(sValidDeathTypes), false);
                    TraitNames trait = ghost.DeathStyle switch
                    {
                        SimDescription.DeathType.Drown => TraitNames.Hydrophobic,
                        SimDescription.DeathType.Electrocution => TraitNames.AntiTV,
                        SimDescription.DeathType.Burn => TraitNames.Pyromaniac,
                        _ => TraitNames.Unknown
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
                Parent.mGhosts.Add(ghost.CreatedSim);
                Parent.mCreatedObjects.Add((GameObject)urnstone);
                return;
            }

            private IGameObject CreateGhostJig()
            {
                int randomRoomWeightedBySize = GetRandomRoomWeightedBySize();
                if (randomRoomWeightedBySize == 36863)
                {
                    return null;
                }
                IGameObject gameObject = CreateObjectOutOfWorld("SocialJigOnePerson");
                World.FindGoodLocationParams @params = new(GetRoomEntrance(randomRoomWeightedBySize))
                {
                    BooleanConstraints = FindGoodLocationBooleans.PreferEmptyTiles | FindGoodLocationBooleans.Routable,
                    RequiredRoomID = randomRoomWeightedBySize
                };
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
                    foreach (int num in World.GetInsideRoomsAtLevel(Lot.LotId, i, eRoomDefinition.LightBlocking))
                    {
                        if (num != 0 && !base.Lot.IsRoomHidden(num))
                        {
                            list.Add(new(base.Lot, num));
                        }
                    }
                }
                return list.Count > 0 ? RandomUtil.GetWeightedRandomObjectFromList(list).RoomId : 36863;
            }
        }

        public override void CleanUp()
        {
            try
            {
                AlarmManager.RemoveAlarm(mExitHandle);
                mExitHandle = AlarmHandle.kInvalidHandle;
                if (mMusicHandle != 0)
                {
                    Audio.StopSound(mMusicHandle);
                    mMusicHandle = 0;
                }
                Sim[] ghosts = new Sim[mGhosts.Count];
                mGhosts.CopyTo(ghosts);
                mGhosts = null;
                for (int i = ghosts.Length - 1; i >= 0; i--)
                {
                    Sim sim = ghosts[i];
                    // TODO see if this can be extracted out too
                    VisualEffect visualEffect = VisualEffect.Create("ep6GenieTargetSimDisappear_main");
                    visualEffect.ParentTo(sim, Sim.FXJoints.Spine0);
                    visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
                    sim.FadeOut(false, true);
                }
                foreach (GameObject @object in mCreatedObjects)
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
                base.CleanUp();
            }
            finally
            {
                WonderPowerManager.TogglePowerRunning();
            }
        }

        public override void OnReset(Sim sim)
        {
            if (mGhosts is not null)
            {
                if (mGhosts.Contains(sim))
                {
                    mGhosts.Remove(sim);
                    sim.RemoveRole(this);
                    sim.Destroy();
                    sim.Dispose();
                }
                if (mGhosts.Count == 0)
                {
                    Exit();
                }
            }
        }

        public override void OnParticipantDeleted(Sim participant)
        {
            if (mGhosts is not null)
            {
                mGhosts.Remove(participant);
                participant.RemoveRole(this);
                if (mGhosts.Count == 0)
                {
                    Exit();
                }
            }
        }
    }
}