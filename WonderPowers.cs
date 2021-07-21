using Gamefreak130.Common.Booters;
using Gamefreak130.Common.Helpers;
using Gamefreak130.Common.Interactions;
using Gamefreak130.Common.Tasks;
using Gamefreak130.WonderPowersSpace.Helpers;
using Gamefreak130.WonderPowersSpace.UI;
using Sims3.Gameplay;
using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using System;
using System.Linq;
using System.Reflection;

namespace Gamefreak130
{
    //TODO Cleanup LAYO
    //TODO Check code for unused blocks, zero references, notimplementedexceptions
    //TODO Command to set karma, reset cooldown
    //CONSIDER Common exception catching for alarms
    //CONSIDER SortedList for powers (by cost)
    //CONSIDER Powers implement IWeightable for karmic backlash selection
    public static class WonderPowers
    {
        [Tunable]
        private static readonly bool kCJackB;

        public static bool IsKidsMagicInstalled;

        static WonderPowers()
        {
            World.OnStartupAppEventHandler += OnStartupApp;
            World.OnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.OnWorldQuitEventHandler += OnWorldQuit;
        }

        private static void OnStartupApp(object sender, EventArgs e)
        {
            WonderPowerManager.Init();
            if (GameUtils.IsInstalled(ProductVersion.EP5))
            {
                TransmogrifyTraitMapping.Init();
            }
            IsKidsMagicInstalled = ReflectionEx.IsAssemblyLoaded("Skydome_KidsMagic");
            foreach (ConstructorInfo ctor in AppDomain.CurrentDomain.GetAssemblies()
                                                                    .SelectMany(assembly => assembly.GetExportedTypes())
                                                                    .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition && typeof(PowerBooter).IsAssignableFrom(type))
                                                                    .Select(type => type.GetConstructor(new Type[0]))
                                                                    .OfType<ConstructorInfo>())
            {
                PowerBooter booter = ctor.Invoke(null) as PowerBooter;
                booter.Boot();
            }
            if (!ReflectionEx.IsAssemblyLoaded("Gamefreak130.LTRMenuMusicReplacement"))
            {
                Simulator.AddObject(new RepeatingFunctionTask(OptionsInjector.InjectOptions));
            }
        }

        private static void OnPreLoad()
        {
            if (!GameStates.IsTravelling)
            {
                WonderPowerManager.Init();
            }
            new BuffBooter("Gamefreak130_KarmaBuffs").Boot();
            if (GenericManager<BuffNames, BuffInstance, BuffInstance>.sDictionary.TryGetValue((ulong)BuffNames.UnicornsBlessing, out BuffInstance buff))
            {
                buff.mBuff.mInfo.mProductVersion = ProductVersion.BaseGame;
            }
            if (GenericManager<BuffNames, BuffInstance, BuffInstance>.sDictionary.TryGetValue((ulong)BuffNames.UnicornsIre, out BuffInstance buff2))
            {
                buff2.mBuff.mInfo.mProductVersion = ProductVersion.BaseGame;
            }
            Tunings.Inject(GoToLot.Singleton.GetType(), typeof(Lot), typeof(WonderPowersSpace.Interactions.GoToLotAndFight.Definition), typeof(Lot), true);
            Tunings.Inject(Urnstone.ResurrectSim.Singleton.GetType(), typeof(Sim), typeof(WonderPowersSpace.Interactions.DivineInterventionResurrect.Definition), typeof(Sim), true);
            Tunings.Inject(GhostHunter.ReactToAngryGhost.Singleton.GetType(), typeof(Sim), typeof(WonderPowersSpace.Interactions.ReactToGhost.Definition), typeof(Sim), true);
        }

        private static void OnWorldLoadFinished(object sender, EventArgs e)
        {
            EventTracker.AddListener(EventTypeId.kEnterInWorldSubState, OnEnteredWorld);
        }

        private static void OnWorldQuit(object sender, EventArgs e)
        {
            //TODO remove if still empty once we're done lol
        }

        private static ListenerAction OnEnteredWorld(Event e)
        {
            if (Sims3.UI.Hud.RewardTraitsPanel.Instance?.GetChildByID(799350305u, true) is Button button)
            {
                button.Click += (sender, eventArgs) =>
                {
                    Simulator.AddObject(new Sims3.UI.OneShotFunctionTask(WonderModeMenu.Show));
                    eventArgs.Handled = true;
                };
                //TEST travel while powers running
                button.Enabled = !WonderPowerManager.IsPowerRunning;
            }
            GameStates.sSingleton.mInWorldState.mStateMachine.AddState(new CASInstantBeautyState());
            GameStates.sSingleton.mInWorldState.mStateMachine.AddState(new CASTransmogrifyState());
            return ListenerAction.Remove;
        }
    }
}