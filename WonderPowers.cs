using System;
using System.Collections;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

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
    public class WonderPowers
    {
        [Tunable]
        private static readonly bool kCJackB;
    }
}