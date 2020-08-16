using Gamefreak130.WonderPowersSpace.Helpers;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using System;

namespace Gamefreak130.Common
{
    public class BuffBooter
    {
        public string mXmlResource;

        public BuffBooter(string xmlResource)
        {
            mXmlResource = xmlResource;
        }

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
    public class WonderPowers
    {
        [Tunable]
        private static readonly bool kCJackB;

        static WonderPowers()
        {
            World.OnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            LoadSaveManager.ObjectGroupsPostLoad += OnPostLoad;
            World.OnWorldQuitEventHandler += OnWorldQuit;
        }

        private static void OnWorldLoadFinished(object sender, EventArgs e)
        {
            EventTracker.AddListener(EventTypeId.kEnterInWorldSubState, OnEnteredWorld);
        }

        private static void OnPreLoad()
        {
            WonderPowersSpace.Helpers.WonderPowers.PreWorldLoadStartup();
            WonderPower.LoadPowers("KarmaPowers");
        }

        private static void OnPostLoad()
        {
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