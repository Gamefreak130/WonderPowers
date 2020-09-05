using Sims3.SimIFace;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.UI;
using Sims3.Gameplay.Utilities;

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
                        Common.Methods.ForceSocial(actor, target, social, InteractionPriorityLevel.CriticalNPCBehavior, false);
                    }
                }
                actor.AddAlarm(1f, TimeUnit.Seconds, delegate { CruiseForBruise(actor); }, "Gamefreak130 wuz here -- Cry Havoc alarm", AlarmType.DeleteOnReset);
            }
        }

        private static bool CanFight(Sim x, Sim y)
            => y != null && (y.CurrentInteraction is null || y.CurrentInteraction.GetPriority().Level < InteractionPriorityLevel.CriticalNPCBehavior) && x != y 
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
}