using Gamefreak130.Common.Helpers;
using Gamefreak130.Common.Structures;
using Gamefreak130.Common.UI;
using Gamefreak130.WonderPowersSpace.Loggers;
using Gamefreak130.WonderPowersSpace.UI;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Sims3.UI.CAS;
using Sims3.UI.CAS.CAP;
using Sims3.UI.Hud;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using static Sims3.Gameplay.GlobalFunctions;
using static Sims3.SimIFace.Gameflow.GameSpeed;
using static Sims3.SimIFace.ResourceUtils;
using Gameflow = Sims3.Gameplay.Gameflow;
using OneShotFunctionTask = Sims3.UI.OneShotFunctionTask;
using Responder = Sims3.UI.Responder;

namespace Gamefreak130.WonderPowersSpace.Helpers
{
    public class WonderPower : IWeightable
	{
		private readonly MethodInfo mRunMethod;

		private readonly int mCost;

		public string WonderPowerName { get; private set; }

		public bool IsBadPower { get; private set; }

		public int Cost
		{
			get
			{
				float cost = mCost;
				if (Household.ActiveHousehold is not null)
				{
					foreach (Sim current in Household.ActiveHousehold.Sims)
					{
						if (!IsBadPower && current.SimDescription.TraitManager.HasElement(TraitNames.Good))
						{
							cost *= TunableSettings.kGoodTraitKarmaDiscount;
						}
						if (IsBadPower && current.SimDescription.TraitManager.HasElement(TraitNames.Evil))
						{
							cost *= TunableSettings.kEvilTraitKarmaDiscount;
						}
					}
				}
				return (int)cost;
			}
		}

		public float Weight => Cost;

        public WonderPower(string name, bool isBad, int cost, MethodInfo runMethod)
		{
			WonderPowerName = name;
			IsBadPower = isBad;
			mCost = cost;
			mRunMethod = runMethod;
		}

		// The Run() method is used in a OneShotFunctionWithParam added to the Simulator when the power selection dialog ends,
		// So that any power-specific dialogs should not fire until after the power selection dialog is disposed
		// Hence why the boolean argument here is an object -- FunctionWithParam delegates take a generic object as their argument
		public void Run(object isBacklash)
        {
            if (isBacklash is bool backlash)
            {
				try
				{
					WonderPowerManager.StopPowerSting();
					Gameflow.SetGameSpeed(Normal, Gameflow.SetGameSpeedContext.Gameplay);
                    // Activation of any power will disable the karma menu
                    // Re-enabling is left to the powers' individual run methods when activation is complete
                    WonderPowerManager.TogglePowerRunning();
					int cost = backlash ? -Cost : Cost;
					WonderPowerManager.Karma -= cost;
					HudExtender.StartSpendEffects();
					Func<bool, bool> run = (Func<bool, bool>)Delegate.CreateDelegate(typeof(Func<bool, bool>), mRunMethod);
                    if (!run(backlash))
                    {
						if (backlash)
                        {
							PlumbBob.SelectedActor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString("BacklashFailure"), StyledNotification.NotificationStyle.kGameMessagePositive);
                        }
						else
                        {
							StyledNotification.Show(new(WonderPowerManager.LocalizeString("PowerFailure"), StyledNotification.NotificationStyle.kSystemMessage));
							WonderPowerManager.Karma += Cost;
							HudExtender.StartSpendEffects();
						}
						WonderPowerManager.TogglePowerRunning();
					}
				}
                catch (Exception e)
                {
                    // Since this is a Task, NRaas ErrorTrap can't catch any exceptions that occur
                    // So we'll do it live, f*** it! I'll write it, and we'll do it live!
                    PowerExceptionLogger.sInstance.Log(e);
                    if (!backlash)
                    {
						WonderPowerManager.Karma += Cost;
						HudExtender.StartSpendEffects();
					}
					WonderPowerManager.TogglePowerRunning();
				}
            }
        }
    }

	[Persistable]
	public sealed class WonderPowerManager
	{
		/*private bool mHaveShownFirstWishFulfillmentDialog;

		private bool mHaveShownWitchingHourDialog;*/

		[PersistableStatic(true)]
		private static WonderPowerManager sInstance;
		
		[PersistableStatic(false)]
		private static bool sIsPowerRunning;

		[PersistableStatic(false)]
		private static bool sIsBacklashRunning;

		private static readonly List<WonderPower> sAllWonderPowers = new();

		private static uint sStingHandle;

		private int mCurrentKarmaLevel = TunableSettings.kInitialKarmaLevel;

        public static int Karma
        {
            get => sInstance.mCurrentKarmaLevel;
            set => sInstance.mCurrentKarmaLevel = MathUtils.Clamp(value, -100, 100);
        }

		public static bool IsPowerRunning
		{
			get => sIsPowerRunning;
			private set
			{
				if (RewardTraitsPanel.Instance?.GetChildByID(799350305u, true) is Button button)
				{
					button.Enabled = !value;
					button.TooltipText = value ? LocalizeString("PowerIsDeploying") : "";
				}
				sIsPowerRunning = value;
			}
		}

		public static void TogglePowerRunning() => TogglePowerRunning(true);

		internal static int OnToggleAvailabilityCommand(object[] _)
		{
			if (IsPowerRunning)
			{
				TogglePowerRunning(false);
			}
			return 1;
		}

		private static void TogglePowerRunning(bool tryStartBacklash)
        {
			IsPowerRunning = !IsPowerRunning;
			if (tryStartBacklash && !IsPowerRunning && GameStates.GetInWorldSubState() is LiveModeState && (GameStates.sSingleton.mInWorldState.mBaseCallFlag & StateMachineState.BaseCallFlag.kShutdown) == 0u)
			{
				Simulator.AddObject(new OneShotFunctionTask(TryStartBacklash));
			}
		}

		public static void PlayPowerSting(string stingName) => sStingHandle = Audio.StartSound(stingName);

		public static void PlayPowerSting(string stingName, Vector3 sourcePosition) => sStingHandle = Audio.StartSound(stingName, sourcePosition);

		public static void PlayPowerSting(string stingName, ObjectGuid sourceObject, bool loop = false) => sStingHandle = Audio.StartObjectSound(sourceObject, stingName, loop);

		public static void StopPowerSting()
        {
			if (sStingHandle != 0)
			{
				Audio.StopSound(sStingHandle);
				sStingHandle = 0;
			}
		}

        private static void TryStartBacklash()
        {
			if (sIsBacklashRunning || Karma >= 0)
            {
				sIsBacklashRunning = false;
				return;
            }
			StopPowerSting();
			WonderPower power = ChooseBacklashPower();
			if (power is not null && RandomUtil.RandomChance(TunableSettings.kBacklashBaseChance - (Karma * TunableSettings.kBacklashChanceIncreasePerKarmaPoint)))
            {
				KarmicBacklashDialog.Show(false);
				sIsBacklashRunning = true;
				power.Run(true);
            }
			else
            {
				KarmicBacklashDialog.Show(true);
			}
        }

		private static WonderPower ChooseBacklashPower()
		{
			List<WonderPower> validBacklashPowers = sAllWonderPowers.FindAll(power => power.IsBadPower && power.Cost <= -Karma);
			return validBacklashPowers.Count > 0 ? RandomUtil.GetWeightedRandomObjectFromList(validBacklashPowers) : null;
		}

		/*private WonderPowerManager()
		{
			LoadDialogFlagsFromProfile();
		}

		private static void LoadDialogFlagsFromProfile()
		{
			byte[] section = ProfileManager.GetSection((uint)ProfileManager.GetCurrentPrimaryPlayer(), 5u);
			if (section.Length == 2)
			{
				bHaveShownFirstWishFulfillmentDialog = section[0] > 0;
				bHaveShownWitchingHourDialog = section[1] > 0;
			}
		}

		private static void SaveDialogFlagsToProfile()
		{
			byte[] array = new byte[2]
			{
				(byte)(bHaveShownFirstWishFulfillmentDialog ? 1 : 0),
				(byte)(bHaveShownWitchingHourDialog ? 1 : 0)
			};
			ProfileManager.UpdateSection((uint)ProfileManager.GetCurrentPrimaryPlayer(), 5u, array);
		}*/

        /*private void DisplayWitchingHourDialogPopup()
		{
			SimpleMessageDialog.Show(Localization.LocalizeString("UI/Wondermode/PopupDialog:HourOfReckoningTitle"), Localization.LocalizeString("UI/Wondermode/PopupDialog:HourOfReckoningText"));
			sInstance.mHaveShownWitchingHourDialog = true;
			SaveDialogFlagsToProfile();
		}

		private void DisplayWishFulfilledDialogPopup()
		{
			SimpleMessageDialog.Show(Localization.LocalizeString("UI/Wondermode/PopupDialog:FirstWishFulfillmentTitle"), Localization.LocalizeString("UI/Wondermode/PopupDialog:FirstWishFulfillmentText"));
			sInstance.mHaveShownFirstWishFulfillmentDialog = true;
			SaveDialogFlagsToProfile();
		}*/

		internal static void Init()
		{
			if (!GameStates.IsTravelling)
			{
				sInstance = new();
			}
			sIsPowerRunning = false;
			sIsBacklashRunning = false;
			sStingHandle = 0;
		}

		internal static void AddPower(WonderPower s)
		{
			if (!sAllWonderPowers.Any(power => power.WonderPowerName.Equals(s.WonderPowerName, StringComparison.InvariantCultureIgnoreCase)))
			{
				sAllWonderPowers.Add(s);
			}
		}

		public static bool HasEnoughKarma(int cost) => Karma - cost >= -100;

        public static ReadOnlyCollection<WonderPower> WonderPowerList => sAllWonderPowers.AsReadOnly();

        internal static string LocalizeString(string name, params object[] parameters) => Localization.LocalizeString($"Gameplay/WonderMode:{name}", parameters);

		internal static string LocalizeString(bool isFemale, string name, params object[] parameters) => Localization.LocalizeString(isFemale, $"Gameplay/WonderMode:{name}", parameters);
    }

	public static class HelperMethods
    {
		public static List<GameObject> CreateFogEmittersOnLot(Lot lot)
		{
			Vector2 lotDimensions = BinCommon.GetLotDimensions(lot);
			LotDisplayLevelInfo lotDisplayLevelInfo = World.LotGetDisplayLevelInfo(lot.LotId);
			float num = 0.01f * lotDimensions.x * lotDimensions.y * (lotDisplayLevelInfo.mMax - lotDisplayLevelInfo.mMin);
			RandomObjectPlacementParams ropParams = new(true, true);
			ropParams.ValidFloors = new sbyte[lotDisplayLevelInfo.mMax - lotDisplayLevelInfo.mMin];
			for (int i = 0; i < ropParams.ValidFloors.Length; i++)
			{
				ropParams.ValidFloors[i] = (sbyte)(i + lotDisplayLevelInfo.mMin);
			}
			ropParams.FglParams.BooleanConstraints &= ~FindGoodLocationBooleans.PreferEmptyTiles;
			List<GameObject> list = new();
			for (int i = 0; i < (int)num; i++)
			{
				if (CreateObjectOnLot("fogEmitter", ProductVersion.BaseGame, null, lot, ropParams, true) is not GameObject gameObject)
				{
					break;
				}
				if (gameObject.IsOutside)
				{
					Vector3 position = gameObject.Position;
					gameObject.SetPosition(new(position.x, World.GetTerrainHeight(position.x, position.z), position.z));
				}
				gameObject.OnHandToolPlacementOnTerrainBase();
				list.Add(gameObject);
			}
			return list;
		}

		public static SimDescription SelectTarget(IEnumerable<SimDescription> sims, string title)
		{
			SimDescription target = null;
			if (sims?.Count() > 0)
			{
				List<ObjectPicker.HeaderInfo> list = new()
				{
					new("Ui/Caption/ObjectPicker:Name", "Ui/Tooltip/ObjectPicker:Name", 500)
				};

				List<ObjectPicker.RowInfo> list2 = sims.Select(simDescription => {
					return new ObjectPicker.RowInfo(simDescription, new()
					{
						new ObjectPicker.ThumbAndTextColumn(simDescription.GetThumbnailKey(ThumbnailSize.Large, 0), simDescription.FullName)
					});
				}).ToList();

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

		public static Lot SelectTarget(string title, string confirm)
		{
			List<IMapTagPickerInfo> list = LotManager.AllLotsWithoutCommonExceptions
													 .Cast<Lot>()
													 .Where(lot => lot.CommercialLotSubType is not CommercialLotSubType.kEP1_HiddenTomb)
													 .Select(lot => new MapTagPickerLotInfo(lot, lot.IsPlayerHomeLot ? MapTagType.HomeLot
																													 : lot.IsResidentialLot ? MapTagType.NeighborLot
																													 : MapTagType.Venue))
													 .Cast<IMapTagPickerInfo>().ToList();

			IMapTagPickerInfo info = MapTagPickerUncancellable.Show(list, title, confirm);
			return LotManager.GetLot(info.LotId);
		}

		public static void IntegrateMasterControllerCAS(SimDescription sim)
        {
			SimDescription.DeathType deathType = sim.DeathStyle;

			Type casBase = Type.GetType("NRaas.MasterControllerSpace.Sims.CASBase, NRaasMasterController");
			// Generate OnGetMode delegate using generics and reflection
			Type onGetModeType = Type.GetType("NRaas.MasterControllerSpace.Sims.CASBase+OnGetMode, NRaasMasterController");
			MethodInfo getMode = typeof(HelperMethods).GetMethod(nameof(GetInvalidCASMode), BindingFlags.NonPublic | BindingFlags.Static);
			getMode = getMode.MakeGenericMethod(Type.GetType("NRaas.MasterControllerSpace.Sims.CASBase+EditType, NRaasMasterController"));
			object getModeDelegate = Delegate.CreateDelegate(onGetModeType, getMode);
			// Put it all together to set up MasterController CAS
			ReflectionEx.StaticInvoke(casBase, "Perform", new[] { sim, getModeDelegate }, new[] { typeof(SimDescription), onGetModeType });

			// Reversing outfit and death style changes made due to CASMode not being set to Full
			sim.SetDeathStyle(deathType, true);
			FieldInfo swapOutfits = casBase.GetField("sSwapOutfits", BindingFlags.NonPublic | BindingFlags.Static);
			FieldInfo actualCategory = casBase.GetField("sActualCategory", BindingFlags.NonPublic | BindingFlags.Static);
			if (swapOutfits.GetValue(null) is true && actualCategory.GetValue(null) is not OutfitCategories.Naked)
			{
				object value = sim.Outfits[actualCategory.GetValue(null)];
				sim.Outfits[actualCategory.GetValue(null)] = sim.Outfits[OutfitCategories.Everyday];
				sim.Outfits[OutfitCategories.Everyday] = value;

				actualCategory.SetValue(null, OutfitCategories.Everyday);
				casBase.GetField("sActualIndex", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 0);
				swapOutfits.SetValue(null, false);
			}
		}

#pragma warning disable IDE0060 // Remove unused parameter

		/// <summary>
		/// Method matching delegate type "NRaas.MasterControllerSpace.Sims.CASBase.OnGetMode" used to delay CAS mode state transition when setting up MasterController's CAS
		/// </summary>
		/// <remarks>
		/// <typeparamref name="T"/> should ALWAYS be of type "NRaas.MasterControllerSpace.Sims.CASBase.EditType". The only reason this is generic is to let us substitute the type in at runtime using reflection
		/// </remarks>
		/// <typeparam name="T">Should always be of type "NRaas.MasterControllerSpace.Sims.CASBase.EditType"</typeparam>
		/// <returns>An invalid <see cref="CASMode"/>, allowing "NRaas.MasterControllerSpace.Sims.CASBase.Perform" to run without automatically transitioning to CAS</returns>
		private static CASMode GetInvalidCASMode<T>(SimDescription _, ref OutfitCategories __, ref int ___, ref T ____) where T : Enum
			=> (CASMode)(-1);

#pragma warning restore IDE0060 // Remove unused parameter
	}

	public class TransmogrifyTraitMapping
	{
		private readonly UUGraph<TraitNames> mGraph = new();

		public static readonly TransmogrifyTraitMapping sInstance = new();

		private TransmogrifyTraitMapping()
		{
		}

		public static void Init() => sInstance.ParseMappingData("TransmogrifyTraitMapping");

		private void ParseMappingData(string xmlName)
		{
			XmlDbData xmlDbData = XmlDbData.ReadData(xmlName);
			XmlDbTable xmlDbTable = null;
			xmlDbData?.Tables.TryGetValue("TraitMapping", out xmlDbTable);
			if (xmlDbTable is not null)
			{
				foreach (XmlDbRow row in xmlDbTable.Rows)
				{
					if (row.TryGetEnum("TraitName", out TraitNames trait, TraitNames.Unknown))
					{
						if (!mGraph.ContainsNode(trait))
						{
							mGraph.AddNode(trait);
						}
						if (ParserFunctions.TryParseCommaSeparatedList(row["MapsTo"], out List<TraitNames> mappingTraits, TraitNames.Unknown))
						{
							foreach (TraitNames mappingTrait in mappingTraits)
							{
								if (!mGraph.ContainsNode(mappingTrait))
								{
									mGraph.AddNode(mappingTrait);
								}
								if (!mGraph.ContainsEdge(trait, mappingTrait))
								{
									mGraph.AddEdge(trait, mappingTrait);
								}
							}
						}
					}
				}
			}
		}

        public IEnumerable<TraitNames> GetMappedTraits(SimDescription sim, TraitNames trait)
			=> mGraph.ContainsNode(trait)
                ? from mappedTrait in mGraph.GetNeighbors(trait)
                  where TraitManager.GetTraitFromDictionary(mappedTrait).TraitValidForAgeSpecies(sim.GetCASAGSAvailabilityFlags()) && sim.TraitManager.CanAddTrait((ulong)mappedTrait)
                  select mappedTrait
                : new TraitNames[0];
    }

	public class CASInstantBeautyState : CASFullModeState
    {
		public CASInstantBeautyState() : base()
        {
			mStateId = (int)HashString32("CASInstantBeautyState");
			mStateName = "CAS Wonder Mode -- Instant Beauty";
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
				if (CASCharacterSheet.gSingleton is not null)
				{
					UIHelper.HideElementById(CASCharacterSheet.gSingleton, (uint)CASCharacterSheet.ControlIDs.CharacterButton);
					UIHelper.HideElementById(CASCharacterSheet.gSingleton, (uint)CASCharacterSheet.ControlIDs.CharacterText);
					UIHelper.HideElementById(CASCharacterSheet.gSingleton, (uint)CASCharacterSheet.ControlIDs.RandomizeButton);
				}

				if (CASBasics.gSingleton?.GetChildByID((uint)CASBasics.ControlIDs.HumanBasicsWindow, true) is WindowBase window)
                {
					for (uint i = 0; i < 5; i++)
					{
						UIHelper.HideElementByIndex(window, i);
					}
					WindowBase window2 = window.GetChildByIndex(5);
					if (window2 is not null)
					{
						window2.Area = new(new(window2.Area.TopLeft.x, 80), new(window2.Area.BottomRight.x, 80));
					}
					window2 = window.GetChildByIndex(6);
					if (window2 is not null)
					{
						window2.Area = new(new(window2.Area.TopLeft.x, 170), new(window2.Area.BottomRight.x, 170));
					}
				}

				if (CAPPetSheet.gSingleton is not null)
                {
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.RandomizeButton);
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.BasicsButton);
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.BasicsFButton);
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.BasicsText);
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.AccessoriesButton);
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.AccessoriesText);
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.CharacterButton);
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.CharacterText);
				}

				WindowBase panel = CAPPetSheet.gSingleton?.mSpeciesButtonPanels?
														  .Where(kvp => kvp.Key == Responder.Instance?.CASModel?.Species)
														  .Select(kvp => kvp.Value)
														  .FirstOrDefault();
				if (panel is not null)
				{
					UIHelper.HideElementById(panel, (uint)CAPPetSheet.ControlIDs.AccessoriesButton);
				}

				if (CAPBreeds.gSingleton is not null)
				{
					UIHelper.HideElementById(CAPBreeds.gSingleton, (uint)CAPBreeds.ControlIDs.DogButtonPanel);
				}

				if (CASPuck.Instance is CASPuck puck)
                {
					UIHelper.HideElementById(puck, (uint)CASPuck.ControlIDs.CloseButton);
					if (puck.GetChildByID((uint)CASPuck.ControlIDs.OptionsButton, true) is Button button2)
                    {
						button2.Click -= puck.OnOptionsClick;
						button2.Click -= CASPuckExtender.OnOptionsClick;
						button2.Click += CASPuckExtender.OnOptionsClick;
                    }
                }
			}
		}
    }

	public class CASTransmogrifyState : CASFullModeState
	{
		public CASTransmogrifyState() : base()
		{
			mStateId = (int)HashString32("CASTransmogrifyState");
			mStateName = "CAS Wonder Mode -- Transmogrify";
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
				if (CASCharacterSheet.gSingleton is not null)
				{
					UIHelper.HideElementById(CASCharacterSheet.gSingleton, (uint)CASCharacterSheet.ControlIDs.RandomizeButton);
				}

				if (CASBasics.gSingleton is not null)
				{
					UIHelper.HideElementById(CASBasics.gSingleton, (uint)CASBasics.ControlIDs.RandomizeNameButton);
					UIHelper.HideElementById(CASBasics.gSingleton, (uint)CASBasics.ControlIDs.GhostBasicsButton);
					if (CASBasics.gSingleton.GetChildByID((uint)CASBasics.ControlIDs.HumanBasicsWindow, true) is WindowBase window)
					{
						for (uint i = 0; i < 5; i++)
						{
							UIHelper.HideElementByIndex(window, i);
						}
						WindowBase window2 = window.GetChildByIndex(5);
						if (window2 is not null)
						{
							window2.Area = new(new(window2.Area.TopLeft.x, 80), new(window2.Area.BottomRight.x, 80));
						}
						window2 = window.GetChildByIndex(6);
						if (window2 is not null)
						{
							window2.Area = new(new(window2.Area.TopLeft.x, 170), new(window2.Area.BottomRight.x, 170));
						}
					}
				}

				if (CASCharacter.gSingleton is not null)
				{
					if (CASCharacter.gSingleton.GetChildByID((uint)CASCharacter.ControlIDs.TraitsWindow, true) is WindowBase window)
					{
						UIHelper.HideElementById(window, (uint)CASCharacter.TraitsControlId.Randomize);
						UIHelper.HideElementById(window, (uint)CASCharacter.TraitsControlId.ShowAddTraitsButton);
						UIHelper.DisableElementById(window, (uint)CASCharacter.TraitsControlId.ExternalTraitIcon1);
						UIHelper.DisableElementById(window, (uint)CASCharacter.TraitsControlId.ExternalTraitIcon2);
						UIHelper.DisableElementById(window, (uint)CASCharacter.TraitsControlId.ExternalTraitIcon3);
						UIHelper.DisableElementById(window, (uint)CASCharacter.TraitsControlId.ExternalTraitIcon4);
						UIHelper.DisableElementById(window, (uint)CASCharacter.TraitsControlId.ExternalTraitIcon5);
					}
					if (CASCharacter.gSingleton.GetChildByID((uint)CASCharacter.ControlIDs.VoicesWindow, true) is WindowBase window2)
					{
						UIHelper.DisableElementById(window2, (uint)CASCharacter.VoiceControlId.VoiceSlider);
					}
				}

				if (CAPPetSheet.gSingleton is not null)
				{
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.RandomizeButton);
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.BasicsButton);
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.BasicsFButton);
					UIHelper.HideElementById(CAPPetSheet.gSingleton, (uint)CAPPetSheet.ControlIDs.BasicsText);
				}

				if (CAPBreeds.gSingleton is not null)
                {
					UIHelper.HideElementById(CAPBreeds.gSingleton, (uint)CAPBreeds.ControlIDs.DogButtonPanel);
                }

				if (CAPCharacter.gSingleton is not null)
                {
					if (CAPCharacter.gSingleton.GetChildByID((uint)CAPCharacter.ControlIDs.TraitsWindow, true) is WindowBase window)
                    {
						UIHelper.HideElementById(window, (uint)CAPCharacter.TraitsControlId.Randomize);
						UIHelper.HideElementById(window, (uint)CAPCharacter.TraitsControlId.ShowAddTraitsButton);
						UIHelper.DisableElementById(window, (uint)CAPCharacter.TraitsControlId.ExternalTraitIcon1);
						UIHelper.DisableElementById(window, (uint)CAPCharacter.TraitsControlId.ExternalTraitIcon2);
						UIHelper.DisableElementById(window, (uint)CAPCharacter.TraitsControlId.ExternalTraitIcon3);
						UIHelper.DisableElementById(window, (uint)CAPCharacter.TraitsControlId.ExternalTraitIcon4);
						UIHelper.DisableElementById(window, (uint)CAPCharacter.TraitsControlId.ExternalTraitIcon5);
					}
					if (CAPCharacter.gSingleton.GetChildByID((uint)CAPCharacter.ControlIDs.VoicesWindow, true) is WindowBase window2)
                    {
						UIHelper.DisableElementById(window2, (uint)CAPCharacter.VoiceControlId.VoiceSlider);
					}
                } 

				if (CASPuck.Instance is CASPuck puck)
				{
					UIHelper.HideElementById(puck, (uint)CASPuck.ControlIDs.CloseButton);
					if (puck.GetChildByID((uint)CASPuck.ControlIDs.OptionsButton, true) is Button button2)
					{
						button2.Click -= puck.OnOptionsClick;
						button2.Click -= CASPuckExtender.OnOptionsClick;
						button2.Click += CASPuckExtender.OnOptionsClick;
					}
				}
			}
		}
	}
}