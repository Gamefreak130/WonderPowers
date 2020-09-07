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

namespace Gamefreak130.WonderPowersSpace.Situations
{
    public class CryHavocSituation : RootSituation
    {
        public CryHavocSituation()
        {
        }

        public CryHavocSituation(Lot lot) : base(lot) => SetState(new StartSituation(this));

        public class StartSituation : ChildSituation<CryHavocSituation>
        {
            public StartSituation()
            {
            }

            public StartSituation(CryHavocSituation parent) : base(parent)
            {
            }

            public override void Init(CryHavocSituation parent)
            {
                //TODO Add fog effect to lot
                //TODO Change sound, make 3d sound based on lot position
                //CONSIDER Animation for Sims on exit?
                //Audio.StartSound("sting_death", new Function(() => Parent.SetState(new EndSituation(Parent))));
                AlarmManager.Global.AddAlarm(180f, TimeUnit.Minutes, delegate { Parent.Exit(); }, "DEBUG", AlarmType.AlwaysPersisted, null);
                Camera.FocusOnLot(Lot.LotId, 2f); //2f is standard lerpTime
                Parent.mFighters = Lot.GetAllActors().FindAll(IsValidFighter);

                while (Parent.mFighters.Count < TunableSettings.kCryHavocMinSims)
                {
                    List<Sim> otherSims = new List<Sim>(Queries.GetObjects<Sim>()).FindAll((sim) => !Parent.mFighters.Contains(sim));
                    if (otherSims.Count == 0) { break; }
                    Parent.mFighters.Add(GetClosestObject(otherSims, Lot, IsValidFighter));
                }
                PruneFighters();
                foreach (Sim sim in Parent.mFighters)
                {
                    if (sim != null)
                    {
                        sim.AssignRole(Parent);
                        GoToLotAndFight visitLot = new GoToLotAndFight.Definition().CreateInstance(Lot, sim, new InteractionPriority(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as GoToLotAndFight;
                        ForceSituationSpecificInteraction(sim, visitLot);
                    }
                }
            }

            private bool IsValidFighter(Sim sim) => sim != null && sim.SimDescription.TeenOrAbove && !sim.IsHorse && sim.CanBeSocializedWith;

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

        private List<Sim> mFighters;

        public override void CleanUp() 
        {
            try
            {
                foreach (Sim sim in mFighters)
                {
                    if (sim != null)
                    {
                        sim.BuffManager.RemoveElement(Buffs.BuffCryHavoc.kBuffCryHavocGuid);
                        sim.RemoveRole(this);
                        Sim.MakeSimGoHome(sim, false, new InteractionPriority(InteractionPriorityLevel.CriticalNPCBehavior));
                    }
                }
                base.CleanUp();
            }
            finally
            {
                Helpers.WonderPowers.TogglePowerRunning();
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

        public override void OnParticipantDeleted(Sim participant)
        {
        }

        public class StartSituation : ChildSituation<FireSituation>
        {
            public StartSituation()
            {
            }

            public StartSituation(FireSituation parent) : base(parent)
            {
            }

            List<GameObject> mBurnableObjects;

            List<Sim> mBurnableSims;

            public override void Init(FireSituation parent)
            {
                mBurnableObjects = Lot.GetObjects<GameObject>((@object) => !(@object is Sim) && @object.GetFireType() != FireType.DoesNotBurn && !@object.Charred);
                mBurnableSims = Lot.GetSims((sim) => sim.IsHuman && sim.SimDescription.ChildOrAbove);
                AlarmManager.Global.AddAlarm(1f, TimeUnit.Seconds, StartFires, "Gamefreak130 wuz here -- Fire situation alarm", AlarmType.AlwaysPersisted, null);
            }

            private void StartFires()
            {
                try
                {
                    //TODO
                    //Audio.StartSound("sting_firestorm");
                    Camera.FocusOnLot(Lot.LotId, 2f); //2f is standard lerptime

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
            Helpers.WonderPowers.TogglePowerRunning();
            base.CleanUp(); 
        }

        private void CheckForExit()
        {
            if ((Lot.FireManager == null || Lot.FireManager.NoFire) && Lot.GetSims((sim) => FirefighterSituation.IsSimOnFire(sim)).Count == 0)
            {
                Exit();
            }
            else
            {
                AlarmManager.Global.AddAlarm(1f, TimeUnit.Minutes, CheckForExit, "Gamefreak130 wuz here -- Fire situation alarm", AlarmType.AlwaysPersisted, null);
            }
        }
    }
}
