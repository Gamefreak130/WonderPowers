﻿using Gamefreak130.Common;
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
    //TODO Cleanup unused methods in LinqBridge
    //CONSIDER Common exception catching for alarms and tasks
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
            LoadSaveManager.ObjectGroupsPostLoad += OnPostLoad;
            World.OnWorldQuitEventHandler += OnWorldQuit;
        }

        private static void OnStartupApp(object sender, EventArgs e)
        {
            WonderPowerManager.Init();
            bool flag = true;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (name == "Skydome_KidsMagic")
                {
                    IsKidsMagicInstalled = true;
                }

                if (name == "Gamefreak130.LTRMenuMusicReplacement")
                {
                    flag = false;
                }
                else
                {
                    foreach (Type type in assembly.GetExportedTypes()
                                                  .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition && typeof(PowerBooter).IsAssignableFrom(type)))
                    {
                        PowerBooter booter = type.GetConstructor(new Type[0]).Invoke(null) as PowerBooter;
                        booter.LoadPowers();
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