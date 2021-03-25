using Gamefreak130.Common;
using Gamefreak130.WonderPowersSpace.Helpers;
using Gamefreak130.WonderPowersSpace.UI;
using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects;
using Sims3.SimIFace;
using Sims3.UI;
using System;
using System.Reflection;

namespace Gamefreak130
{
    //TODO Cleanup LAYO
    //TODO Check code for unused blocks, zero references, notimplementedexceptions
    //TODO Find way to dynamically load XML for custom powers
    //TODO Command to set karma, reset cooldown
    //CONSIDER SortedList for powers (by cost)
    //CONSIDER Powers implement IWeightable for karmic backlash selection
    public static class WonderPowers
    {
        [Tunable]
        private static readonly bool kCJackB;

        static WonderPowers()
        {
            World.OnStartupAppEventHandler += OnStartupApp;
            World.OnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            LoadSaveManager.ObjectGroupsPostLoad += OnPostLoad;
            World.OnWorldQuitEventHandler += OnWorldQuit;
        }

        private static void OnStartupApp(object sender, EventArgs e)
        {
            WonderPowerManager.Init();
            WonderPowerManager.LoadMainPowers();
            bool flag = true;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Gamefreak130.LTRMenuMusicReplacement")
                {
                    flag = false;
                }
                else
                {
                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (!type.IsAbstract && !type.IsGenericTypeDefinition && typeof(IPowerBooter).IsAssignableFrom(type) && type != typeof(WonderPowerManager))
                        {
                            IPowerBooter booter = type.GetConstructor(new Type[0]).Invoke(null) as IPowerBooter;
                            booter.LoadPowers();
                        }
                    }
                }
            }
            if (flag)
            {
                Simulator.AddObject(new RepeatingFunctionTask(OptionsInjector.InjectOptions));
            }
        }

        private static void OnPreLoad()
        {
            new BuffBooter("Gamefreak130_KarmaBuffs").LoadBuffData();
            Tunings.Inject(GoToLot.Singleton.GetType(), typeof(Lot), typeof(WonderPowersSpace.Interactions.GoToLotAndFight), typeof(Lot), true);
            Tunings.Inject(Urnstone.ResurrectSim.Singleton.GetType(), typeof(Sim), typeof(WonderPowersSpace.Interactions.DivineInterventionResurrect), typeof(Sim), true);
        }

        private static void OnPostLoad()
        {
            WonderPowerManager.LoadValues();
        }

        private static void OnWorldLoadFinished(object sender, EventArgs e)
        {
            EventTracker.AddListener(EventTypeId.kEnterInWorldSubState, OnEnteredWorld);
        }

        private static void OnWorldQuit(object sender, EventArgs e)
        {
            WonderPowerManager.ReInit();
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
            GameStates.sSingleton.mInWorldState.mStateMachine.AddState(new CASWonderModeState());
            return ListenerAction.Remove;
        }
    }
}