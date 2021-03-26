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
using Gamefreak130.WonderPowersSpace.UI;
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
using Gamefreak130.Common;
using System.Text;
using System.Collections.ObjectModel;
using Sims3.UI.CAS;

namespace Gamefreak130.WonderPowersSpace.Helpers
{
	// TODO Separate booter from the power manager
	public interface IPowerBooter
    {
		void LoadPowers();
    }

	internal class PowerExceptionLogger : EventLogger<Exception>
	{
		private PowerExceptionLogger()
        {
        }

		internal static readonly PowerExceptionLogger sInstance = new();

        protected override void Notify() => StyledNotification.Show(new(WonderPowerManager.LocalizeString("PowerError"), StyledNotification.NotificationStyle.kSystemMessage));
	}

	[Persistable]
	public class WonderPower
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

		private readonly MethodInfo mRunMethod;

		private readonly int mCost;

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
			WonderPowerManager.Add(this);
		}

		[Persistable(false)]
		private delegate void RunDelegate(bool isBacklash);//, GameObject target);

		// The Run() method is used in a OneShotFunctionWithParam added to the Simulator when the power selection dialog ends,
		// So that any power-specific dialogs should not fire until after the power selection dialog is disposed
		// Hence why the boolean argument here is an object -- FunctionWithParam delegates take a generic object as their argument
		public void Run(object isBacklash)
        {
            if (isBacklash is bool backlash)
            {
				try
				{
                    Gameflow.SetGameSpeed(Normal, Gameflow.SetGameSpeedContext.Gameplay);
                    // Activation of any power will disable the karma menu
                    // Re-enabling is left to the powers' individual run methods when activation is complete
                    WonderPowerManager.TogglePowerRunning();
					int newKarma = WonderPowerManager.GetKarma();
					if (backlash)
                    {
						newKarma += Cost();
                    }
					else
                    {
						newKarma -= Cost();
                    }
					WonderPowerManager.SetKarma(newKarma);
					RunDelegate run = (RunDelegate)Delegate.CreateDelegate(typeof(RunDelegate), mRunMethod);
                    run(backlash);
					// TODO make the activation functions booleans to handle failures that do not warrant a script error
                }
                catch (Exception e)
                {
                    // Since this runs on the Simulator, NRaas ErrorTrap can't catch any exceptions that occur
                    // So we'll do it live, f*** it! I'll write it, and we'll do it live!
                    PowerExceptionLogger.sInstance.Log(e);
                    WonderPowerManager.TogglePowerRunning();
                    if (!backlash)
                    {
                        WonderPowerManager.SetKarma(WonderPowerManager.GetKarma() + Cost());
                    }
                }
            }
        }

		public int Cost()
        {
			/*if (WonderPowers.NumFreePowers > 0)
			{
				return 0;
			}*/
			int cost = mCost;
			if (Household.ActiveHousehold is not null)
			{
				foreach (Sim current in Household.ActiveHousehold.Sims)
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
    }

	[Persistable]
	public sealed class WonderPowerManager : IPowerBooter //ScriptObject
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

		private float mTotalPromisesFulfilledKarma;

		private int mKarmaPromisesFulfilled;

		private int mTotalPromisesFulfilled;

		[Tunable, TunableComment("How many karma points the player starts with")]
		private static readonly int kInitialKarmaLevel = 0;

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

		public static Vector3 kWonderBuffVfxOffset = new(0.097f, 0.5f, -0.113f);

		private static WitchingHourState smWitchingHourState = WitchingHourState.NONE;

		[PersistableStatic]
		private static WonderPowerManager sInstance;

        private static readonly List<WonderPower> sAllWonderPowers = new();

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

		private int mCurrentKarmaLevel = kInitialKarmaLevel;

		[PersistableStatic]
		private static float sCurrentBadKarmaChance = 0f;

        public static float NearbySimsDistance => kNearbySimsDistance;

        private int Karma
        {
            get => mCurrentKarmaLevel;
            set
            {
                mCurrentKarmaLevel = value;
                if (mCurrentKarmaLevel < -100)
                {
                    mCurrentKarmaLevel = -100;
                }
                if (mCurrentKarmaLevel > 100)
                {
                    mCurrentKarmaLevel = 100;
                }
                if (mCurrentKarmaLevel == 100)
                {
                    //EventTracker.SendEvent(EventTypeId.kChallengeKarmaReached100);
                }
            }
        }

        private static bool DebugBadPowersOn
        {
            get => sInstance.mDebugBadPowersOn;
            set => sInstance.mDebugBadPowersOn = value;
        }

        public static bool BadPowersOn { get; set; } = true;

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

		private WonderPowerManager()
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

		/*public override ScriptExecuteType Init(bool postLoad)
		{
			return ScriptExecuteType.Threaded;
		}*/

		public static bool IsInWitchingHour()
		{
			return smWitchingHourState is not WitchingHourState.NONE;
		}

		/*public override void Simulate()
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
								if (!AnyPowersRunning() && BadPowersOn)
								{
									mWitchingHourPower = CheckTriggerBadPower();
									if (mWitchingHourPower != null)
									{
										flag = true;
									}
								}
								if (!flag)
								{
									float karma = Karma;
									float @float = RandomUtil.GetFloat(kKarmaDailyRationLow, kKarmaDailyRationHigh);
									if (Karma < 100f)
									{
										//Karma += @float;
									}
									VisualEffect visualEffect = VisualEffect.Create("wonderkarma_lot_out");
									if (visualEffect != null)
									{
										//Vector3 floorPosition3 = WonderPowerActivation.GetFloorPosition(true, LotManager.ActiveLot);
										//visualEffect.SetPosAndOrient(floorPosition3, Vector3.UnitX, Vector3.UnitY);
										visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
									}
									KarmaDial.Load(karma, Karma, true);
									if (!bHaveShownWitchingHourDialog)
									{
										KarmaDial.WitchingHourCompletedFunction = (KarmaDial.WitchingHourCompleted)(object)new KarmaDial.WitchingHourCompleted(DisplayWitchingHourDialogPopup);
									}
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
		}*/

		internal static MethodInfo FindMethod(string methodName)
        {
			if (methodName.Contains(","))
			{
				string[] array = methodName.Split(new[] { ',' });
				string typeName = array[0].Trim() + "," + array[1].Trim();
				Type type = Type.GetType(typeName, true);
				string text = array[2];
				text = text.Trim();
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
			if (sInstance is not null)
			{
				Simulator.AddObject(new Sims3.UI.OneShotFunctionTask(delegate ()
				{
					sInstance.OnShowKarmaStar(dream);
				}));
				sInstance.mTotalPromisesFulfilled++;
				float num = 0;// sCurrentKarmaWishAmountModifier;
				float num2 = kKarmaBasicWishAmount;
				if (dream is ActiveDreamNode activeDreamNode && activeDreamNode.Owner is not null && activeDreamNode.IsMajorWish)
				{
					num2 = kKarmaLifetimeWishAmount;
				}
				sInstance.mTotalPromisesFulfilledKarma += num2 + num;
			}
		}

		public void OnShowKarmaStar(object d)
		{
			if (mKarmaPromisesFulfilled == 0)
			{
				ShowKarmaDial();
			}
			IDreamAndPromise dreamAndPromise = d as IDreamAndPromise;
			if (dreamAndPromise is ActiveDreamNode activeDreamNode && activeDreamNode.Owner is not null)
			{
				VisualEffect.FireOneShotEffect("wonderkarma_gain", activeDreamNode.Owner, Sim.FXJoints.HatGrip, VisualEffect.TransitionType.SoftTransition);
			}
			mKarmaPromisesFulfilled++;
			if (mKarmaPromisesFulfilled == sInstance.mTotalPromisesFulfilled)
			{
				mTotalPromisesFulfilledKarma = 0f;
				mKarmaPromisesFulfilled = 0;
				mTotalPromisesFulfilled = 0;
				/*while (KarmaDial.IsVisible)
				{
					Simulator.Sleep(0u);
				}*/
			}
		}

		public void ShowKarmaDial()
		{
			float karma = GetKarma();
			float num = karma + mTotalPromisesFulfilledKarma;
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
			int num = power.Cost();
			if (num > 0f)
			{
				Karma -= num;
			}
			sCurrentBadKarmaChance += kKarmaBadEventIncreaseConstant;
			//power.WasUsed = true;
		}

		public void CancelledPower(WonderPower power)
		{
			int num = power.Cost();
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

		internal static void Init()
		{
			sInstance = new();
			//Simulator.AddObject(sInstance);
		}

        internal static void ReInit()
		{
			/*foreach (WonderPowerActivation mActiveWonderPower in sInstance.mActiveWonderPowers)
			{
				mActiveWonderPower.CleanupAfterPower();
			}*/
			Ferry<WonderPowerManager>.LoadCargo();
			sCurrentBadKarmaChance = 0f;
			//sInstance.Destroy();
			Init();
		}

		internal static void LoadValues() => Ferry<WonderPowerManager>.UnloadCargo();

		public static void LoadMainPowers() => sInstance.LoadPowers();

		public void LoadPowers()
		{
			XmlDbData xmlDbData = XmlDbData.ReadData("Gamefreak130_KarmaPowers");
			XmlDbTable xmlDbTable = null;
			xmlDbData?.Tables.TryGetValue("Power", out xmlDbTable);
			if (xmlDbTable is not null)
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
							MethodInfo methodInfo = FindMethod(runMethod);
							new WonderPower(name, isBad, cost, methodInfo);
						}
					}
				}
			}
		}

        public static void Add(WonderPower s) => sAllWonderPowers.Add(s);

		public static bool HasEnoughKarma(int karma) => sInstance.Karma - karma >= -100;

		public static int GetKarma() => sInstance.Karma;

		public static void SetKarma(int karma) => sInstance.Karma = karma;

		public static void OnPowerUsed(WonderPower power)
		{
			if (sInstance is not null)
			{
				sInstance.UsedPower(power);
			}
		}

		public static void OnPowerCancelled(WonderPower power)
		{
			if (sInstance is not null)
			{
				sInstance.CancelledPower(power);
			}
		}

		public static WonderPower GetByName(string name)
		{
			foreach (WonderPower mAllWonderPower in sAllWonderPowers)
			{
				if (name.Equals(mAllWonderPower?.WonderPowerName, StringComparison.InvariantCultureIgnoreCase))
				{
					return mAllWonderPower;
				}
			}
			return null;
		}

        public static ReadOnlyCollection<WonderPower> GetWonderPowerList() => sAllWonderPowers.AsReadOnly();

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
				WonderPowerStandIdle wonderPowerStandIdle = new();
				wonderPowerStandIdle.Init(ref parameters);
				return wonderPowerStandIdle;
			}

			public override string GetInteractionName(IActor _a, IGameObject _target, InteractionObjectPair iop)
			{
				return Localization.LocalizeString("Gameplay/WonderMode/Power:" + mPowerName);
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
			DoLoop(ExitReason.Finished | ExitReason.CanceledByScript, new(UpdateIdle), null);
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
			Sim selectedSim = null;
			//CONSIDER Pets?
			if (isBacklash)
			{
				List<Sim> validSims = Household.ActiveHousehold.Sims.FindAll((sim) => sim.SimDescription.ChildOrAbove && !sim.BuffManager.HasElement((BuffNames)HashString64("Gamefreak130_CursedBuff")));
				if (validSims.Count > 0)
				{
					selectedSim = RandomUtil.GetRandomObjectFromList(validSims);
				}
			}
			else
			{
				List<SimDescription> targets = PlumbBob.SelectedActor.LotCurrent.GetSims((sim) => sim.SimDescription.ChildOrAbove && !sim.BuffManager.HasElement((BuffNames)HashString64("Gamefreak130_CursedBuff"))).ConvertAll((sim) => sim.SimDescription);
				selectedSim = SelectTarget(targets, WonderPowerManager.LocalizeString("CurseDialogTitle"))?.CreatedSim;
			}
			
			if (selectedSim is null)
            {
				//return false;
            }

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
				selectedSim.Motives.SetValue(motive, motive is CommodityKind.Bladder ? -100 : -95);
			}
			selectedSim.BuffManager.AddElement(HashString64("Gamefreak130_CursedBuff"), (Origin)HashString64("FromWonderPower"));
			WonderPowerManager.TogglePowerRunning();
        }

		public static void DivineInterventionActivation(bool _)
        {
			List<SimDescription> targets = new List<Urnstone>(Queries.GetObjects<Urnstone>())
												.ConvertAll((urnstone) => urnstone.DeadSimsDescription)
												.FindAll((description) => description is not null);
			Urnstone selectedUrnstone = Urnstone.FindGhostsGrave(SelectTarget(targets, WonderPowerManager.LocalizeString("DivineInterventionDialogTitle")));
			if (selectedUrnstone is null)
            {
				//return false;
            }
			Audio.StartSound("sting_lifetime_opp_success");
			if (selectedUrnstone.MyGhost is null or { IsSelectable: false })
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
			InteractionInstance instance = new DivineInterventionResurrect.Definition().CreateInstance(ghost, ghost, new(InteractionPriorityLevel.MaxDeath), false, false);
			ghost.InteractionQueue.AddNext(instance);
        }

		public static void DoomActivation(bool isBacklash)
        {
			Sim selectedSim = null;
			if (isBacklash)
			{
				List<Sim> validSims = Household.ActiveHousehold.AllActors.FindAll((sim) => sim.SimDescription.ChildOrAbove && !sim.BuffManager.HasElement(BuffNames.UnicornsIre));
				if (validSims.Count > 0)
				{
					selectedSim = RandomUtil.GetRandomObjectFromList(validSims);
				}
			}
			else
			{
				List<SimDescription> targets = PlumbBob.SelectedActor.LotCurrent.GetAllActors()
																				.FindAll((sim) => sim.SimDescription.ChildOrAbove && !sim.BuffManager.HasElement(BuffNames.UnicornsIre))
																				.ConvertAll((sim) => sim.SimDescription);
				selectedSim = SelectTarget(targets, WonderPowerManager.LocalizeString("DoomDialogTitle"))?.CreatedSim;
			}

			if (selectedSim is null)
            {
				//return false;
            }

			//CONSIDER animation, visual effect?
			//CONSIDER Toggle power on sound finish?
			//CONSIDER Negative trait swap?
			//TODO cancel all interactions
			Audio.StartSound("sting_job_demote");
			Camera.FocusOnSim(selectedSim);
			if (selectedSim.IsSelectable)
			{
				PlumbBob.SelectActor(selectedSim);
			}
			selectedSim.BuffManager.AddBuff(BuffNames.UnicornsIre, -40, 1440, false, MoodAxis.None, (Origin)HashString64("FromWonderPower"), true);
			BuffInstance buff = selectedSim.BuffManager.GetElement(BuffNames.UnicornsIre);
			buff.mBuffName = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DoomBuff";
			buff.mDescription = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DoomBuffDescription";
			// This will automatically trigger the BuffsChanged event, so the UI should refresh itself after this and we won't have to do it manually
			buff.SetThumbnail("doom", ProductVersion.BaseGame, selectedSim);
			WonderPowerManager.TogglePowerRunning();
		}

		public static void EarthquakeActivation(bool _)
        {
			Sim actor = PlumbBob.SelectedActor;
			Lot lot = actor.LotCurrent.IsWorldLot
				? GetClosestObject((List<Lot>)LotManager.AllLotsWithoutCommonExceptions, actor)
				: actor.LotCurrent;

			//TODO Add EOR earthquake sting
			lot.AddAlarm(30f, TimeUnit.Seconds, () => Camera.FocusOnLot(lot.LotId, 2f), "Gamefreak130 wuz here -- Activation focus alarm", AlarmType.NeverPersisted); //2f is standard lerptime
			Audio.StartSound("earthquake");
			CameraController.Shake(FireFightingJob.kEarthquakeCameraShakeIntensity, FireFightingJob.kEarthquakeCameraShakeDuration);
			lot.AddAlarm(FireFightingJob.kEarthquakeTimeUntilTNS, TimeUnit.Minutes, WonderPowerManager.TogglePowerRunning, "Gamefreak130 wuz here -- Activation complete alarm", AlarmType.AlwaysPersisted);

			foreach (Sim sim in lot.GetAllActors())
            {
				if (sim.IsPet)
                {
					PetStartleBehavior.StartlePet(sim, StartleType.Invalid, (Origin)HashString64("FromWonderPower"), lot, true, PetStartleReactionType.NoReaction, new(InteractionPriorityLevel.CriticalNPCBehavior));
                }
				else
                {
					InteractionInstance instance = new PanicReact.Definition().CreateInstance(sim, sim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false);
					sim.InteractionQueue.AddNext(instance);
                }
            }

			List<GameObject> breakableObjects = lot.GetObjects<GameObject>((@object) => @object.Repairable is { Broken: false });
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
				World.FindGoodLocationParams fglParams = new(randomPosition);
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
				selectedLot = SelectTarget(WonderPowerManager.LocalizeString("FireDestinationTitle"), WonderPowerManager.LocalizeString("FireDestinationConfirm"));
			}

			new FireSituation(selectedLot);
		}

		public static void GhostifyActivation(bool _)
        {
			List<SimDescription> targets = PlumbBob.SelectedActor.LotCurrent.GetAllActors()
																			.FindAll((sim) => sim.SimDescription.ChildOrAbove && !sim.IsGhostOrHasGhostBuff && !sim.BuffManager.HasElement((BuffNames)Buffs.BuffGhostify.kBuffGhostifyGuid))
																			.ConvertAll((sim) => sim.SimDescription);
			Sim sim = SelectTarget(targets, WonderPowerManager.LocalizeString("GhostifyDialogTitle"))?.CreatedSim;
			if (sim is null)
            {
				//return false;
            }
			Camera.FocusOnSim(sim);
			if (sim.IsSelectable)
			{
				PlumbBob.SelectActor(sim);
			}
			sim.InteractionQueue.CancelAllInteractions();
			sim.BuffManager.AddElement(Buffs.BuffGhostify.kBuffGhostifyGuid, (Origin)HashString64("FromWonderPower"));
			WonderPowerManager.TogglePowerRunning();
		}

		public static void GhostsActivation(bool isBacklash)
        {
			Lot selectedLot;
			if (isBacklash)
			{
				//List<Lot> list = (LotManager.AllLotsWithoutCommonExceptions as List<Lot>).FindAll((lot) => lot.CommercialLotSubType is not CommercialLotSubType.kEP1_HiddenTomb);
				//selectedLot = RandomUtil.GetRandomObjectFromList(list);
				Sim actor = PlumbBob.SelectedActor;
				selectedLot = actor.LotCurrent.IsWorldLot
					? GetClosestObject((List<Lot>)LotManager.AllLotsWithoutCommonExceptions, actor)
					: actor.LotCurrent;
			}
			else
			{
				selectedLot = SelectTarget(WonderPowerManager.LocalizeString("GhostsDestinationTitle"), WonderPowerManager.LocalizeString("GhostsDestinationConfirm"));
			}

			new GhostsSituation(selectedLot);
		}

		public static void GoodMoodActivation(bool _)
        {
			Audio.StartSound("sting_good_mood");
			Camera.FocusOnSelectedSim();
			foreach (Sim sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors())
			{
				//CONSIDER visual effect?
				//CONSIDER Toggle power on sound finish?
				foreach (BuffInstance bi in new List<BuffInstance>(sim.BuffManager.Buffs))
                {
					if (bi is {Guid: not (BuffNames.Singed or BuffNames.HavingAMidlifeCrisis or BuffNames.HavingAMidlifeCrisisWithPromise or BuffNames.MalePregnancy), EffectValue: < 0})
                    {
						sim.BuffManager.ForceRemoveBuff(bi.Guid);
                    }
                }
				sim.BuffManager.AddElement(HashString64("Gamefreak130_GoodMoodBuff"), (Origin)HashString64("FromWonderPower"));
			}
			//CONSIDER styled notifications (not just for this, but for all of these potentially?)
			WonderPowerManager.TogglePowerRunning();
		}

		public static void InstantBeautyActivation(bool _)
        {
			List<SimDescription> targets = PlumbBob.SelectedActor.LotCurrent.GetSims((sim) => sim.SimDescription.ToddlerOrAbove && !sim.OccultManager.DisallowClothesChange() && !sim.BuffManager.DisallowClothesChange()).ConvertAll((sim) => sim.SimDescription);
			Sim selectedSim = SelectTarget(targets, WonderPowerManager.LocalizeString("InstantBeautyDialogTitle"))?.CreatedSim;
			if (selectedSim is null)
            {
				//return false;
            }
            // CONSIDER anim/vis effect and sting?
            GameStates.sSingleton.mInWorldState.GotoCASMode((InWorldState.SubState)HashString32("CASWonderModeState"));
			CASLogic singleton = CASLogic.GetSingleton();
			singleton.LoadSim(selectedSim.SimDescription, selectedSim.CurrentOutfitCategory, 0);
			singleton.UseTempSimDesc = true;
			while (GameStates.NextInWorldStateId is not InWorldState.SubState.LiveMode)
			{
				Simulator.Sleep(1U);
			}
			Camera.FocusOnSim(selectedSim);
			WonderPowerManager.TogglePowerRunning();
		}

		public static void LuckyBreakActivation(bool _)
		{
			List<SimDescription> targets = PlumbBob.SelectedActor.LotCurrent.GetAllActors()
																			.FindAll((sim) => sim.SimDescription.ChildOrAbove && !sim.BuffManager.HasElement(BuffNames.UnicornsBlessing))
																			.ConvertAll((sim) => sim.SimDescription);
			Sim selectedSim = SelectTarget(targets, WonderPowerManager.LocalizeString("LuckyBreakDialogTitle"))?.CreatedSim;

			if (selectedSim is null)
            {
				//return false;
            }

			//CONSIDER animation, visual effect?
			//CONSIDER Toggle power on sound finish?
			//TODO cancel all interactions
			Audio.StartSound("sting_good_mood");
			Camera.FocusOnSim(selectedSim);
			if (selectedSim.IsSelectable)
			{
				PlumbBob.SelectActor(selectedSim);
			}
			selectedSim.BuffManager.AddBuff(BuffNames.UnicornsBlessing, 40, 1440, false, MoodAxis.Happy, (Origin)HashString64("FromWonderPower"), true);
			BuffInstance buff = selectedSim.BuffManager.GetElement(BuffNames.UnicornsBlessing);
			buff.mBuffName = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_LuckyBreakBuff";
			buff.mDescription = "Gameplay/Excel/Buffs/BuffList:Gamefreak130_LuckyBreakBuffDescription";
			// This will automatically trigger the BuffsChanged event, so the UI should refresh itself after this and we won't have to do it manually
			buff.SetThumbnail("moodlet_feelinglucky", ProductVersion.BaseGame, selectedSim);
			WonderPowerManager.TogglePowerRunning();
		}

		public static void MeteorStrikeActivation(bool isBacklash)
        {
			Lot selectedLot;
			if (isBacklash)
			{
				//List<Lot> list = (LotManager.AllLotsWithoutCommonExceptions as List<Lot>).FindAll((lot) => lot.CommercialLotSubType is not CommercialLotSubType.kEP1_HiddenTomb);
				//selectedLot = RandomUtil.GetRandomObjectFromList(list);
				Sim actor = PlumbBob.SelectedActor;
				selectedLot = actor.LotCurrent.IsWorldLot
					? GetClosestObject((List<Lot>)LotManager.AllLotsWithoutCommonExceptions, actor)
					: actor.LotCurrent;
			}
			else
			{
				selectedLot = SelectTarget(WonderPowerManager.LocalizeString("MeteorDestinationTitle"), WonderPowerManager.LocalizeString("MeteorDestinationConfirm"));
			}

			Audio.StartSound("sting_meteor_forshadow");
			selectedLot.AddAlarm(30f, TimeUnit.Seconds, () => Camera.FocusOnLot(selectedLot.LotId, 2f), "Gamefreak130 wuz here -- Activation focus alarm", AlarmType.NeverPersisted);
			Meteor.TriggerMeteorEvent(selectedLot.GetRandomPosition(false, true));
			AlarmManager.Global.AddAlarm(Meteor.kMeteorLifetime + 3, TimeUnit.Minutes, WonderPowerManager.TogglePowerRunning, "Gamefreak130 wuz here -- Activation complete alarm", AlarmType.AlwaysPersisted, null);
		}

		private static SimDescription SelectTarget(List<SimDescription> sims, string title)
		{
			SimDescription target = null;
			if (sims?.Count > 0)
			{
				List<ObjectPicker.HeaderInfo> list = new()
				{
					new("Ui/Caption/ObjectPicker:Name", "Ui/Tooltip/ObjectPicker:Name", 500)
				};
				List<ObjectPicker.RowInfo> list2 = new();
				foreach (SimDescription description in sims)
				{
					ObjectPicker.RowInfo item = new(description, new()
					{
						new ObjectPicker.ThumbAndTextColumn(description.GetThumbnailKey(ThumbnailSize.Large, 0), description.FullName)
					});
					list2.Add(item);
				}
				List<ObjectPicker.TabInfo> list3 = new()
				{
					new("shop_all_r2", Localization.LocalizeString("Ui/Tooltip/CAS/LoadSim:Header"), list2)
				};

				while (target is null)
				{
					target = ObjectPickerDialog.Show(true, ModalDialog.PauseMode.PauseSimulator, title, Localization.LocalizeString("Ui/Caption/ObjectPicker:OK"), Localization.LocalizeString("Ui/Caption/ObjectPicker:Cancel"), list3, list, 1)?[0].Item as SimDescription;
				}
			}
			return target;
		}

		private static Lot SelectTarget(string title, string confirm)
        {
			List<IMapTagPickerInfo> list = new();
			foreach (Lot lot in LotManager.AllLotsWithoutCommonExceptions)
			{
				if (lot.CommercialLotSubType is not CommercialLotSubType.kEP1_HiddenTomb)
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

	public class CASWonderModeState : CASFullModeState
    {
		// CONSIDER route through MasterControllerIntegration if necessary
		public CASWonderModeState() : base()
        {
			mStateId = (int)HashString32("CASWonderModeState");
			mStateName = "CAS Wonder Mode";
		}

        public override void Startup()
        {
			base.Startup();
			CASLogic cas = CASLogic.GetSingleton();
			cas.ShowUI += OnShowUI;
        }

        public override void Shutdown()
        {
			CASLogic cas = CASLogic.GetSingleton();
			cas.ShowUI -= OnShowUI;
			base.Shutdown();
		}

        public static void OnShowUI(bool toShow)
        {
			if (toShow)
			{
				CASWonderMode.HideCharacterSheetElement((uint)CASCharacterSheet.ControlIDs.CharacterButton);
				CASWonderMode.HideCharacterSheetElement((uint)CASCharacterSheet.ControlIDs.CharacterText);
				CASWonderMode.HideCharacterSheetElement((uint)CASCharacterSheet.ControlIDs.ClothingButton);
				CASWonderMode.HideCharacterSheetElement((uint)CASCharacterSheet.ControlIDs.ClothingText);
				CASWonderMode.HideCharacterSheetElement((uint)CASCharacterSheet.ControlIDs.RandomizeButton);
				CASWonderMode.HideCharacterSheetElement((uint)CASCharacterSheet.ControlIDs.RandomizeFaceButton);

				if (CASBasics.gSingleton?.GetChildByID((uint)CASBasics.ControlIDs.HumanBasicsWindow, true) is WindowBase window)
                {
					for (uint i = 0; i < 5; i++)
					{
						if (window.GetChildByIndex(i) is WindowBase window2)
                        {
							window2.Visible = false;
                        }
					}
					WindowBase window3 = window.GetChildByIndex(5);
					if (window3 is not null)
					{
						window3.Area = new(new(window3.Area.TopLeft.x, 80), new(window3.Area.BottomRight.x, 80));
					}
					window3 = window.GetChildByIndex(6);
					if (window3 is not null)
					{
						window3.Area = new(new(window3.Area.TopLeft.x, 170), new(window3.Area.BottomRight.x, 170));
					}
				}

				if (CASPuck.Instance is CASPuck puck)
                {
					if (puck.GetChildByID((uint)CASPuck.ControlIDs.CloseButton, true) is Button button)
                    {
						button.Click -= puck.OnCloseClick;
						button.Click -= CASWonderMode.OnCloseClick;
						button.Click += CASWonderMode.OnCloseClick;
                    }
					if (puck.GetChildByID((uint)CASPuck.ControlIDs.OptionsButton, true) is Button button2)
                    {
						button2.Click -= puck.OnOptionsClick;
						button2.Click -= CASWonderMode.OnOptionsClick;
						button2.Click += CASWonderMode.OnOptionsClick;
                    }
                }
			}
		}
    }
}