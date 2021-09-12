using Gamefreak130.Common.Booters;
using Gamefreak130.WonderPowersSpace.Helpers;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using System.Reflection;

namespace Gamefreak130.WonderPowersSpace.Booters
{
    public abstract class PowerBooter : Booter
	{
		public PowerBooter(string xmlName) : base(xmlName)
		{
		}

		protected string PowerData => GetResourceAt(0);

		protected override void LoadData()
		{
			XmlDbData xmlDbData = XmlDbData.ReadData(PowerData);
			xmlDbData.Tables.TryGetValue("Power", out XmlDbTable xmlDbTable);
			foreach (XmlDbRow row in xmlDbTable.Rows)
			{
				string name = row["PowerName"];
				if (row.TryGetEnum("ProductVersion", out ProductVersion version, ProductVersion.Undefined) && GameUtils.IsInstalled(version) && !string.IsNullOrEmpty(name))
				{
					string runMethod = row["EffectMethod"];
					if (!string.IsNullOrEmpty(runMethod))
					{
						bool isBad = row.GetBool("IsBad");
						int cost = row.GetInt("Cost");
						MethodInfo methodInfo = FindMethod(runMethod, typeof(ActivationMethods));
						WonderPowerManager.AddPower(new(name, isBad, cost, methodInfo));
					}
				}
			}
		}
	}

	public sealed class WonderPowerBooter : PowerBooter
	{
		public WonderPowerBooter() : base("Gamefreak130_KarmaPowers")
		{
		}
	}
}
