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
                //Audio.StartSound("sting_death", new Function(() => Parent.SetState(new EndSituation(Parent))));
                Sims3.Gameplay.Utilities.AlarmManager.Global.AddAlarm(180f, Sims3.Gameplay.Utilities.TimeUnit.Minutes, delegate { Parent.SetState(new EndSituation(Parent)); }, "DEBUG", Sims3.Gameplay.Utilities.AlarmType.AlwaysPersisted, null);
                Camera.FocusOnLot(Lot.LotId, 2f); //2f is standard lerpTime
                Parent.mFighters = new List<Sim>(Lot.GetSims()).FindAll((sim) => IsValidFighter(sim));

                while (Parent.mFighters.Count < TunableSettings.kCryHavocMinSims)
                {
                    List<Sim> otherSims = new List<Sim>(Queries.GetObjects<Sim>()).FindAll((sim) => !Parent.mFighters.Contains(sim));
                    if (otherSims.Count == 0) { break; }
                    Parent.mFighters.Add(GetClosestObject(otherSims, Lot, IsValidFighter));
                }

                foreach (Sim sim in Parent.mFighters)
                {
                    sim.AssignRole(Parent);
                    GoToLotAndFight visitLot = new GoToLotAndFight.Definition().CreateInstance(Lot, sim, new InteractionPriority(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as GoToLotAndFight;
                    ForceSituationSpecificInteraction(sim, visitLot);
                }
            }

            private bool IsValidFighter(Sim sim) => sim != null && sim.SimDescription.TeenOrAbove && !sim.IsHorse && sim.CanBeSocializedWith;
        }

        public class EndSituation : ChildSituation<CryHavocSituation>
        {
            public EndSituation()
            {
            }

            public EndSituation(CryHavocSituation parent) : base(parent)
            { 
            }

            public override void Init(CryHavocSituation parent)
            {
                foreach (Sim sim in parent.mFighters)
                {
                    sim.BuffManager.RemoveElement(Buffs.BuffCryHavoc.kBuffCryHavocGuid);
                    sim.RemoveRole(parent);
                    Sim.MakeSimGoHome(sim, false, new InteractionPriority(InteractionPriorityLevel.CriticalNPCBehavior));
                }
                Exit();
            }
        }

        public class GoToLotAndFight : GoToLot
        {
            new public class Definition : GoToLot.Definition
            {
                public override bool Test(Sim actor, Lot target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback) => true;

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    InteractionInstance instance = new GoToLotAndFight() { CancellableByPlayer = parameters.CancellableByPlayer };
                    instance.Init(ref parameters);
                    return instance;
                }
            }

            public override bool Run()
            {
                bool flag = base.Run();
                if (flag)
                {
                    Actor.BuffManager.AddElement(Buffs.BuffCryHavoc.kBuffCryHavocGuid, Origin.None);
                }
                return flag;
            }
        }

        private List<Sim> mFighters;

        public override void OnReset(Sim sim)
        {
            sim.BuffManager.RemoveElement(Buffs.BuffCryHavoc.kBuffCryHavocGuid);
            mFighters.Remove(sim);
            sim.RemoveRole(this);
            if (mFighters.Count == 0)
            {
                SetState(new EndSituation(this));
            }
        }

        public override void OnParticipantDeleted(Sim participant)
        {
            mFighters.Remove(participant);
            participant.RemoveRole(this);
            if (mFighters.Count == 0)
            {
                SetState(new EndSituation(this));
            }
        }
    }
}
