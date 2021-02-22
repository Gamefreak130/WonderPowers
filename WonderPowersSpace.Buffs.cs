using Sims3.SimIFace;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.UI;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace.CAS;
using System.Collections.Generic;
using Sims3.Gameplay.Objects;
using Gamefreak130.Common;

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

    public class BuffDoom : BuffUnicornsIre
    {
        public const ulong kBuffDoomGuid = 0xE6E44C7930BDD3F3;

        public BuffDoom(BuffData data) : base(data)
        {
        }
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
                List<SimDescription.DeathType> types;
                if (sim.IsHuman)
                {
                    types = new(sHumanDeathTypes);
                }
                else
                {
                    return (uint)Common.Helpers.CoinFlipSelect(SimDescription.DeathType.PetOldAgeGood, SimDescription.DeathType.PetOldAgeBad);
                }

                List<ObjectPicker.HeaderInfo> list = new()
                {
                    new("Ui/Caption/ObjectPicker:Ghost", "Ui/Caption/ObjectPicker:Ghost", 300)
                };
                List<ObjectPicker.RowInfo> list2 = new();
                foreach (SimDescription.DeathType current in types)
                {
                    string name = Sims3.UI.CAS.CASBasics.mGhostDeathNames[types.IndexOf(current)];
                    ObjectPicker.RowInfo item = new ObjectPicker.RowInfo(current, new()
                    {
                        new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(ResourceKey.CreatePNGKey(name, 0u), ThumbnailSize.ExtraLarge), Urnstone.DeathTypeToLocalizedString(current))
                    });
                    list2.Add(item);
                }
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

        public override void OnRemoval(BuffManager bm, BuffInstance bi) => base.OnRemoval(bm, bi);
    }
}