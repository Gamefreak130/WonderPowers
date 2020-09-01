using Gamefreak130.Common;
using Gamefreak130.WonderPowersSpace.Helpers;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using System;
using System.Reflection;

namespace Gamefreak130.Common
{
    public delegate T GenericDelegate<T>();

    public class RepeatingFunctionTask : Task
    {
        private StopWatch mTimer;

        private readonly int mDelay;

        private readonly GenericDelegate<bool> mFunction;

        public RepeatingFunctionTask(GenericDelegate<bool> function)
        {
            mFunction = function;
            mDelay = 500;
        }

        public RepeatingFunctionTask(GenericDelegate<bool> function, int delay)
        {
            mFunction = function;
            mDelay = delay;
        }

        public override void Dispose()
        {
            if (mTimer != null)
            {
                mTimer.Dispose();
                mTimer = null;
            }
            if (ObjectId != ObjectGuid.InvalidObjectGuid)
            {
                Simulator.DestroyObject(ObjectId);
                ObjectId = ObjectGuid.InvalidObjectGuid;
            }
            base.Dispose();
        }

        public override void Simulate()
        {
            mTimer = StopWatch.Create(StopWatch.TickStyles.Milliseconds);
            mTimer.Start();
            do
            {
                mTimer.Restart();
                while (mTimer != null && mTimer.GetElapsedTime() < mDelay)
                {
                    if (Simulator.CheckYieldingContext(false))
                    {
                        Simulator.Sleep(0u);
                    }
                }
                if (!mFunction())
                {
                    Dispose();
                    break;
                }
                if (Simulator.CheckYieldingContext(false))
                {
                    Simulator.Sleep(0u);
                }
            }
            while (mTimer != null);
        }
    }

    public class BuffBooter
    {
        private readonly string mXmlResource;

        public BuffBooter(string xmlResource) => mXmlResource = xmlResource;

        public void LoadBuffData()
        {
            AddBuffs(null);
            UIManager.NewHotInstallStoreBuffData += new UIManager.NewHotInstallStoreBuffCallback(AddBuffs);
        }

        public void AddBuffs(ResourceKey[] resourceKeys)
        {
            XmlDbData xmlDbData = XmlDbData.ReadData(mXmlResource);
            if (xmlDbData != null)
            {
                Sims3.Gameplay.ActorSystems.BuffManager.ParseBuffData(xmlDbData, true);
            }
        }
    }
}

namespace Gamefreak130
{
    //TODO Cleanup LAYO
    //TODO Check code for unused blocks, zero references, notimplementedexceptions
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
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool flag = Array.Exists(assemblies, (assembly) => assembly.FullName.Contains("Gamefreak130.LTRMenuMusicReplacement"));
            if (!flag)
            {
                Simulator.AddObject(new RepeatingFunctionTask(OptionsInjector.InjectOptions));
            }
        }

        private static void OnPreLoad()
        {
            WonderPowersSpace.Helpers.WonderPowers.PreWorldLoadStartup();
            WonderPower.LoadPowers("KarmaPowers");
            new BuffBooter("Gamefreak130_KarmaBuffs").LoadBuffData();
        }

        private static void OnPostLoad()
        {
        }

        private static void OnWorldLoadFinished(object sender, EventArgs e)
        {
            EventTracker.AddListener(EventTypeId.kEnterInWorldSubState, OnEnteredWorld);
        }

        private static void OnWorldQuit(object sender, EventArgs e)
        {
            WonderPowersSpace.Helpers.WonderPowers.WorldLoadShutdown();
        }

        private static ListenerAction OnEnteredWorld(Event e)
        {
            if (Sims3.UI.Hud.RewardTraitsPanel.Instance?.GetChildByID(799350305u, true) is Button button)
            {
                button.Click += delegate(WindowBase sender, UIButtonClickEventArgs eventArgs) {
                    Simulator.AddObject(new OneShotFunctionTask(WonderPowersSpace.Helpers.UI.WonderModeMenu.Show));
                    eventArgs.Handled = true;
                };
            }
            return ListenerAction.Remove;
        }
    }
}