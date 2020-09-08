using System;
using System.Collections;
using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.DreamsAndPromises;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.Gameplay.Utilities;
using static Sims3.Gameplay.UI.PieMenu;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.RouteDestinations;
using Sims3.UI;
using Sims3.UI.Hud;
using Gamefreak130.WonderPowersSpace.Helpers.UI;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interfaces;
using System.Reflection;
using Sims3.Gameplay.CAS;
using System.Xml;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.MapTags;
using Gameflow = Sims3.Gameplay.Gameflow;
using static Sims3.SimIFace.Gameflow.GameSpeed;
using static Sims3.Gameplay.GlobalFunctions;
using Gamefreak130.WonderPowersSpace.Situations;
using Sims3.Gameplay.Objects.Miscellaneous;
using Responder = Sims3.UI.Responder;
using Queries = Sims3.Gameplay.Queries;
using Sims3.Gameplay.Objects;
using Gamefreak130.WonderPowersSpace.Interactions;
using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.SimIFace.Enums;
using static Sims3.SimIFace.ResourceUtils;

namespace Gamefreak130.WonderPowersSpace.Helpers
{
	[Persistable]
	public class WonderPower : IEquatable<WonderPower>
	{
		public string WonderPowerName
		{
			get;
			private set;
		}

		public bool IsBadPower 
		{ 
			get;
			private set;
		}

		private MethodInfo mRunMethod;

		private int mCost;

		//public bool IsLocked;

		/*public abstract bool WasUsed
		{
			get;
			set;
		}*/

		public WonderPower()
        {
        }

		public WonderPower(string name, bool isBad, int cost, MethodInfo runMethod)
		{
			WonderPowerName = name;
			IsBadPower = isBad;
			mCost = cost;
			mRunMethod = runMethod;
			WonderPowers.Add(this);
		}

		public void Destroy()
		{
			WonderPowers.Remove(this);
		}

		[Persistable(false)]
		private delegate void RunDelegate(bool isBacklash);//, GameObject target);

		public void Run(object isBacklash)
        {
			try
			{
				Gameflow.SetGameSpeed(Normal, Gameflow.SetGameSpeedContext.Gameplay);
				// Activation of any power will disable the karma menu
				// Re-enabling is left to the powers' individual run methods when activation is complete
				WonderPowers.TogglePowerRunning();
				RunDelegate run = (RunDelegate)Delegate.CreateDelegate(typeof(RunDelegate), mRunMethod);
				run((bool)isBacklash);
			}
			catch
            {//TODO Log power errors 'cause apparently NRaas won't do it for me (also refunds maybe)
				StyledNotification.Show(new StyledNotification.Format(WonderPowers.LocalizeString("PowerError"), StyledNotification.NotificationStyle.kSystemMessage));
				WonderPowers.TogglePowerRunning();
            }
		}

		public int Cost()
        {
			/*if (WonderPowers.NumFreePowers > 0)
			{
				return 0;
			}*/
			Sim selectedActor = PlumbBob.SelectedActor;
			int cost = mCost;
			if (selectedActor != null)
			{
				foreach (Sim current in selectedActor.Household.Sims)
				{
					if (!IsBadPower && current.SimDescription.TraitManager.HasElement(TraitNames.Good))
					{
						//cost *= WonderPowers.kGoodTraitDiscount;
					}
					if (IsBadPower && current.SimDescription.TraitManager.HasElement(TraitNames.Evil))
					{
						//cost *= WonderPowers.kBadTraitDiscount;
					}
				}
			}
			return cost;
		}

		internal static void LoadPowers(string xml)
		{
			List<string> list = new List<string>();
			XmlDbData xmlDbData = XmlDbData.ReadData(xml);
			XmlDbTable xmlDbTable = null;
			xmlDbData?.Tables.TryGetValue("Power", out xmlDbTable);
			if (xmlDbTable != null)
			{
				foreach (XmlDbRow row in xmlDbTable.Rows)
				{
					string name = row.GetString("PowerName");
					if (row.TryGetEnum("ProductVersion", out ProductVersion version, ProductVersion.Undefined) && GameUtils.IsInstalled(version) && !string.IsNullOrEmpty(name))
					{
						string runMethod = row.GetString("EffectMethod");
						if (!string.IsNullOrEmpty(runMethod))
						{
							bool isBad = row.GetBool("IsBad");
							int cost = row.GetInt("Cost");
							MethodInfo methodInfo = WonderPowers.FindMethod(runMethod);
							new WonderPower(name, isBad, cost, methodInfo);
							list.Add(name);
						}
					}
				}
			}

			for (int i = WonderPowers.GetWonderPowerList().Count - 1; i >= 0; i--)
			{
				WonderPower power = WonderPowers.GetWonderPowerList()[i];
				if (!list.Contains(power.WonderPowerName))
				{
					power.Destroy();
				}
			}
		}

        public bool Equals(WonderPower s) => WonderPowerName == s.WonderPowerName;

        public void AssignTo(WonderPower s)
        {
			s.IsBadPower = IsBadPower;
			s.mCost = mCost;
			s.mRunMethod = mRunMethod;
		}
    }

	[Persistable]
	public class WonderPowers : ScriptObject
	{
		private enum WitchingHourState
		{
			NONE,
			PRE_WITCHINGHOUR,
			WITCHINGHOUR,
			POST_WITCHINGHOUR
		}

		private const int kWishFulfillmentIndex = 0;

		private const int kWitchingHourIndex = 1;

		private static WonderPower mWitchingHourPower;

		private static VisualEffect mWitchingVfx;

		private static VisualEffect mWitchingFloorVfx;

		private static bool bHaveShownFirstWishFulfillmentDialog = false;

		private static bool bHaveShownWitchingHourDialog = false;

		private float TotalPromisesFulfilledKarma;

		private int KarmaPromisesFulfilled;

		private int TotalPromisesFulfilled;

		[Tunable, TunableComment("How many karma points the player starts with")]
		private static readonly float kInitialKarmaLevel = 200f;//TODO change this back to 0

		[Tunable, TunableComment("How much karma is gained daily, low range")]
		private static readonly float kKarmaDailyRationLow = 9f;

		[Tunable, TunableComment("How much karma is gained daily, high range")]
		private static readonly float kKarmaDailyRationHigh = 16f;

		[Tunable, TunableComment("Amount of karma gained when the user fulfills a basic wish")]
		private static readonly float kKarmaBasicWishAmount = 1f;

		[Tunable, TunableComment("Amount of karma gained when the user fulfills a lifetime wish")]
		private static readonly float kKarmaLifetimeWishAmount = 100f;

		[Tunable, TunableComment("How much to modify the karma gain from completing a wish with each unlocked Karma level.")]
		private static readonly int kKarmaWishAmountModifierPerLevel = 1;

		[Tunable, TunableComment("Base bad karma event increase factor (added each time a good power is used)")]
		private static readonly float kKarmaBadEventIncreaseConstant = 0f;

		[Tunable, TunableComment("Distance to check for affected nearby Sims")]
		private static readonly float kNearbySimsDistance = 10f;

		public static Vector3 kWonderBuffVfxOffset = new Vector3(0.097f, 0.5f, -0.113f);

		private static WitchingHourState smWitchingHourState = WitchingHourState.NONE;

		[PersistableStatic]
		private static WonderPowers sInstance;

		public readonly List<WonderPower> mAllWonderPowers = new List<WonderPower>();

		private bool mIsPowerRunning;

		public static bool IsPowerRunning
		{
			get => sInstance.mIsPowerRunning;
			private set
            {
				if (RewardTraitsPanel.Instance?.GetChildByID(799350305u, true) is Button button)
				{
					button.Enabled = !value;
					sInstance.mIsPowerRunning = value;
				}
			}
		}

        public static void TogglePowerRunning() => IsPowerRunning = !IsPowerRunning;

        //private readonly List<WonderPowerActivation> mActiveWonderPowers = new List<WonderPowerActivation>();

        private bool mDebugBadPowersOn;

		private DateAndTime mlastRunTime;

		private static bool mBadPowersOn = true;

		[PersistableStatic]
		private static float sCurrentKarmaLevel = kInitialKarmaLevel;

		[PersistableStatic]
		private static float sCurrentBadKarmaChance = 0f;

		public static float NearbySimsDistance
		{
			get
			{
				return kNearbySimsDistance;
			}
		}

		private float Karma
		{
			get
			{
				return sCurrentKarmaLevel;
			}
			set
			{
				sCurrentKarmaLevel = value;
				if (sCurrentKarmaLevel < 0f)
				{
					sCurrentKarmaLevel = 0f;
				}
				if (sCurrentKarmaLevel > 100f)
				{
					sCurrentKarmaLevel = 100f;
				}
				if (sCurrentKarmaLevel == 100f)
				{
					//EventTracker.SendEvent(EventTypeId.kChallengeKarmaReached100);
					throw new NotImplementedException();
				}
			}
		}

		private static bool DebugBadPowersOn
		{
			get
			{
				return sInstance.mDebugBadPowersOn;
			}
			set
			{
				sInstance.mDebugBadPowersOn = value;
			}
		}

		public static bool BadPowersOn
		{
			get
			{
				return mBadPowersOn;
			}
			set
			{
				mBadPowersOn = value;
			}
		}

		/*public static void SetKarmaWishModifierLevel(int nLevel)
		{
			sCurrentKarmaWishAmountModifier = kKarmaWishAmountModifierPerLevel * nLevel;
		}*/

		public static void OnOptionsLoaded()
		{
			LoadDialogFlagsFromProfile();
		}

		public static void OnOptionsReset()
		{
			bHaveShownFirstWishFulfillmentDialog = false;
			bHaveShownWitchingHourDialog = false;
			SaveDialogFlagsToProfile();
		}

		public WonderPowers()
		{
			//LoadDialogFlagsFromProfile();
		}

		private static void LoadDialogFlagsFromProfile()
		{
			throw new NotImplementedException();
			/*byte[] section = ProfileManager.GetSection((uint)ProfileManager.GetCurrentPrimaryPlayer(), 5u);
			if (section.Length == 2)
			{
				bHaveShownFirstWishFulfillmentDialog = section[0] > 0;
				bHaveShownWitchingHourDialog = section[1] > 0;
			}*/
		}

		private static void SaveDialogFlagsToProfile()
		{
			throw new NotImplementedException();
			/*try
			{
				byte[] array = new byte[2]
				{
					(byte)(bHaveShownFirstWishFulfillmentDialog ? 1 : 0),
					(byte)(bHaveShownWitchingHourDialog ? 1 : 0)
				};
				ProfileManager.UpdateSection((uint)ProfileManager.GetCurrentPrimaryPlayer(), 5u, array);
			}
			catch (Exception)
			{
			}*/
		}

		public override ScriptExecuteType Init(bool postLoad)
		{
			return ScriptExecuteType.Threaded;
		}

		public static bool IsInWitchingHour()
		{
			return smWitchingHourState != WitchingHourState.NONE;
		}

		public override void Simulate()
		{
			try
			{
				smWitchingHourState = WitchingHourState.NONE;
				mlastRunTime = SimClock.CurrentTime();
				DateAndTime a = mlastRunTime + new DateAndTime(SimClock.ConvertToTicks(1440f - (mlastRunTime.Hour * 60f) - 5f, TimeUnit.Minutes));
				while (!Sims3.SimIFace.Environment.HasEditInGameModeSwitch)
				{
					DateAndTime b = SimClock.CurrentTime();
					uint tickCount = 1u;
					switch (smWitchingHourState)
					{
						case WitchingHourState.NONE:
							if (a <= b)
							{
								smWitchingHourState = WitchingHourState.PRE_WITCHINGHOUR;
                                Gameflow.SetGameSpeed(Normal, Gameflow.SetGameSpeedContext.Gameplay);
								LotManager.SetAutoGameSpeed();
							}
							break;
						case WitchingHourState.PRE_WITCHINGHOUR:
							mWitchingHourPower = null;
							mWitchingVfx = VisualEffect.Create("wonderKarma_lot");
							if (mWitchingVfx != null)
							{
								//Vector3 floorPosition = WonderPowerActivation.GetFloorPosition(true, LotManager.ActiveLot);
								//mWitchingVfx.SetPosAndOrient(floorPosition, Vector3.UnitX, Vector3.UnitY);
								mWitchingVfx.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
							}
							mWitchingFloorVfx = VisualEffect.Create("wonderKarma_lotFloor");
							if (mWitchingFloorVfx != null)
							{
								//Vector3 floorPosition2 = WonderPowerActivation.GetFloorPosition(false, LotManager.ActiveLot);
								//mWitchingFloorVfx.SetPosAndOrient(floorPosition2, Vector3.UnitX, Vector3.UnitY);
								mWitchingFloorVfx.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
							}
							smWitchingHourState = WitchingHourState.WITCHINGHOUR;
							break;
						case WitchingHourState.WITCHINGHOUR:
							if ((DebugBadPowersOn && b.Hour != mlastRunTime.Hour) || b.DayOfWeek != mlastRunTime.DayOfWeek)
							{
								bool flag = false;
								/*if (!AnyPowersRunning() && BadPowersOn)
								{
									mWitchingHourPower = CheckTriggerBadPower();
									if (mWitchingHourPower != null)
									{
										flag = true;
									}
								}*/
								if (!flag)
								{
									float karma = Karma;
									float @float = RandomUtil.GetFloat(kKarmaDailyRationLow, kKarmaDailyRationHigh);
									if (Karma < 100f)
									{
										Karma += @float;
									}
									VisualEffect visualEffect = VisualEffect.Create("wonderkarma_lot_out");
									if (visualEffect != null)
									{
										//Vector3 floorPosition3 = WonderPowerActivation.GetFloorPosition(true, LotManager.ActiveLot);
										//visualEffect.SetPosAndOrient(floorPosition3, Vector3.UnitX, Vector3.UnitY);
										visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
									}
									/*KarmaDial.Load(karma, Karma, true);
									if (!bHaveShownWitchingHourDialog)
									{
										KarmaDial.WitchingHourCompletedFunction = (KarmaDial.WitchingHourCompleted)(object)new KarmaDial.WitchingHourCompleted(DisplayWitchingHourDialogPopup);
									}*/
								}
								else
								{
									VisualEffect visualEffect2 = VisualEffect.Create("wonderKarma_lotbad");
									if (visualEffect2 != null)
									{
										//Vector3 floorPosition4 = WonderPowerActivation.GetFloorPosition(true, LotManager.ActiveLot);
										//visualEffect2.SetPosAndOrient(floorPosition4, Vector3.UnitX, Vector3.UnitY);
										visualEffect2.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
									}
									VisualEffect visualEffect3 = VisualEffect.Create("wonderKarma_lotbad");
									if (visualEffect3 != null)
									{
										//Vector3 floorPosition5 = WonderPowerActivation.GetFloorPosition(false, LotManager.ActiveLot);
										//visualEffect3.SetPosAndOrient(floorPosition5, Vector3.UnitX, Vector3.UnitY);
										visualEffect3.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
									}
								}
								mlastRunTime = b;
								if (mWitchingVfx != null)
								{
									mWitchingVfx.Stop();
									mWitchingVfx.Dispose();
									mWitchingVfx = null;
								}
								if (mWitchingFloorVfx != null)
								{
									mWitchingFloorVfx.Stop();
									mWitchingFloorVfx.Dispose();
									mWitchingFloorVfx = null;
								}
								tickCount = (uint)SimClock.ConvertToTicks(5f, TimeUnit.Minutes);
								a = mlastRunTime + new DateAndTime(SimClock.ConvertToTicks(1440f - (mlastRunTime.Hour * 60f) - 5f, TimeUnit.Minutes));
								smWitchingHourState = WitchingHourState.POST_WITCHINGHOUR;
							}
							break;
						case WitchingHourState.POST_WITCHINGHOUR:
							if (mWitchingHourPower != null)
							{
								mWitchingHourPower.Run(true);
								mWitchingHourPower = null;
							}
							smWitchingHourState = WitchingHourState.NONE;
							LotManager.SetAutoGameSpeed();
							break;
					}
					Simulator.Sleep(tickCount);
				}
				Simulator.Sleep(uint.MaxValue);
			}
			catch (Exception)
			{
			}
		}

		internal static MethodInfo FindMethod(string methodName)
        {
			if (methodName.Contains(","))
			{
				string[] array = methodName.Split(new char[]
				{
			        ','
				});
				string typeName = array[0] + "," + array[1];
				Type type = Type.GetType(typeName, true);
				string text = array[2];
				text = text.Replace(" ", "");
				return type.GetMethod(text);
			}
			Type typeFromHandle = typeof(ActivationMethods);
			return typeFromHandle.GetMethod(methodName);
		}

		private void DisplayWitchingHourDialogPopup()
		{
			SimpleMessageDialog.Show(Localization.LocalizeString("UI/Wondermode/PopupDialog:HourOfReckoningTitle"), Localization.LocalizeString("UI/Wondermode/PopupDialog:HourOfReckoningText"));
			bHaveShownWitchingHourDialog = true;
			SaveDialogFlagsToProfile();
		}

		public static void OnPromiseFulfilled(IDreamAndPromise dream)
		{
			if (sInstance != null)
			{
				Simulator.AddObject(new Sims3.UI.OneShotFunctionTask(delegate ()
				{
					sInstance.OnShowKarmaStar(dream);
				}));
				sInstance.TotalPromisesFulfilled++;
				float num = 0;// sCurrentKarmaWishAmountModifier;
				float num2 = kKarmaBasicWishAmount;
				if (dream is ActiveDreamNode activeDreamNode && activeDreamNode.Owner != null && activeDreamNode.IsMajorWish)
				{
					num2 = kKarmaLifetimeWishAmount;
				}
				sInstance.TotalPromisesFulfilledKarma += num2 + num;
			}
		}

		public void OnShowKarmaStar(object d)
		{
			if (KarmaPromisesFulfilled == 0)
			{
				ShowKarmaDial();
			}
			IDreamAndPromise dreamAndPromise = d as IDreamAndPromise;
			if (dreamAndPromise is ActiveDreamNode activeDreamNode && activeDreamNode.Owner != null)
			{
				VisualEffect.FireOneShotEffect("wonderkarma_gain", activeDreamNode.Owner, Sim.FXJoints.HatGrip, VisualEffect.TransitionType.SoftTransition);
			}
			KarmaPromisesFulfilled++;
			if (KarmaPromisesFulfilled == sInstance.TotalPromisesFulfilled)
			{
				TotalPromisesFulfilledKarma = 0f;
				KarmaPromisesFulfilled = 0;
				TotalPromisesFulfilled = 0;
				/*while (KarmaDial.IsVisible)
				{
					Simulator.Sleep(0u);
				}*/
			}
		}

		public void ShowKarmaDial()
		{
			float karma = GetKarma();
			float num = karma + TotalPromisesFulfilledKarma;
			SetKarma((int)num);
			/*KarmaDial.Load(karma, Karma, false);
			if (!bHaveShownFirstWishFulfillmentDialog && sInstance != null)
			{
				KarmaDial.WishFulfilledCompletedFunction = (KarmaDial.WishFulfilledCompleted)(object)new KarmaDial.WishFulfilledCompleted(sInstance.DisplayWishFulfilledDialogPopup);
			}*/
		}

		private void DisplayWishFulfilledDialogPopup()
		{
			SimpleMessageDialog.Show(Localization.LocalizeString("UI/Wondermode/PopupDialog:FirstWishFulfillmentTitle"), Localization.LocalizeString("UI/Wondermode/PopupDialog:FirstWishFulfillmentText"));
			bHaveShownFirstWishFulfillmentDialog = true;
			SaveDialogFlagsToProfile();
		}

		public void UsedPower(WonderPower power)
		{
			float num = power.Cost();
			if (num > 0f)
			{
				Karma -= num;
			}
			sCurrentBadKarmaChance += kKarmaBadEventIncreaseConstant;
			//power.WasUsed = true;
		}

		public void CancelledPower(WonderPower power)
		{
			float num = power.Cost();
			if (num > 0f)
			{
				Karma += num;
				sCurrentBadKarmaChance -= kKarmaBadEventIncreaseConstant;
			}
		}

		/*private WonderPower CheckTriggerBadPower()
		{
			float @float = RandomUtil.GetFloat(100f);
			if (@float < sCurrentBadKarmaChance || sCurrentBadKarmaChance >= 100f || DebugBadPowersOn)
			{
				int num = 0;
				foreach (WonderPower mAllWonderPower in mAllWonderPowers)
				{
					if (mAllWonderPower.IsBadPower)
					{
						num += mAllWonderPower.ChanceToSpawnAsBadPower;
					}
				}
				if (num > 0)
				{
					int num2 = RandomUtil.GetInt(num - 1);
					foreach (WonderPower mAllWonderPower2 in mAllWonderPowers)
					{
						if (mAllWonderPower2.IsBadPower)
						{
							num2 -= mAllWonderPower2.ChanceToSpawnAsBadPower;
							if (num2 < 0)
							{
								sCurrentBadKarmaChance = 0f;
								return mAllWonderPower2;
							}
						}
					}
				}
			}
			return null;
		}*/

		private void AddWonderPower(WonderPower s)
		{
			int i = mAllWonderPowers.IndexOf(s);
			if (i >= 0)
			{
				s.AssignTo(mAllWonderPowers[i]);
			}
			else
			{
				mAllWonderPowers.Add(s);
			}
		}

        private void RemoveWonderPower(WonderPower s) => mAllWonderPowers.Remove(s);

        internal static void PreWorldLoadStartup()
		{
			if (sInstance is null)
			{
				sInstance = new WonderPowers();
				//Simulator.AddObject(sInstance);
			}
			if (Gameflow.sGameLoadedFromWorldFile)
			{
				sCurrentKarmaLevel = kInitialKarmaLevel;
				sCurrentBadKarmaChance = 0f;
			}
		}
		
		internal static void WorldLoadShutdown()
		{
			/*foreach (WonderPowerActivation mActiveWonderPower in sInstance.mActiveWonderPowers)
			{
				mActiveWonderPower.CleanupAfterPower();
			}*/
			sInstance.mAllWonderPowers.Clear();
			sCurrentKarmaLevel = kInitialKarmaLevel;
			sCurrentBadKarmaChance = 0f;
			sInstance.Destroy();
			sInstance = null;
		}

        public static void Add(WonderPower s) => sInstance.AddWonderPower(s);

        public static void Remove(WonderPower s) => sInstance.RemoveWonderPower(s);

		public static bool HasEnoughKarma(int karma) => sInstance != null && sInstance.Karma - karma >= 0f;

		public static float GetKarma() => sInstance.Karma;

		public static void SetKarma(int karma)
		{
			if (sInstance != null)
			{
				sInstance.Karma = karma;
			}
		}

		public static void OnPowerUsed(WonderPower power)
		{
			if (sInstance != null)
			{
				sInstance.UsedPower(power);
			}
		}

		public static void OnPowerCancelled(WonderPower power)
		{
			if (sInstance != null)
			{
				sInstance.CancelledPower(power);
			}
		}

		public static WonderPower GetByName(string name)
		{
			foreach (WonderPower mAllWonderPower in sInstance.mAllWonderPowers)
			{
				if (name.Equals(mAllWonderPower?.WonderPowerName, StringComparison.InvariantCultureIgnoreCase))
				{
					return mAllWonderPower;
				}
			}
			return null;
		}

        public static List<WonderPower> GetWonderPowerList() => sInstance.mAllWonderPowers;

        /*public static void BadPowersDebug(bool activate)
		{
			if (activate)
			{
				if (BadPowersOn)
				{
					DebugBadPowersOn = true;
					sInstance.mlastRunTime = SimClock.CurrentTime();
				}
			}
			else
			{
				DebugBadPowersOn = false;
				if (sInstance != null)
				{
					sCurrentBadKarmaChance = 0f;
				}
			}
		}*/

        internal static string LocalizeString(string name, params object[] parameters) => Localization.LocalizeString("Gameplay/WonderMode:" + name, parameters);

        /*public static void AddActivePower(WonderPowerActivation activePower)
		{
			sInstance.mActiveWonderPowers.Add(activePower);
		}

		public static void RemoveActivePower(WonderPowerActivation activePower)
		{
			sInstance.mActiveWonderPowers.Remove(activePower);
		}

		public static bool AnyPowersRunning()
		{
			return sInstance.mActiveWonderPowers.Count > 0;
		}*/
    }

	/*[Persistable(false)]
	public abstract class WonderPowerActivation : ScriptObject
	{
		protected enum SelectionType
		{
			SELECT_SIM,
			SELECT_OBJECT,
			SELECT_POINT
		}

		private delegate void SelectionCallback(UITriggerEventArgs eventArgs);

		public const string sWonderPowerReason = "Gameplay/WonderMode:PowerIsDeploying";

		private const float kAfterglowYOff = 0.5f;

		private const float kTickTockYOff = 0.5f;

		private static readonly List<Sim> mAffectedSimList = new List<Sim>();

		protected ActivationType mHowActivated;

		private ObjectGuid mScriptHandle = ObjectGuid.InvalidObjectGuid;

		private readonly WonderPower mWonderPowerType;

		private static WonderPowerActivation mCursorSelector;

		protected VisualEffect mPowerCursorEffect;

		protected VisualEffect mPowerCursorActiveEffect;

		protected VisualEffect mPowerCursorValidEffect;

		protected VisualEffect mPowerCursorInvalidEffect;

		protected VisualEffect mPowerCursorDeployEffect;

		protected CASAgeGenderFlags mSimAgeFilter;

		protected CASAgeGenderFlags mSimGenderFilter;

		protected bool mDeadSimTargetable;

		private bool mTargetSelected;

		private bool mTickTockGood;

		private bool mPlayedShazam;

		private Sim mTickTockSim;

		protected object mSelectedObject;

		private SelectionCallback mSelectionCallback;

		private Sim.SwitchOutfitHelper mSoh;

		private bool mOutfitReady;

		private Sim mSingeSim;

		private VisualEffect mTickTockVfx;

		private BuffNames mAffectSimsBuff;

		private Origin mAffectSimsBuffOrigin;

		private InteractionInstance mDisableInteraction;

		private static readonly List<object> mNearbyGoodReactions = InitNearbySimGoodReactions();

		private static readonly List<object> mNearbyBadReactions = InitNearbySimBadReactions();

		public static WonderPowerActivation CursorSelector
		{
			get
			{
				return mCursorSelector;
			}
		}

		public WonderPower PowerType
		{
			get
			{
				return mWonderPowerType;
			}
		}

		public bool PlayedShazamAnim()
		{
			return mPlayedShazam;
		}

		public WonderPowerActivation(WonderPower wonderPowerType)
		{
			mWonderPowerType = wonderPowerType;
		}

		public override ScriptExecuteType Init(bool postLoad)
		{
			return ScriptExecuteType.Task;
		}

		public override void Dispose()
		{
			mAffectedSimList.Clear();
			base.Dispose();
		}

		public virtual void Activate(ActivationType howActivated, GameObject target)
		{
			if (GameStates.IsInWorld() && GameStates.GetInWorldSubState() is LiveModeState)
			{
				if (!WonderPowers.AnyPowersRunning() && mWonderPowerType != null) //&& (!mWonderPowerType.IsLocked || howActivated != 0))
				{
					WonderPowers.AddActivePower(this);
					mHowActivated = howActivated;
					mSelectedObject = target;
					mScriptHandle = Simulator.AddObject(this);
				}
			}
		}

		protected bool DisableInterruptions(Sim actor)
		{
			bool flag = true;
			if (actor != null)
			{
				DisableInterruptionsNoWait(actor);
				flag = WaitForStandup(actor);
				if (flag)
				{
					PlayTickTockEffect(actor, mTickTockGood);
					RouteToSafeArea(actor);
				}
				KillTickTockEffect();
			}
			return flag;
		}

		protected bool DisableInterruptionsNoRoute(Sim actor)
		{
			bool result = true;
			if (actor != null)
			{
				DisableInterruptionsNoWait(actor);
				result = WaitForStandup(actor);
				KillTickTockEffect();
			}
			return result;
		}

		private void DisableInteractionFailedCallback(Sim _, float __)
		{
			mDisableInteraction.Cancelled = true;
		}

		protected void DisableInterruptionsNoWait(Sim actor)
		{
			if (actor != null && actor.InteractionQueue != null)
			{
				actor.InteractionQueue.CancelPosture(actor);
				actor.InteractionQueue.CancelAllInteractions();
				mDisableInteraction = WonderPowerStandIdle.GetDefinition(PowerType.WonderPowerName).CreateInstanceWithCallbacks(actor, actor, new InteractionPriority(InteractionPriorityLevel.MaxDeath), false, false, null, null, DisableInteractionFailedCallback);
				actor.InteractionQueue.AddNext(mDisableInteraction);
			}
		}

		protected void RouteToSafeArea(Sim actor)
		{
			if (!(GlobalFunctions.CreateObjectOutOfWorld("SimSittingOnFloorJig") is SocialJig socialJig))
			{
				return;
			}
			socialJig.RegisterParticipants(actor, null);
			socialJig.SetPosition(actor.Position);
			socialJig.SetForward(actor.ForwardVector);
			Vector3 position = actor.Position;
			Vector3 forward = actor.ForwardVector;
			if (GlobalFunctions.FindGoodLocationNearby(socialJig, ref position, ref forward, true))
			{
				Route route = actor.CreateRoute();
				PointDestination pointDestination = new PointDestination
				{
					mPoint = position
				};
				position.y = CameraController.GetPosition().y;
				pointDestination.mFacingDirection = (CameraController.GetPosition() - position).Normalize();
				route.AddDestination(pointDestination);
				route.AddObjectToIgnoreForRoute(socialJig.ObjectId);
				route.Plan();
				if (route.PlanResult.Succeeded())
				{
					route.ExecutionFromNonSimTaskIsSafe = true;
					actor.DoRoute(route);
				}
			}
			socialJig.Destroy();
		}

		protected bool WaitForStandup(Sim actor)
		{
			if (actor != null)
			{
				while (mDisableInteraction != null && actor.InteractionQueue != null && !actor.InteractionQueue.IsRunning(mDisableInteraction, false))
				{
					Simulator.Sleep(0u);
					if (mDisableInteraction.Cancelled)
					{
						EnableInterruptions(actor);
						return false;
					}
				}
				while (!actor.IsStandingIdle && actor.IdleManager != null)
				{
					Simulator.Sleep(0u);
				}
			}
			return true;
		}

		protected void EnableInterruptions(Sim actor)
		{
			if (actor != null && actor.InteractionQueue != null)
			{
				if (actor.InteractionQueue.GetHeadInteraction() == mDisableInteraction)
				{
					actor.AddExitReason(ExitReason.Finished);
					actor.AddExitReason(ExitReason.CanceledByScript);
				}
				actor.InteractionQueue.CancelAllInteractionsByType(WonderPowerStandIdle.GetDefinition(PowerType.WonderPowerName));
				mDisableInteraction = null;
			}
			//NotificationManager.Instance.ShowUI(true);
		}

		protected void DisableBalloons(Sim actor)
		{
			if (actor != null)
			{
				actor.SocialComponent.LeaveConversation();
				actor.ThoughtBalloonManager.Dispose();
				//actor.ThoughtBalloonManager.RemovePendingBallons();
				//actor.ThoughtBalloonManager.BalloonLockOut = true;
			}
		}

		protected void EnableBalloons(Sim actor)
		{
			if (actor != null)
			{
				//actor.ThoughtBalloonManager.BalloonLockOut = false;
			}
		}

		public static void IncreaseFearLevel(Sim actor)
		{
			throw new NotImplementedException();
			/*if ((actor.SimDescription.Age & CASAgeGenderFlags.Baby) == 0 && (actor.SimDescription.Age & CASAgeGenderFlags.Toddler) == 0 && !actor.BuffManager.HasElement(BuffNames.WonderTraumatized))
			{
				if (actor.BuffManager.HasElement(BuffNames.WonderTerrified))
				{
					InteractionPriority priority = new InteractionPriority(actor.InheritedPriority().Level, actor.InheritedPriority().Value + 1f);
					actor.InteractionQueue.AddNext(TraitFunctions.CowardTraitFaint.Singleton.CreateInstance(actor, actor, priority, false, false));
					actor.BuffManager.AddElement(BuffNames.WonderTraumatized, Origin.None);
					actor.BuffManager.RemoveElement(BuffNames.WonderTerrified);
					ActiveTopic.AddToSim(actor, "Buff Wonder Scared");
				}
				else if (actor.BuffManager.HasElement(BuffNames.WonderPanicked))
				{
					actor.Motives.SetValue(CommodityKind.Bladder, -100f);
					actor.BuffManager.AddElement(BuffNames.WonderTerrified, Origin.None);
					actor.BuffManager.RemoveElement(BuffNames.WonderPanicked);
					ActiveTopic.AddToSim(actor, "Buff Wonder Scared");
				}
				else if (actor.BuffManager.HasElement(BuffNames.WonderScared))
				{
					actor.ThoughtBalloonManager.ShowBalloonByKey(ThoughtBalloonTypes.kThoughtBalloon, ThoughtBalloonPriority.High, ThoughtBalloonDuration.Medium, ThoughtBalloonCooldown.Short, ThoughtBalloonAxis.kNeutral, "BuffScared");
					actor.BuffManager.AddElement(BuffNames.WonderPanicked, Origin.None);
					actor.BuffManager.RemoveElement(BuffNames.WonderScared);
					ActiveTopic.AddToSim(actor, "Buff Wonder Scared");
				}
				else if (actor.BuffManager.HasElement(BuffNames.WonderInsecure))
				{
					actor.BuffManager.AddElement(BuffNames.WonderScared, Origin.None);
					actor.BuffManager.RemoveElement(BuffNames.WonderInsecure);
					ActiveTopic.AddToSim(actor, "Buff Wonder Scared");
				}
				else if (actor.HasTrait(TraitNames.Brave) || actor.HasTrait(TraitNames.Childish) || actor.HasTrait(TraitNames.Daredevil) || actor.HasTrait(TraitNames.Insane) || actor.HasTrait(TraitNames.PartyAnimal))
				{
					actor.BuffManager.AddElement(BuffNames.WonderInsecure, Origin.None);
				}
				else if (actor.HasTrait(TraitNames.Coward) || actor.HasTrait(TraitNames.Neurotic))
				{
					actor.BuffManager.AddElement(BuffNames.WonderPanicked, Origin.None);
					ActiveTopic.AddToSim(actor, "Buff Wonder Scared");
				}
				else
				{
					actor.BuffManager.AddElement(BuffNames.WonderScared, Origin.None);
					ActiveTopic.AddToSim(actor, "Buff Wonder Scared");
				}
			}*
		}

		public static List<object> InitNearbySimGoodReactions()
		{
			List<object> list = new List<object>
			{
				ReactionTypes.LaughAt,
				ReactionTypes.Excited,
				ReactionTypes.Cheer,
				ReactionTypes.Surprise
			};
			return list;
		}

		public static List<object> InitNearbySimBadReactions()
		{
			List<object> list = new List<object>
			{
				ReactionTypes.Scared,
				ReactionTypes.Shocked,
				ReactionTypes.Surprise,
				ReactionTypes.Startled
			};
			return list;
		}

		public static ReactionTypes GetRandomNearbySimReaction(bool isGood)
		{
			return isGood
				? (ReactionTypes)RandomUtil.GetRandomObjectFromList(mNearbyGoodReactions)
				: (ReactionTypes)RandomUtil.GetRandomObjectFromList(mNearbyBadReactions);
		}

		protected void AffectNearbySim(Sim actor, GameObject targetObject, ReactionSpeed speed, bool isGood, bool applyMoodlet, bool clearQueue)
		{
			if (actor != null)
			{
				if (clearQueue)
				{
					actor.InteractionQueue.CancelAllInteractions();
				}
				ReactionTypes reaction = GetRandomNearbySimReaction(isGood);
				if (targetObject is Sim)
				{
					Sim targetSim = targetObject as Sim;
					ReactionInteraction.ReactionByTrait(actor, targetSim, !isGood, ref reaction);
				}
				actor.PlayReaction(reaction, new InteractionPriority(InteractionPriorityLevel.CriticalNPCBehavior), targetObject, string.Empty, ResourceKey.kInvalidResourceKey, ThoughtBalloonAxis.kNeutral, speed, null, null, false, 12f);
				if (applyMoodlet)
				{
					actor.BuffManager.AddElement(mAffectSimsBuff, mAffectSimsBuffOrigin);
				}
				bool flag = false;
				foreach (Sim mAffectedSim in mAffectedSimList)
				{
					if (mAffectedSim == actor)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					mAffectedSimList.Add(actor);
				}
			}
		}

		protected void DeployAffectNearbySims(Sim actor, bool isGood)
		{
			for (int i = 0; i < mAffectedSimList.Count; i++)
			{
				Sim sim = mAffectedSimList[i];
				if (sim != null && sim.InteractionQueue != null)
				{
					sim.InteractionQueue.CancelAllInteractions();
				}
			}
			for (int j = 0; j < 10; j++)
			{
				AffectNearbySims(actor, ReactionSpeed.AfterInteraction, isGood, false, false);
			}
		}

		protected void ShazamAffectNearbySims(Sim actor, bool isGood)
		{
			AffectNearbySims(actor, ReactionSpeed.Immediate, isGood, false, true);
			DeployAffectNearbySims(actor, isGood);
		}

		protected void AffectAnyNearbySims(GameObject actor, BuffNames moodlet, Origin moodletOrigin, bool isGood)
		{
			mAffectSimsBuff = moodlet;
			mAffectSimsBuffOrigin = moodletOrigin;
			AffectAnyNearbySims(actor, ReactionSpeed.Immediate, isGood, true, true, WonderPowers.NearbySimsDistance);
		}

		protected void AffectAnyNearbySims(GameObject actor, BuffNames moodlet, Origin moodletOrigin, ReactionSpeed speed, bool isGood)
		{
			mAffectSimsBuff = moodlet;
			mAffectSimsBuffOrigin = moodletOrigin;
			AffectAnyNearbySims(actor, speed, isGood, true, true, WonderPowers.NearbySimsDistance);
		}

		protected void AffectAnyNearbySims(GameObject actor, BuffNames moodlet, Origin moodletOrigin, bool isGood, float reactionDistance)
		{
			mAffectSimsBuff = moodlet;
			mAffectSimsBuffOrigin = moodletOrigin;
			AffectAnyNearbySims(actor, ReactionSpeed.Immediate, isGood, true, true, reactionDistance);
		}

		protected void AffectAnyNearbySims(GameObject actor, BuffNames moodlet, Origin moodletOrigin, ReactionSpeed speed, bool isGood, float reactionDistance)
		{
			mAffectSimsBuff = moodlet;
			mAffectSimsBuffOrigin = moodletOrigin;
			AffectAnyNearbySims(actor, speed, isGood, true, true, reactionDistance);
		}

		protected void AffectAnyNearbySims(GameObject actor, ReactionSpeed speed, bool isGood, bool applyMoodlet, bool clearQueue, float reactionDistance)
		{
			if (actor == null)
			{
				return;
			}
			Sim[] objects = Sims3.Gameplay.Queries.GetObjects<Sim>(actor.Position, reactionDistance);
			foreach (Sim sim in objects)
			{
				if (sim != actor)
				{
					AffectNearbySim(sim, actor, speed, isGood, applyMoodlet, clearQueue);
				}
			}
		}

		protected void AffectNearbySims(GameObject actor, BuffNames moodlet, Origin moodletOrigin, bool isGood)
		{
			mAffectSimsBuff = moodlet;
			mAffectSimsBuffOrigin = moodletOrigin;
			AffectNearbySims(actor, ReactionSpeed.Immediate, isGood, true, true);
		}

		protected void AffectNearbySims(GameObject actor, BuffNames moodlet, Origin moodletOrigin, ReactionSpeed speed, bool isGood)
		{
			mAffectSimsBuff = moodlet;
			mAffectSimsBuffOrigin = moodletOrigin;
			AffectNearbySims(actor, speed, isGood, true, true);
		}

		protected void AffectNearbySims(GameObject actor, ReactionSpeed speed, bool isGood, bool applyMoodlet, bool clearQueue)
		{
			if (actor == null)
			{
				return;
			}
			if (clearQueue)
			{
				for (int i = 0; i < mAffectedSimList.Count; i++)
				{
					Sim sim = mAffectedSimList[i];
					if (sim != null && sim.InteractionQueue != null)
					{
						sim.InteractionQueue.CancelAllInteractions();
					}
				}
			}
			if (actor.IsOutside)
			{
				float nearbySimsDistance = WonderPowers.NearbySimsDistance;
				Sim[] objects = Sims3.Gameplay.Queries.GetObjects<Sim>(actor.Position, nearbySimsDistance);
				foreach (Sim sim2 in objects)
				{
					if (sim2 != actor && sim2.IsOutside)
					{
						AffectNearbySim(sim2, actor, speed, isGood, applyMoodlet, clearQueue);
					}
				}
				return;
			}
			List<Sim> sims = actor.LotCurrent.GetAllActors();
			for (int k = 0; k < sims.Count; k++)
			{
				Sim sim3 = sims[k];
				if (actor != sim3 && actor.RoomId == sim3.RoomId)
				{
					AffectNearbySim(sim3, actor, speed, isGood, applyMoodlet, clearQueue);
				}
			}
		}

		protected void PlayShazamAnim(Sim actor, bool isGood)
		{
			PlayShazamScreenEffect(isGood);
			mTickTockGood = isGood;
			mPlayedShazam = false;
			if (actor == null)
			{
				return;
			}
			if (actor.IsStandingIdle && actor.CurrentInteraction == null && actor.InteractionQueue.RunningInteraction == null && actor.InteractionQueue.TransitionInteraction == null)
			{
				StateMachineClient stateMachineClient = (!isGood) ? StateMachineClient.Acquire(actor.Proxy.ObjectId, "Wonderpower_Shazam_Negative", AnimationPriority.kAPLookAt, false) : StateMachineClient.Acquire(actor.Proxy.ObjectId, "Wonderpower_Shazam_Positive", AnimationPriority.kAPLookAt, false);
				if (stateMachineClient != null)
				{
					stateMachineClient.FlushEventQueue();
					stateMachineClient.SetActor("x", actor);
					stateMachineClient.EnterState("x", "Enter");
					stateMachineClient.RequestState(false, "x", "Exit");
					ShazamAffectNearbySims(actor, isGood);
					mPlayedShazam = true;
				}
			}
			else
			{
				PlayTickTockEffect(actor, isGood);
			}
		}

		protected void PlayTickTockEffect(Sim actor, bool isGood)
		{
			if (actor != null && mTickTockVfx == null)
			{
				mTickTockVfx = isGood ? VisualEffect.Create("wonderTicktockGood") : VisualEffect.Create("wonderTicktockBad");
				if (mTickTockVfx != null)
				{
					Vector3 trans = Vector3.UnitY * kTickTockYOff;
					mTickTockVfx.ParentTo(actor, Sim.ContainmentSlots.RightCarry, ref trans);
					mTickTockVfx.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
					mTickTockSim = actor;
				}
			}
		}

		protected void KillTickTockEffect()
		{
			if (mTickTockVfx != null)
			{
				Audio.StopGroup(393239870u, true);
				mTickTockVfx.Stop();
				mTickTockVfx.Dispose();
				mTickTockVfx = null;
			}
			if (mTickTockSim != null && mTickTockGood)
			{
				VisualEffect visualEffect = VisualEffect.Create("wonderTicktockGood_out");
				if (visualEffect != null)
				{
					visualEffect.ParentTo(mTickTockSim, Sim.ContainmentSlots.RightCarry);
					visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
				}
				mTickTockSim = null;
			}
		}

		protected void PlayAfterglowAnim(Sim actor, bool isGood)
		{
			if (actor != null)
			{
				VisualEffect visualEffect = (!isGood) ? VisualEffect.Create("wonderAfterglowBad") : VisualEffect.Create("wonderAfterglowGood");
				if (visualEffect != null)
				{
					Vector3 trans = Vector3.UnitY * kAfterglowYOff;
					visualEffect.ParentTo(actor, Sim.ContainmentSlots.RightCarry, ref trans);
					visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
				}
			}
		}

		protected void PlayShazamScreenEffect(bool isGood)
		{
			VisualEffect visualEffect = !isGood ? VisualEffect.Create("wonderShazamScreenBad") : VisualEffect.Create("wonderShazamScreenGood");
			if (visualEffect != null)
			{
				visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
			}
		}

		private void PrepSingedSwap()
		{
			BuffSinged.SetupSingedOutfit(mSingeSim);
			mSoh = new Sim.SwitchOutfitHelper(mSingeSim, OutfitCategories.Singed, 0);
			if (mSoh != null)
			{
				mSoh.Start();
				mSoh.Wait(false);
				mOutfitReady = true;
			}
		}

		protected void SingeSim()
		{
			while (!mOutfitReady)
			{
				Simulator.Sleep(1u);
			}
			mSingeSim.SwitchToOutfitWithoutSpin(OutfitCategories.Singed);
		}

		protected void SingeSimSetup(Sim actor)
		{
			mSingeSim = actor;
			mOutfitReady = false;
			OneShotFunction obj = new OneShotFunction(PrepSingedSwap);
			Simulator.AddObject(obj);
		}

		protected void UnSingeSim(Sim actor)
		{
			if (actor.CurrentOutfitCategory == OutfitCategories.Singed)
			{
				actor.SwitchToOutfitWithoutSpin(OutfitCategories.Everyday);
			}
		}

		protected virtual void PlayJingle()
		{
		}

		public override void Simulate()
		{
			DisableSave();
			try
			{
				if (CanRun())
				{
					bool flag = SelectTargets();
					if (flag)
					{
						if (PowerWillAffectAnything())
						{
							UpdateKarma();
						}
						Sims3.Gameplay.Gameflow.UnlockGameSpeed();
						Sims3.Gameplay.Gameflow.SetGameSpeed(Sims3.SimIFace.Gameflow.GameSpeed.Normal, Sims3.Gameplay.Gameflow.SetGameSpeedContext.Gameplay);
						Run();
					}
				}
			}
			catch (Exception)
			{
			}
			CleanupAfterPower();
		}

		public void CleanupAfterPower()
		{
			EnableSave();
			if (mSoh != null)
			{
				mSoh.Abort();
				mSoh = null;
			}
			WonderPowers.RemoveActivePower(this);
			Simulator.DestroyObject(mScriptHandle);
		}

		protected virtual bool CanRun()
		{
			return mHowActivated == ActivationType.KarmaTrigger || WonderPowers.HasEnoughKarma(mWonderPowerType.Cost());
		}

		protected abstract bool SelectTargets();

		protected abstract void Run();

		protected virtual void UpdateKarma()
		{
			if (mHowActivated == ActivationType.UserSelected)
			{
				WonderPowers.OnPowerUsed(mWonderPowerType);
			}
		}

		private void OnDisambiguateTargetSelected(MenuItem item)
		{
			mTargetSelected = true;
			if (item != null && item.mTag is ScenePickArgs scenePickArgs)
			{
				ObjectGuid objID = new ObjectGuid(scenePickArgs.mObjectId);
				mSelectedObject = GameObject.GetObject(objID);
			}
		}

		protected virtual bool DisambiguateTarget(int x, int y, Vector3 pos, UITriggerEventArgs eventArgs, ref ulong[] objectIds, ref uint[] objectTypes)
		{
			//TODO Fix
			if (objectIds.Length == 0)
			{
				return false;
			}
			if (objectIds.Length == 1)
			{
				ObjectGuid objID = new ObjectGuid(objectIds[0]);
				Sim sim = GameObject.GetObject(objID) as Sim;
				mTargetSelected = true;
				mSelectedObject = sim;
				return false;
			}
			ScenePickArgs pickArgs = default;
			pickArgs.mX = x;
			pickArgs.mY = y;
			pickArgs.mObjectId = 0uL;
			pickArgs.mObjectType = ScenePickObjectType.None;
			pickArgs.mWorldPos = pos;
			pickArgs.mMouseEvent = new UIMouseEventArgs();
			pickArgs.mMouseEvent.Init(6u, eventArgs.SourceWindow, eventArgs.DestinationWindow, 0, 0, 0, x, y, 0f, 0f, false, null);
			//TestAndBringUpPieMenu(pickArgs, objectIds, objectTypes, false, false, OnDisambiguateTargetSelected, OnMenuItemHighlight);
			return true;
		}

		/*protected virtual void SelectSimCallback(UITriggerEventArgs eventArgs)
		{
			int x = 0;
			int y = 0;
			ulong[] objectIds = null;
			uint[] objectTypes = null;
			Vector3 zero = Vector3.Zero;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			CameraController.GetInGameCursorPickInfo(ref x, ref y, ref objectIds, ref objectTypes, ref zero, ref flag, ref flag2, ref flag3, ref flag4);
			if (FilterForSim(ref objectIds, ref objectTypes, mSimAgeFilter, mSimGenderFilter) && !DisambiguateTarget(x, y, zero, eventArgs, ref objectIds, ref objectTypes))
			{
				if (objectIds.Length > 0)
				{
					ObjectGuid objID = new ObjectGuid(objectIds[0]);
					Sim sim = (Sim)(mSelectedObject = (GameObject.GetObject(objID) as Sim));
				}
				mTargetSelected = true;
			}
		}*

		private void UpdateUiText(bool validSelection, ulong[] objectIds)
		{
			if (validSelection && objectIds.Length > 0)
			{
				if (objectIds.Length > 1)
				{
					//KarmaPrompt.SetMultiple();
					return;
				}
				ObjectGuid objID = new ObjectGuid(objectIds[0]);
				Sim sim = GameObject.GetObject(objID) as Sim;
				//KarmaPrompt.SetSingle(Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:FirstLastName", sim.FirstName, sim.LastName));
			}
			else
			{
				//KarmaPrompt.RestoreTip();
			}
		}

		protected Sim SelectSim(CASAgeGenderFlags age, CASAgeGenderFlags gender)
		{
			throw new NotImplementedException();
			//TODO Rewrite
			/*mDeadSimTargetable = false;
			mCursorSelector = this;
			mSimAgeFilter = age;
			mSimGenderFilter = gender;
			mTargetSelected = false;
			mSelectedObject = null;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			mSelectionCallback = SelectSimCallback;
			if (mHowActivated == ActivationType.UserSelected)
			{
				while (!mTargetSelected)
				{
					int num = 0;
					int num2 = 0;
					ulong[] objectIds = null;
					uint[] objectTypes = null;
					Vector3 zero = Vector3.Zero;
					CameraController.GetInGameCursorPickInfo(ref num, ref num2, ref objectIds, ref objectTypes, ref zero, ref flag, ref flag2, ref flag3, ref flag4);
					bool flag5 = FilterForSim(ref objectIds, ref objectTypes, mSimAgeFilter, mSimGenderFilter);
					UpdateCursorState(flag5);
					UpdateUiText(flag5, objectIds);
					Simulator.Sleep(0u);
				}
			}
			KarmaPrompt.FadeOut();
			mCursorSelector = null;
			Sim sim = mSelectedObject as Sim;
			if (sim != null)
			{
				sim.InteractionQueue.CancelAllInteractions();
				if (sim.IsLeavingLot && sim.RoutingComponent != null)
				{
					sim.RoutingComponent.KillRoute(false);
				}
			}
			return sim;*
		}

		/*private void SelectObjectCallback(UITriggerEventArgs eventArgs)
		{
			int x = 0;
			int y = 0;
			ulong[] objectIds = null;
			uint[] objectTypes = null;
			Vector3 zero = Vector3.Zero;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			CameraController.GetInGameCursorPickInfo(ref x, ref y, ref objectIds, ref objectTypes, ref zero, ref flag, ref flag2, ref flag3, ref flag4);
			if (FilterForObject(ref objectIds, ref objectTypes) && !DisambiguateTarget(x, y, zero, eventArgs, ref objectIds, ref objectTypes))
			{
				if (objectIds.Length > 0)
				{
					ObjectGuid objID = new ObjectGuid(objectIds[0]);
					mSelectedObject = GameObject.GetObject(objID);
				}
				mTargetSelected = true;
			}
		}*

		protected GameObject SelectObject()
		{
			//TODO Rewrite
			throw new NotImplementedException();
			/*mCursorSelector = this;
			mTargetSelected = false;
			mSelectedObject = null;
			mSelectionCallback = SelectObjectCallback;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			if (mHowActivated == ActivationType.UserSelected)
			{
				while (!mTargetSelected)
				{
					int num = 0;
					int num2 = 0;
					ulong[] objectIds = null;
					uint[] objectTypes = null;
					Vector3 zero = Vector3.Zero;
					CameraController.GetInGameCursorPickInfo(ref num, ref num2, ref objectIds, ref objectTypes, ref zero, ref flag, ref flag2, ref flag3, ref flag4);
					UpdateCursorState(FilterForObject(ref objectIds, ref objectTypes));
					Simulator.Sleep(0u);
				}
			}
			mCursorSelector = null;
			return mSelectedObject as GameObject;*
		}

		/*protected virtual Sim SelectActiveSim(CASAgeGenderFlags age, CASAgeGenderFlags gender)
		{
			//CONSIDER Is this necessary?
			if (!(mSelectedObject is Sim sim))
			{
				sim = Sim.ActiveActor;
			}
			if (sim == null || !IsSimTargetableByWonderPower(sim) || (sim.SimDescription.Age & age) == 0 || (sim.SimDescription.Gender & gender) == 0)
			{
				sim = null;
			}
			return sim;
		}*/

		/*protected virtual Lot GetCurrentLot()
		{
			List<Lot> list = new List<Lot>();
			if (LotManager.ActiveLot != null && LotManager.ActiveLot.IsLotDataLoaded)
			{
				list.Add(LotManager.ActiveLot);
			}
			if (Sim.ActiveActor != null && Sim.ActiveActor.LotCurrent != null && Sim.ActiveActor.LotCurrent.IsLotDataLoaded)
			{
				list.Add(Sim.ActiveActor.LotCurrent);
			}
			foreach (Lot allLot in LotManager.AllLots)
			{
				if (allLot != null && allLot.IsLotDataLoaded && !allLot.IsWorldLot)
				{
					list.Add(allLot);
				}
			}
			Vector3 cameraTargetAsVec = CameraController.GetLODInterestPosition();
			return Camera.GetClosestLotFromList(list, ref cameraTargetAsVec);
		}

		protected static List<Sim> GetSimsInZone(Lot targetLot)
		{
			List<Sim> result = null;
			if (targetLot != null)
			{
				result = targetLot.GetSims();
				if (targetLot.ZoneCurrent != null)
				{
					result = targetLot.ZoneCurrent.Sims;
				}
			}
			return result;
		}

		protected static List<Sim> GetSimsOnLot(Lot targetLot)
		{
			List<Sim> result = null;
			if (targetLot != null)
			{
				List<Sim> list = null;
				list = GetSimsInZone(targetLot);
				if (list != null)
				{
					result = new List<Sim>();
					{
						foreach (Sim item in list)
						{
							if (item != null && item.LotCurrent == targetLot)
							{
								result.Add(item);
							}
						}
						return result;
					}
				}
			}
			return result;
		}*

		public virtual bool OnTriggerDown(UITriggerEventArgs eventArgs)
		{
			switch (eventArgs.TriggerCode)
			{
				case 2880154537u:
					mSelectionCallback?.Invoke(eventArgs);
					return true;
				case 2880154533u:
					mTargetSelected = true;
					mSelectedObject = null;
					return true;
				default:
					return false;//LiveModeState.LiveModeCommonOnTriggerDown(eventArgs);
			}
		}

		public virtual bool OnTriggerUp(UITriggerEventArgs eventArgs)
		{
			return false;//LiveModeState.GenericCameraOnTriggerUp(eventArgs);
		}

		protected virtual bool IsSimTargetableByWonderPower(Sim ob)
		{
			if (ob == null)
			{
				return false;
			}
			if (ob.Service != null)
			{
				return false;
			}
			if (ob.RoutingComponent != null)
			{
				RoutingComponent routingComponent = ob.RoutingComponent;
				if (routingComponent.RoutingParent is Vehicle)
				{
					return false;
				}
			}
			if (ob.IsLeavingLot)
			{
				if (ob.CurrentInteraction == null)
				{
					return false;
				}
				if (!ob.CurrentInteraction.CancellableByPlayer)
				{
					return false;
				}
			}
			return ob.SimDescription.DeathStyle == 0 || mDeadSimTargetable;
		}

		protected virtual bool DoesObjectMatchSim(ulong id, ulong objType, CASAgeGenderFlags age, CASAgeGenderFlags gender)
		{
			ScenePickObjectType val = (ScenePickObjectType)objType;
			if ((int)val == 9)
			{
				ObjectGuid objID = new ObjectGuid(id);
				Sim sim = GameObject.GetObject(objID) as Sim;
				if (IsSimTargetableByWonderPower(sim) && (sim.SimDescription.Age & age) != 0 && (sim.SimDescription.Gender & gender) != 0)
				{
					return true;
				}
			}
			return false;
		}

		protected virtual bool FilterForSim(ref ulong[] objectIds, ref uint[] objectTypes, CASAgeGenderFlags age, CASAgeGenderFlags gender)
		{
			ulong[] array = null;
			uint[] array2 = null;
			int num = 0;
			if (objectIds != null && objectTypes != null && objectIds.Length == objectTypes.Length && objectIds.Length > 0)
			{
				num = 0;
				for (int i = 0; i < objectIds.Length; i++)
				{
					if (DoesObjectMatchSim(objectIds[i], objectTypes[i], age, gender))
					{
						num++;
					}
				}
				array = new ulong[num];
				array2 = new uint[num];
				num = 0;
				for (int j = 0; j < objectIds.Length; j++)
				{
					if (DoesObjectMatchSim(objectIds[j], objectTypes[j], age, gender))
					{
						array[num] = objectIds[j];
						array2[num] = objectTypes[j];
						num++;
					}
				}
			}
			objectIds = array;
			objectTypes = array2;
			return num > 0;
		}

		private bool DoesObjectMatchSimBuffs(ulong id, ulong objType, params BuffNames[] buffNames)
		{
			ScenePickObjectType val = (ScenePickObjectType)objType;
			if ((int)val == 9)
			{
				ObjectGuid objID = new ObjectGuid(id);
				Sim sim = GameObject.GetObject(objID) as Sim;
				if (IsSimTargetableByWonderPower(sim) && !sim.BuffManager.HasAnyElement(buffNames))
				{
					return true;
				}
			}
			return false;
		}

		protected virtual bool FilterOutSimsWithBuffs(ref ulong[] objectIds, ref uint[] objectTypes, params BuffNames[] buffNames)
		{
			ulong[] array = null;
			uint[] array2 = null;
			int num = 0;
			if (objectIds != null && objectTypes != null && objectIds.Length == objectTypes.Length && objectIds.Length > 0)
			{
				num = 0;
				for (int i = 0; i < objectIds.Length; i++)
				{
					if (DoesObjectMatchSimBuffs(objectIds[i], objectTypes[i], buffNames))
					{
						num++;
					}
				}
				array = new ulong[num];
				array2 = new uint[num];
				num = 0;
				for (int j = 0; j < objectIds.Length; j++)
				{
					if (DoesObjectMatchSimBuffs(objectIds[j], objectTypes[j], buffNames))
					{
						array[num] = objectIds[j];
						array2[num] = objectTypes[j];
						num++;
					}
				}
			}
			objectIds = array;
			objectTypes = array2;
			return num > 0;
		}

		protected virtual bool FilterForObject(ref ulong[] objectIds, ref uint[] objectTypes)
		{
			ulong[] array = null;
			uint[] array2 = null;
			int num = 0;
			if (objectIds != null && objectTypes != null && objectIds.Length == objectTypes.Length && objectIds.Length > 0)
			{
				num = 0;
				for (int i = 0; i < objectIds.Length; i++)
				{
					ScenePickObjectType val = (ScenePickObjectType)objectTypes[i];
					if ((int)val == 2)
					{
						ObjectGuid objID = new ObjectGuid(objectIds[i]);
						GameObject @object = GameObject.GetObject(objID);
						if (@object != null)
						{
							num++;
						}
					}
				}
				array = new ulong[num];
				array2 = new uint[num];
				num = 0;
				for (int j = 0; j < objectIds.Length; j++)
				{
					ScenePickObjectType val2 = (ScenePickObjectType)objectTypes[j];
					if ((int)val2 == 2)
					{
						ObjectGuid objID2 = new ObjectGuid(objectIds[j]);
						GameObject object2 = GameObject.GetObject(objID2);
						if (object2 != null)
						{
							array[num] = objectIds[j];
							array2[num] = objectTypes[j];
							num++;
						}
					}
				}
			}
			objectIds = array;
			objectTypes = array2;
			return num > 0;
		}

		public virtual bool PowerWillAffectAnything()
		{
			return true;
		}

		public virtual void DisableSave()
		{
			//Sims3.Gameplay.Gameflow.Singleton.DisableSave("Gameplay/WonderMode:PowerIsDeploying");
		}

		public virtual void EnableSave()
		{
			//Sims3.Gameplay.Gameflow.Singleton.EnableSave("Gameplay/WonderMode:PowerIsDeploying");
		}

		public static Vector3 GetFloorPosition(bool bGetMainFloor, Lot targetLot)
		{
			Vector3 result = Vector3.Empty;
			if (targetLot != null)
			{
				result = targetLot.Position;
				if (bGetMainFloor)
				{
					int level = targetLot.DoesFoundationExistOnLot() ? 1 : 0;
					result.y = World.GetLevelHeight(result.x, result.z, level) + 0.25f;
				}
				else
				{
					int currentLotDisplayLevel = targetLot.CurrentLotDisplayLevel;
					result.y = World.GetLevelHeight(result.x, result.z, currentLotDisplayLevel) + 0.25f;
				}
			}
			return result;
		}
	}*/

	public class WonderPowerStandIdle : Interaction<Sim, GameObject>
	{
		[DoesntRequireTuning]
		private sealed class Definition : NewInteractionDefinition
		{
			private string mPowerName;

			public string PowerName
			{
				get
				{
					return mPowerName;
				}
				set
				{
					mPowerName = value;
				}
			}

			public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
			{
				WonderPowerStandIdle wonderPowerStandIdle = new WonderPowerStandIdle();
				wonderPowerStandIdle.Init(ref parameters);
				return wonderPowerStandIdle;
			}

			public override string GetInteractionName(IActor _a, IGameObject _target, InteractionObjectPair iop)
			{
				return Localization.LocalizeString("Gameplay/WonderMode/Power:" + mPowerName, new object[0]);
			}

			/*public override string[] GetPath()
			{
				return new string[]
				{
					"Sim..."
				};
			}*/

			public override bool Test(IActor _a, IGameObject _target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				return true;
			}
		}

		public static InteractionDefinition GetDefinition(string powerName)
		{
			return new Definition
			{
				PowerName = powerName
			};
		}

		public override bool Run()
		{
			DoLoop(ExitReason.Finished | ExitReason.CanceledByScript, new InsideLoopFunction(UpdateIdle), null);
			return true;
		}

		private void UpdateIdle(StateMachineClient smc, LoopData loopData)
		{
			if (Actor is Sim sim && sim.IsStandingIdle)
			{
				sim.LoopIdle();
			}
		}
	}

	public static class ActivationMethods
    {
		public static void CryHavocActivation(bool _)
        {
			Sim sim = PlumbBob.SelectedActor;
			Lot lot = sim.LotCurrent.IsWorldLot 
				? GetClosestObject((List<Lot>)LotManager.AllLotsWithoutCommonExceptions, sim) 
				: sim.LotCurrent;

			new CryHavocSituation(lot);
		}

		public static void CurseActivation(bool isBacklash)
        {
			Sim selectedSim;
			if (isBacklash)
			{
				selectedSim = RandomUtil.GetRandomObjectFromList(Household.ActiveHousehold.Sims.FindAll((sim) => sim.SimDescription.ChildOrAbove));
			}
			else
			{
				List<SimDescription> targets = PlumbBob.SelectedActor.LotCurrent.GetSims((sim) => sim.SimDescription.ChildOrAbove).ConvertAll((sim) => sim.SimDescription);
				selectedSim = SelectTarget(targets, WonderPowers.LocalizeString("CurseDialogTitle"))?.CreatedSim;
			}

			if (selectedSim != null)
			{
				//CONSIDER visual effect?
				//CONSIDER Toggle power on sound finish?
				//TODO Add glissdown
				Camera.FocusOnSim(selectedSim);
				if (selectedSim.IsSelectable)
				{
					PlumbBob.SelectActor(selectedSim);
				}
				foreach (CommodityKind motive in (Responder.Instance.HudModel as HudModel).GetMotives(selectedSim))
				{
					selectedSim.Motives.SetValue(motive, motive == CommodityKind.Bladder ? -100 : -95);
				}
				selectedSim.BuffManager.AddElement(HashString64("Gamefreak130_CursedBuff"), (Origin)HashString64("FromWonderPower"));
			}
			else
            {
				//TODO Refund or something
            }
			WonderPowers.TogglePowerRunning();
        }

		public static void DivineInterventionActivation(bool _)
        {
			List<SimDescription> targets = new List<Urnstone>(Queries.GetObjects<Urnstone>())
												.ConvertAll((urnstone) => urnstone.DeadSimsDescription)
												.FindAll((description) => description != null);
			Urnstone selectedUrnstone = Urnstone.FindGhostsGrave(SelectTarget(targets, WonderPowers.LocalizeString("DivineInterventionDialogTitle")));
			if (selectedUrnstone != null)
            {
				Audio.StartSound("sting_lifetime_opp_success");
				if (selectedUrnstone.MyGhost == null || !selectedUrnstone.MyGhost.IsSelectable)
                {
					Sim actor = PlumbBob.SelectedActor;
					Vector3 position = actor.Position;
					Vector3 forwardVector = actor.ForwardVector;
					if (FindGoodLocationNearby(selectedUrnstone, ref position, ref forwardVector))
					{
						selectedUrnstone.GhostSpawn(false, position, actor.LotCurrent, false);
					}
					else
					{
						selectedUrnstone.GhostSpawn(false);
					}
				}
				Sim ghost = selectedUrnstone.MyGhost;
				InteractionInstance instance = new DivineInterventionResurrect.Definition().CreateInstance(ghost, ghost, new InteractionPriority(InteractionPriorityLevel.MaxDeath), false, false);
				ghost.InteractionQueue.AddNext(instance);
            }
			else
            {
				//TODO Refund or something
				WonderPowers.TogglePowerRunning();
            }
        }

		public static void DoomActivation(bool isBacklash)
        {
			Sim selectedSim;
			if (isBacklash)
			{
				selectedSim = RandomUtil.GetRandomObjectFromList(Household.ActiveHousehold.Sims.FindAll((sim) => sim.SimDescription.ChildOrAbove));
			}
			else
			{
				List<SimDescription> targets = PlumbBob.SelectedActor.LotCurrent.GetSims((sim) => sim.SimDescription.ChildOrAbove).ConvertAll((sim) => sim.SimDescription);
				selectedSim = SelectTarget(targets, WonderPowers.LocalizeString("DoomDialogTitle"))?.CreatedSim;
			}

			if (selectedSim != null)
			{
				//CONSIDER animation, visual effect?
				//CONSIDER Toggle power on sound finish?
				//TODO cancel all interactions
				Audio.StartSound("sting_job_demote");
				Camera.FocusOnSim(selectedSim);
				if (selectedSim.IsSelectable)
				{
					PlumbBob.SelectActor(selectedSim);
				}
				selectedSim.BuffManager.AddElement(Buffs.BuffDoom.kBuffDoomGuid, (Origin)HashString64("FromWonderPower"));
			}
			else
            {
				//TODO Refund or something
            }
			WonderPowers.TogglePowerRunning();
		}

		public static void EarthquakeActivation(bool _)
        {
			Sim actor = PlumbBob.SelectedActor;
			Lot lot = actor.LotCurrent.IsWorldLot
				? GetClosestObject((List<Lot>)LotManager.AllLotsWithoutCommonExceptions, actor)
				: actor.LotCurrent;

			//TODO Add EOR earthquake sting
			Camera.FocusOnLot(lot.LotId, 2f); //2f is standard lerptime
			Audio.StartSound("earthquake");
			CameraController.Shake(FireFightingJob.kEarthquakeCameraShakeIntensity, FireFightingJob.kEarthquakeCameraShakeDuration);
			lot.AddAlarm(FireFightingJob.kEarthquakeTimeUntilTNS, TimeUnit.Minutes, WonderPowers.TogglePowerRunning, "Gamefreak130 wuz here -- Activation complete alarm", AlarmType.AlwaysPersisted);

			foreach (Sim sim in lot.GetAllActors())
            {
				if (sim.IsPet)
                {
					PetStartleBehavior.StartlePet(sim, StartleType.Invalid, (Origin)HashString64("FromWonderPower"), lot, true, PetStartleReactionType.NoReaction, new InteractionPriority(InteractionPriorityLevel.CriticalNPCBehavior));
                }
				else
                {
					InteractionInstance instance = new PanicReact.Definition().CreateInstance(sim, sim, new InteractionPriority(InteractionPriorityLevel.CriticalNPCBehavior), false, false);
					sim.InteractionQueue.AddNext(instance);
                }
            }

			List<GameObject> breakableObjects = lot.GetObjects<GameObject>((@object) => @object.Repairable is RepairableComponent component && !component.Broken);
			for (int i = 0; i < TunableSettings.kEarthquakeMaxBroken; i++)
            {
				if (breakableObjects.Count == 0) { break; }
				GameObject @object = RandomUtil.GetRandomObjectFromList(breakableObjects);
				@object.Repairable.BreakObject();
				breakableObjects.Remove(@object);
            }
			int maxTrash = RandomUtil.GetInt(1, TunableSettings.kEarthquakeMaxTrash);
			for (int i = 0; i < maxTrash; i++)
            {
				Vector3 randomPosition = lot.GetRandomPosition(true, true);
				TrashPile trashPile = CreateObjectOutOfWorld("TrashPileIndoor") as TrashPile;
				World.FindGoodLocationParams fglParams = new World.FindGoodLocationParams(randomPosition);
				if (PlaceAtGoodLocation(trashPile, fglParams, true))
                {
					trashPile.AddToWorld();
					continue;
                }
				i--;
            }
        }

		public static void FeralPosessionActivation(bool isBacklash)
        {
			//TODO this
			throw new NotImplementedException();
        }

		public static void FireActivation(bool isBacklash)
        {
			Lot selectedLot;
			if (isBacklash)
			{
				Sim actor = PlumbBob.SelectedActor;
				selectedLot = actor.LotCurrent.IsWorldLot
					? GetClosestObject((List<Lot>)LotManager.AllLotsWithoutCommonExceptions, actor)
					: actor.LotCurrent;
			}
			else
			{
				selectedLot = SelectTarget(WonderPowers.LocalizeString("FireDestinationTitle"), WonderPowers.LocalizeString("FireDestinationConfirm"));
			}

			new FireSituation(selectedLot);
		}

		public static void GhostifyActivation(bool _)
        {
			//TEST Child pets
			List<SimDescription> targets = PlumbBob.SelectedActor.LotCurrent.GetSims((sim) => sim.SimDescription.ChildOrAbove && !sim.IsGhostOrHasGhostBuff && !sim.BuffManager.HasElement((BuffNames)Buffs.BuffGhostify.kBuffGhostifyGuid))
																			.ConvertAll((sim) => sim.SimDescription);
			Sim sim = SelectTarget(targets, WonderPowers.LocalizeString("GhostifyDialogTitle"))?.CreatedSim;
			if (sim != null)
			{
				Camera.FocusOnSim(sim);
				if (sim.IsSelectable)
				{
					PlumbBob.SelectActor(sim);
				}
				sim.InteractionQueue.CancelAllInteractions();
				sim.BuffManager.AddElement(Buffs.BuffGhostify.kBuffGhostifyGuid, (Origin)HashString64("FromWonderPower"));
			}
			else
            {
				//TODO refund or something
            }
			WonderPowers.TogglePowerRunning();
		}

		public static void MeteorStrikeActivation(bool isBacklash)
        {
			Lot selectedLot;
			if (isBacklash)
			{
				List<Lot> list = (LotManager.AllLotsWithoutCommonExceptions as List<Lot>).FindAll((lot) => lot.CommercialLotSubType != CommercialLotSubType.kEP1_HiddenTomb);
				selectedLot = RandomUtil.GetRandomObjectFromList(list);
			}
			else
			{
				selectedLot = SelectTarget(WonderPowers.LocalizeString("MeteorDestinationTitle"), WonderPowers.LocalizeString("MeteorDestinationConfirm"));
			}

			Audio.StartSound("sting_meteor_forshadow");
			Meteor.TriggerMeteorEvent(selectedLot.GetRandomPosition(false, true));
			AlarmManager.Global.AddAlarm(Meteor.kMeteorLifetime + 3, TimeUnit.Minutes, WonderPowers.TogglePowerRunning, "Gamefreak130 wuz here -- Activation complete alarm", AlarmType.AlwaysPersisted, null);
		}

		private static SimDescription SelectTarget(List<SimDescription> sims, string title)
		{
			SimDescription target = null;
			if (sims != null && sims.Count > 0)
			{
				List<ObjectPicker.HeaderInfo> list = new List<ObjectPicker.HeaderInfo>
				{
					new ObjectPicker.HeaderInfo("Ui/Caption/ObjectPicker:Name", "Ui/Tooltip/ObjectPicker:Name", 500)
				};
				List<ObjectPicker.RowInfo> list2 = new List<ObjectPicker.RowInfo>();
				foreach (SimDescription description in sims)
				{
					ObjectPicker.RowInfo item = new ObjectPicker.RowInfo(description, new List<ObjectPicker.ColumnInfo>
					{
						new ObjectPicker.ThumbAndTextColumn(description.GetThumbnailKey(ThumbnailSize.Large, 0), description.FullName)
					});
					list2.Add(item);
				}
				List<ObjectPicker.TabInfo> list3 = new List<ObjectPicker.TabInfo>
				{
					new ObjectPicker.TabInfo("shop_all_r2", Localization.LocalizeString("Ui/Tooltip/CAS/LoadSim:Header"), list2)
				};

				while (target == null)
				{
					target = ObjectPickerDialog.Show(true, ModalDialog.PauseMode.PauseSimulator, title, Localization.LocalizeString("Ui/Caption/ObjectPicker:OK"), Localization.LocalizeString("Ui/Caption/ObjectPicker:Cancel"), list3, list, 1)?[0].Item as SimDescription;
				}
			}
			return target;
		}

		private static Lot SelectTarget(string title, string confirm)
        {
			List<IMapTagPickerInfo> list = new List<IMapTagPickerInfo>();
			foreach (Lot lot in LotManager.AllLotsWithoutCommonExceptions)
			{
				if (lot.CommercialLotSubType != CommercialLotSubType.kEP1_HiddenTomb)
				{
					list.Add(new MapTagPickerLotInfo(lot, lot.IsPlayerHomeLot ? MapTagType.HomeLot
														: lot.IsResidentialLot ? MapTagType.NeighborLot
														: MapTagType.Venue));
				}
			}
			IMapTagPickerInfo info = MapTagPickerUncancellable.Show(list, title, confirm);
			return LotManager.GetLot(info.LotId);
		}
	}

	internal static class OptionsInjector
    {
		private static bool mOptionsInjectionHandled;

		internal static bool InjectOptions()
		{
			try
			{
				if (OptionsDialog.sDialog != null)
				{
					if (!mOptionsInjectionHandled)
					{
						OptionsDialog.sDialog.mMusicData.Add(new Dictionary<string, List<OptionsDialog.SongData>>());
						OptionsDialog.sDialog.mMusicData.Add(new Dictionary<string, List<OptionsDialog.SongData>>());
						Button button = OptionsDialog.sDialog.mModalDialogWindow.GetChildByID(2822726298u, true) as Button;
						button.Click += OnMusicSelectionClicked;
						button = OptionsDialog.sDialog.mModalDialogWindow.GetChildByID(2822726299u, true) as Button;
						button.Click += OnMusicSelectionClicked;
						ParseXml("MusicEntriesKarmaLoad");
						foreach (uint num in Enum.GetValues(typeof(ProductVersion)))
						{
							if (GameUtils.IsInstalled((ProductVersion)num))
							{
								string name = ((ProductVersion)num).ToString();
								ParseXml("MusicEntriesKarmaLoad" + name);
							}
						}
						mOptionsInjectionHandled = true;
					}
				}
				else
				{
					mOptionsInjectionHandled = false;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static void OnMusicSelectionClicked(WindowBase sender, UIButtonClickEventArgs eventArgs)
		{
			if (sender.ID == 2822726298u)
			{
				OptionsDialog.sDialog.mButtonHolderWindow.Visible = false;
				OptionsDialog.sDialog.mCurrentFilterIndex = (OptionsDialog.MusicTypeIndex)5;
			}
			if (sender.ID == 2822726299u)
			{
				OptionsDialog.sDialog.mButtonHolderWindow.Visible = false;
				OptionsDialog.sDialog.mCurrentFilterIndex = (OptionsDialog.MusicTypeIndex)6;
			}
			OptionsDialog.sDialog.UpdateTable();
		}

		private static void ParseXml(string xmlFileName)
		{
			if (Simulator.LoadXML(xmlFileName) is XmlDocument xml && xml.GetElementsByTagName("MusicSelection")[0] is XmlElement xmlElement)
			{
				OptionsDialog.sDialog.LoadSongData(xmlElement, "Karma", 5);
				OptionsDialog.sDialog.LoadSongData(xmlElement, "Load", 6);
				if (OptionsDialog.sDialog.mItemGridGenreButtons.Count > 0)
				{
					OptionsDialog.sDialog.mItemGridGenreButtons.SelectedItem = 0;
					string text = OptionsDialog.sDialog.mItemGridGenreButtons.InternalGrid.CellTags[0, 0] as string;
					if (!string.IsNullOrEmpty(text))
					{
						OptionsDialog.sDialog.mCurrentGenre = text;
						OptionsDialog.sDialog.UpdateTable();
					}
				}
			}
		}
	}
}
