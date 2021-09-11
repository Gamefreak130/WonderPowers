using Gamefreak130.Common.Helpers;
using Gamefreak130.WonderPowersSpace.Buffs;
using Gamefreak130.WonderPowersSpace.Interactions;
using Gamefreak130.WonderPowersSpace.Situations;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Objects.Appliances;
using Sims3.Gameplay.Objects.Beds;
using Sims3.Gameplay.Objects.Environment;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Objects.Miscellaneous;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.Enums;
using Sims3.UI;
using Sims3.UI.CAS;
using System;
using System.Collections.Generic;
using System.Linq;
using static Sims3.Gameplay.GlobalFunctions;
using static Sims3.SimIFace.ResourceUtils;
using OneShotFunctionTask = Sims3.UI.OneShotFunctionTask;
using Queries = Sims3.Gameplay.Queries;

namespace Gamefreak130.WonderPowersSpace.Helpers
{
    public static class ActivationMethods
	{
		public static bool CryHavocActivation(bool _)
		{
			Sim sim = PlumbBob.SelectedActor;
			Lot lot = sim.LotCurrent.IsWorldLot
				? GetClosestObject(LotManager.AllLotsWithoutCommonExceptions.Cast<Lot>(), sim)
				: sim.LotCurrent;

			new CryHavocSituation(lot);
			return true;
		}

		public static bool CurseActivation(bool isBacklash)
		{
			Sim selectedSim = null;
			// CONSIDER Pets?
			if (isBacklash)
			{
				List<Sim> validSims = Household.ActiveHousehold.Sims.FindAll(sim => sim.SimDescription.TeenOrAbove && !sim.BuffManager.HasElement((BuffNames)HashString64("Gamefreak130_CursedBuff")));
				if (validSims.Count > 0)
				{
					selectedSim = RandomUtil.GetRandomObjectFromList(validSims);
				}
			}
			else
			{
				IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetSims(sim => sim.SimDescription.TeenOrAbove && !sim.BuffManager.HasElement((BuffNames)HashString64("Gamefreak130_CursedBuff")))
													  select sim.SimDescription;
				selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("CurseDialogTitle"))?.CreatedSim;
			}

			if (selectedSim is null)
			{
				return false;
			}

			Camera.FocusOnSim(selectedSim);
			if (selectedSim.IsSelectable)
			{
				PlumbBob.SelectActor(selectedSim);
			}
			InteractionInstance instance = new BeCursed.Definition().CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false);
			if (!instance.Test())
			{ 
				return false; 
			} 
			selectedSim.InteractionQueue.AddNext(instance);
			return true;
		}

		public static bool DivineInterventionActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from urnstone in Queries.GetObjects<Urnstone>()
												  select urnstone.DeadSimsDescription into simDescription
												  where simDescription is not null
												  select simDescription;
			Urnstone selectedUrnstone = Urnstone.FindGhostsGrave(HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("DivineInterventionDialogTitle")));
			if (selectedUrnstone is null)
			{
				return false;
			}
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
			Camera.FocusOnSim(ghost);
			if (ghost.IsSelectable)
			{
				PlumbBob.SelectActor(ghost);
			}
			InteractionInstance instance = new DivineInterventionResurrect.Definition().CreateInstance(ghost, ghost, new(InteractionPriorityLevel.MaxDeath), false, false);
			if (!instance.Test())
			{
				return false;
			}
			ghost.InteractionQueue.AddNext(instance);
			return true;
		}

		public static bool DoomActivation(bool isBacklash)
		{
			Sim selectedSim = null;
			if (isBacklash)
			{
				List<Sim> validSims = Household.ActiveHousehold.AllActors.FindAll(sim => sim.SimDescription.ChildOrAbove && sim.BuffManager.GetElement(BuffNames.UnicornsIre)?.mBuffName != "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DoomBuff");
				if (validSims.Count > 0)
				{
					selectedSim = RandomUtil.GetRandomObjectFromList(validSims);
				}
			}
			else
			{
				IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors()
													  where sim.SimDescription.ChildOrAbove && sim.BuffManager.GetElement(BuffNames.UnicornsIre)?.mBuffName != "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DoomBuff"
													  select sim.SimDescription;
				selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("DoomDialogTitle"))?.CreatedSim;
			}

			if (selectedSim is null)
			{
				return false;
			}

			Camera.FocusOnSim(selectedSim);
			if (selectedSim.IsSelectable)
			{
				PlumbBob.SelectActor(selectedSim);
			}
			InteractionInstance instance = new BeDoomed.Definition().CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false);
			if (!instance.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(instance);
			return true;
		}

		public static bool EarthquakeActivation(bool _)
		{
			Sim actor = PlumbBob.SelectedActor;
			Lot lot = actor.LotCurrent.IsWorldLot
				? GetClosestObject(LotManager.AllLotsWithoutCommonExceptions.Cast<Lot>(), actor)
				: actor.LotCurrent;

			Audio.StartSound("earthquake_shake", lot.Position);
			WonderPowerManager.PlayPowerSting("sting_earthquake");
			Camera.FocusOnLot(lot.LotId, 2f); //2f is standard lerptime
			CameraController.Shake(FireFightingJob.kEarthquakeCameraShakeIntensity, FireFightingJob.kEarthquakeCameraShakeDuration);

			AlarmHandle handle = AlarmHandle.kInvalidHandle;
			try
			{
				lot.AddAlarm(FireFightingJob.kEarthquakeTimeUntilTNS, TimeUnit.Minutes, WonderPowerManager.TogglePowerRunning, "Gamefreak130 wuz here -- Activation complete alarm", AlarmType.AlwaysPersisted);

				foreach (Sim sim in lot.GetAllActors())
				{
					if (sim.IsPet)
					{
						PetStartleBehavior.StartlePet(sim, StartleType.Invalid, (Origin)HashString64("FromWonderPower"), lot, true, PetStartleReactionType.NoReaction, new(InteractionPriorityLevel.CriticalNPCBehavior));
					}
					else
					{
						InteractionInstance instance = new EarthquakePanicReact.Definition().CreateInstance(sim, sim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false);
						instance.Hidden = true;
						sim.InteractionQueue.AddNext(instance);
					}
				}

				List<GameObject> breakableObjects = lot.GetObjects<GameObject>(@object => @object.Repairable is { Broken: false });
				int maxBroken = Math.Min(RandomUtil.GetInt(TunableSettings.kEarthquakeMinBroken, TunableSettings.kEarthquakeMaxBroken), breakableObjects.Count);
				for (int i = 0; i < maxBroken; i++)
				{
					if (breakableObjects.Count == 0)
					{ break; }

					GameObject @object = RandomUtil.GetRandomObjectFromList(breakableObjects);
					@object.Repairable.BreakObject();
					breakableObjects.Remove(@object);
				}
				int maxTrash = RandomUtil.GetInt(TunableSettings.kEarthquakeMinTrash, TunableSettings.kEarthquakeMaxTrash);
				for (int i = 0; i < maxTrash; i++)
				{
					Vector3 randomPosition = lot.GetRandomPosition(true, true);
					TrashPile trashPile = CreateObjectOutOfWorld("TrashPileIndoor") as TrashPile;
					World.FindGoodLocationParams fglParams = new(randomPosition);
					if (PlaceAtGoodLocation(trashPile, fglParams, true))
					{
						trashPile.AddToWorld();
					}
				}
			}
			catch
			{
				if (handle != AlarmHandle.kInvalidHandle)
				{
					lot.RemoveAlarm(handle);
				}
				throw;
			}
			PlumbBob.SelectedActor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(PlumbBob.SelectedActor.IsFemale, "EarthquakeTNS", PlumbBob.SelectedActor), StyledNotification.NotificationStyle.kGameMessageNegative);
			return true;
		}

		public static bool FeralPossessionActivation(bool _)
		{
			Sim sim = PlumbBob.SelectedActor;
			Lot lot = sim.LotCurrent.IsWorldLot
				? GetClosestObject(LotManager.AllLotsWithoutCommonExceptions.Cast<Lot>(), sim)
				: sim.LotCurrent;

			new FeralPossessionSituation(lot);
			return true;
		}

		public static bool FireActivation(bool isBacklash)
		{
			Lot selectedLot;
			if (isBacklash)
			{
				Sim actor = PlumbBob.SelectedActor;
				selectedLot = actor.LotCurrent.IsWorldLot
					? GetClosestObject(LotManager.AllLotsWithoutCommonExceptions.Cast<Lot>(), actor)
					: actor.LotCurrent;
			}
			else
			{
				selectedLot = HelperMethods.SelectTarget(WonderPowerManager.LocalizeString("FireDestinationTitle"), WonderPowerManager.LocalizeString("FireDestinationConfirm"));
			}

			new FireSituation(selectedLot);
			return true;
		}

		public static bool GhostifyActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors()
												  where sim.SimDescription.ChildOrAbove && !sim.IsGhostOrHasGhostBuff
												  select sim.SimDescription;
			Sim selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("GhostifyDialogTitle"))?.CreatedSim;
			if (selectedSim is null)
			{
				return false;
			}

			SimDescription.DeathType ghostType = SimDescription.DeathType.None;
			if (selectedSim.IsPet)
			{
				ghostType = RandomUtilEx.CoinFlipSelect(SimDescription.DeathType.PetOldAgeBad, SimDescription.DeathType.PetOldAgeGood);
			}
			else if (selectedSim.IsEP11Bot)
			{
				ghostType = SimDescription.DeathType.Robot;
			}
			else
			{
				List<ObjectPicker.HeaderInfo> list = new()
				{
					new("Ui/Caption/ObjectPicker:Ghost", "Ui/Caption/ObjectPicker:Ghost", 300)
				};

				List<ObjectPicker.RowInfo> list2 = Ghostify.sHumanDeathTypes.Select((deathType, i) => {
					return new ObjectPicker.RowInfo(deathType, new()
					{
						new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(ResourceKey.CreatePNGKey(CASBasics.mGhostDeathNames[i], 0u), ThumbnailSize.ExtraLarge), Urnstone.DeathTypeToLocalizedString(deathType))
					});
				}).ToList();

				List<ObjectPicker.TabInfo> list3 = new()
				{
					new("shop_all_r2", WonderPowerManager.LocalizeString("SelectGhost"), list2)
				};

				while (ghostType is SimDescription.DeathType.None)
				{
					List<ObjectPicker.RowInfo> selection = ObjectPickerDialog.Show(true, ModalDialog.PauseMode.PauseSimulator, WonderPowerManager.LocalizeString("GhostifyDialogTitle"), Localization.LocalizeString("Ui/Caption/ObjectPicker:OK"),
																					Localization.LocalizeString("Ui/Caption/ObjectPicker:Cancel"), list3, list, 1);
					ghostType = selection is not null ? (SimDescription.DeathType)selection[0].Item : SimDescription.DeathType.None;
				}
			}

			Ghostify ghostifyInteraction = new Ghostify.Definition(ghostType).CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as Ghostify;
			if (!ghostifyInteraction.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(ghostifyInteraction);
			return true;
		}

		public static bool GhostsActivation(bool isBacklash)
		{
			Lot selectedLot;
			if (isBacklash)
			{
				Sim actor = PlumbBob.SelectedActor;
				selectedLot = actor.LotCurrent.IsWorldLot
					? GetClosestObject(LotManager.AllLotsWithoutCommonExceptions.Cast<Lot>(), actor)
					: actor.LotCurrent;
			}
			else
			{
				selectedLot = HelperMethods.SelectTarget(WonderPowerManager.LocalizeString("GhostsDestinationTitle"), WonderPowerManager.LocalizeString("GhostsDestinationConfirm"));
			}

			new GhostsSituation(selectedLot);
			return true;
		}

		public static bool GoodMoodActivation(bool _)
		{
			bool flag = false;
			Camera.FocusOnSelectedSim();
			foreach (Sim sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors())
			{
				ActivateGoodMood interaction = new ActivateGoodMood.Definition().CreateInstance(sim, sim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as ActivateGoodMood;
				if (!interaction.Test())
				{
					continue;
				}
				sim.InteractionQueue.AddNext(interaction);
				flag = true;
			}
			if (flag)
			{
				WonderPowerManager.PlayPowerSting("sting_goodmood");
				PlumbBob.SelectedActor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(PlumbBob.SelectedActor.IsFemale, "GoodMoodTNS", PlumbBob.SelectedActor), StyledNotification.NotificationStyle.kGameMessagePositive);
				WonderPowerManager.TogglePowerRunning();
				return true;
			}
			return false;
		}

		public static bool InstantBeautyActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors()
												  where (sim.IsPet && sim.SimDescription.AdultOrAbove) || (sim.SimDescription.ToddlerOrAbove && !sim.OccultManager.DisallowClothesChange() && !sim.BuffManager.DisallowClothesChange())
												  select sim.SimDescription;
			Sim selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("InstantBeautyDialogTitle"))?.CreatedSim;
			if (selectedSim is null)
			{
				return false;
			}
			Beautify beautify = new Beautify.Definition().CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as Beautify;
			if (!beautify.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(beautify);
			return true;
		}

		public static bool LuckyBreakActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors()
												  where sim.SimDescription.ChildOrAbove && sim.BuffManager.GetElement(BuffNames.UnicornsBlessing)?.mBuffName != "Gameplay/Excel/Buffs/BuffList:Gamefreak130_LuckyBreakBuff"
												  select sim.SimDescription;
			Sim selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("LuckyBreakDialogTitle"))?.CreatedSim;

			if (selectedSim is null)
			{
				return false;
			}
			ActivateLuckyBreak activateLuckyBreak = new ActivateLuckyBreak.Definition().CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as ActivateLuckyBreak;
			if (!activateLuckyBreak.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(activateLuckyBreak);
			return true;
		}

		public static bool LuckyFindActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors()
												  where (sim.IsPet || GameUtils.IsInstalled(ProductVersion.EP7)) && sim.SimDescription.TeenOrAbove && !sim.SimDescription.IsHorse && !sim.BuffManager.HasElement(BuffLuckyFind.kBuffLuckyFindGuid)
												  select sim.SimDescription;
			Sim selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("LuckyFindDialogTitle"))?.CreatedSim;

			if (selectedSim is null)
			{
				return false;
			}

			ActivateLuckyFind activateLuckyFind = new ActivateLuckyFind.Definition().CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as ActivateLuckyFind;
			if (!activateLuckyFind.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(activateLuckyFind);
			return true;
		}

		public static bool MeteorStrikeActivation(bool isBacklash)
		{
			Lot selectedLot;
			if (isBacklash)
			{
				Sim actor = PlumbBob.SelectedActor;
				selectedLot = actor.LotCurrent.IsWorldLot
					? GetClosestObject(LotManager.AllLotsWithoutCommonExceptions.Cast<Lot>(), actor)
					: actor.LotCurrent;
			}
			else
			{
				selectedLot = HelperMethods.SelectTarget(WonderPowerManager.LocalizeString("MeteorDestinationTitle"), WonderPowerManager.LocalizeString("MeteorDestinationConfirm"));
			}

			WonderPowerManager.PlayPowerSting("sting_meteor_forshadow");
			selectedLot.AddAlarm(30f, TimeUnit.Seconds, () => Camera.FocusOnLot(selectedLot.LotId, 2f), "Gamefreak130 wuz here -- Activation focus alarm", AlarmType.NeverPersisted);
			Meteor.TriggerMeteorEvent(selectedLot.GetRandomPosition(false, true));
			AlarmManager.Global.AddAlarm(Meteor.kMeteorLifetime + 3, TimeUnit.Minutes, WonderPowerManager.TogglePowerRunning, "Gamefreak130 wuz here -- Activation complete alarm", AlarmType.AlwaysPersisted, null);
			return true;
		}

		public static bool RayOfSunshineActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors()
												  where !sim.BuffManager.HasElement((BuffNames)HashString64("Gamefreak130_BoostedBuff"))
												  select sim.SimDescription;
			Sim selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("RayOfSunshineDialogTitle"))?.CreatedSim;
			if (selectedSim is null)
			{
				return false;
			}

			ActivateRayOfSunshine activateRayOfSunshine = new ActivateRayOfSunshine.Definition().CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as ActivateRayOfSunshine;
			if (!activateRayOfSunshine.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(activateRayOfSunshine);
			return true;
		}

		public static bool RepairActivation(bool _)
		{
			Lot target = PlumbBob.SelectedActor.LotHome;
			if (target is null)
			{
				return false;
			}
			// CONSIDER react broadcast?
			AlarmHandle handle = AlarmHandle.kInvalidHandle;
			try
			{
				Camera.FocusOnLot(target.LotId, 1f);
				handle = AlarmManager.Global.AddAlarm(2f, TimeUnit.Minutes, WonderPowerManager.TogglePowerRunning, "Gamefreak130 wuz here -- Activation complete alarm", AlarmType.AlwaysPersisted, null);
				WonderPowerManager.PlayPowerSting("sting_repair");
				if (target.GetSharedFridgeInventory() is SharedFridgeInventory inventory)
				{
					foreach (ISpoilable spoilable in new List<ISpoilable>(inventory.SpoiledFood))
					{
						spoilable.Unspoil();
						inventory.SpoiledFood.Remove(spoilable);
					}
				}
				foreach (GameObject gameObject in target.GetObjects<GameObject>())
				{
					bool playPoofEffect = false;
					if (!gameObject.InUse && gameObject.InWorld)
					{
						if (!target.IsJunkyardLot || !gameObject.IsOutside)
						{
							if (gameObject.Charred)
							{
								playPoofEffect = true;
								gameObject.Charred = false;
								if (gameObject is Windows)
								{
									RepairableComponent.CreateReplaceObject(gameObject);
								}
							}
							if (gameObject.Scratched)
							{
								playPoofEffect = true;
								gameObject.Scratched = false;
							}
							if (gameObject is ISnackBowl or ICatPrey or AshPile or Book or IDestroyOnMagicalCleanup or IThrowAwayable or { IsCleanable: true, Cleanable: { DirtyLevel: < 0 } })
							{
								playPoofEffect = true;
							}
							if (gameObject.Repairable is { Broken: true } repairable)
							{
								playPoofEffect = true;
								repairable.ForceRepaired(null);
							}
							if (gameObject is ISpoilable { IsSpoiled: true } spoilable)
							{
								playPoofEffect = true;
								spoilable.Unspoil();
							}
							if (gameObject is IFridge fridge)
							{
								playPoofEffect = true;
								fridge.StopFridgeFrontStinkVFX();
							}
							if (gameObject is Hamper { mCount: > 0 } hamper)
							{
								playPoofEffect = true;
								hamper.mCount = 0;
								hamper.UpdateVisualState();
							}
							if (gameObject is WashingMachine { mWashState: not WashingMachine.WashState.Empty } washer)
							{
								playPoofEffect = true;
								washer.SetObjectToReset();
								washer.RemoveClothes();
								washer.SetGeometryState("empty");
								if (washer.mSoundId != 0)
								{
									Audio.StopObjectSound(washer.ObjectId, washer.mSoundId);
									washer.mSoundId = 0U;
								}
							}
							if (gameObject is Dryer dryer)
							{
								if (dryer.CurDryerState is not Dryer.DryerState.Empty)
								{
									playPoofEffect = true;
								}
								dryer.ForceDryerDone();
								// This needs to be added to the simulator with a delay so that it runs after DryerFinishedOneShotTask and properly cleans up the dryer state
								Simulator.AddObject(new OneShotFunctionTask(delegate {
									dryer.TakeClothes(true, PlumbBob.SelectedActor.SimDescription.SimDescriptionId);
									if (dryer is DryerExpensive)
									{
										dryer.SetGeometryState("empty");
									}
								}, StopWatch.TickStyles.Seconds, 1f));
							}
							if (gameObject is Clothesline clothesline)
							{
								if (clothesline.CurClothesState is not Dryer.DryerState.Empty)
								{
									playPoofEffect = true;
								}
								if (clothesline.CurClothesState is Dryer.DryerState.Running)
								{
									clothesline.ForceClothesDry();
								}
								clothesline.ClothesTaken();
							}
							if (gameObject is Fire fire)
							{
								VisualEffect visualEffect = VisualEffect.Create("ep5UnicornRain");
								visualEffect.SetPosAndOrient(fire.Position, fire.ForwardVector, fire.UpVector);
								visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
								fire.ExtinguishFire();
							}
							if (gameObject is Sim sim && sim.BuffManager.HasElement(BuffNames.OnFire))
							{
								VisualEffect visualEffect = VisualEffect.Create("ep5UnicornRain");
								visualEffect.SetPosAndOrient(sim.Position, sim.ForwardVector, sim.UpVector);
								visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
								sim.BuffManager.RemoveElement(BuffNames.OnFire);
							}
							if (gameObject is IBed bed && bed.UseCount == 0 && !bed.IsMade())
							{
								playPoofEffect = true;
								foreach (BedData bedData in bed.PartComponent.PartDataList.Values)
								{
									bedData.BedMade = true;
								}
								bed.ResetBindPose();
							}
						}
					}

					if (playPoofEffect)
					{
						GardenGnome.PlayPoofEffect(gameObject.Position, gameObject.ForwardVector, gameObject.UpVector);
					}
				}
				target.MagicallyCleanUp(false, false);
				target.Enchant();
			}
			catch
			{
				if (handle != AlarmHandle.kInvalidHandle)
				{
					AlarmManager.Global.RemoveAlarm(handle);
				}
				throw;
			}
			PlumbBob.SelectedActor.ShowTNSIfSelectable(WonderPowerManager.LocalizeString(PlumbBob.SelectedActor.IsFemale, "RepairTNS", PlumbBob.SelectedActor), StyledNotification.NotificationStyle.kGameMessagePositive);
			return true;
		}

		public static bool SatisfactionActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors()
												  where !sim.BuffManager.HasElement((BuffNames)HashString64("Gamefreak130_SatisfiedBuff"))
												  select sim.SimDescription;
			Sim selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("SatisfactionDialogTitle"))?.CreatedSim;
			if (selectedSim is null)
			{
				return false;
			}

			SuperSatisfy satisfy = new SuperSatisfy.Definition().CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as SuperSatisfy;
			if (!satisfy.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(satisfy);
			return true;
		}

		public static bool SicknessActivation(bool isBacklash)
		{
			Sim selectedSim = null;
			if (isBacklash)
			{
				List<Sim> validSims = Household.ActiveHousehold.Sims.FindAll((sim) => (WonderPowers.IsKidsMagicInstalled ? sim.SimDescription.ChildOrAbove : sim.SimDescription.TeenOrAbove) && !sim.IsRobot && sim.BuffManager.GetElement(BuffNames.CommodityDecayModifier)?.mBuffName != "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DrainedBuff"
																							&& !sim.BuffManager.HasElement(BuffKarmicSickness.kBuffKarmicSicknessGuid));
				if (validSims.Count > 0)
				{
					selectedSim = RandomUtil.GetRandomObjectFromList(validSims);
				}
			}
			else
			{
				IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetSims((sim) => (WonderPowers.IsKidsMagicInstalled ? sim.SimDescription.ChildOrAbove : sim.SimDescription.TeenOrAbove) && !sim.IsRobot
																														&& sim.BuffManager.GetElement(BuffNames.CommodityDecayModifier)?.mBuffName != "Gameplay/Excel/Buffs/BuffList:Gamefreak130_DrainedBuff" && !sim.BuffManager.HasElement(BuffKarmicSickness.kBuffKarmicSicknessGuid))
													  select sim.SimDescription;
				selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("SicknessDialogTitle"))?.CreatedSim;
			}
			if (selectedSim is null)
			{
				return false;
			}

			Camera.FocusOnSim(selectedSim);
			if (selectedSim.IsSelectable)
			{
				PlumbBob.SelectActor(selectedSim);
			}

			VisualEffect visualEffect = VisualEffect.Create("ep7wandspellpestilence_main");
			visualEffect.ParentTo(selectedSim, Sim.FXJoints.Pelvis);
			visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
			WonderPowerManager.PlayPowerSting("sting_sickness");
			BuffKarmicSickness.AddKarmicSickness(selectedSim);
			StyledNotification.Show(new(WonderPowerManager.LocalizeString(selectedSim.IsFemale, "SicknessTNS", selectedSim), selectedSim.ObjectId, StyledNotification.NotificationStyle.kGameMessageNegative));
			WonderPowerManager.TogglePowerRunning();
			return true;
		}

		public static bool StrokeOfGeniusActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors()
												  where sim.SimDescription.ChildOrAbove && !sim.BuffManager.HasElement(BuffStrokeOfGenius.kBuffStrokeOfGeniusGuid)
												  select sim.SimDescription;
			Sim selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("StrokeOfGeniusDialogTitle"))?.CreatedSim;

			if (selectedSim is null)
			{
				return false;
			}

			ActivateStrokeOfGenius activateStrokeOfGenius = new ActivateStrokeOfGenius.Definition().CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as ActivateStrokeOfGenius;
			if (!activateStrokeOfGenius.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(activateStrokeOfGenius);
			return true;
		}

		public static bool SuperLuckyActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors()
												  where sim.SimDescription.ChildOrAbove && !sim.BuffManager.HasElement(BuffSuperLucky.kBuffSuperLuckyGuid)
												  select sim.SimDescription;
			Sim selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("SuperLuckyDialogTitle"))?.CreatedSim;

			if (selectedSim is null)
			{
				return false;
			}

			ActivateSuperLucky activateSuperLucky = new ActivateSuperLucky.Definition().CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as ActivateSuperLucky;
			if (!activateSuperLucky.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(activateSuperLucky);
			return true;
		}

		public static bool TransmogrifyActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetAllActors()
												  where sim.SimDescription.ToddlerOrAbove && !sim.OccultManager.DisallowClothesChange() && !sim.BuffManager.DisallowClothesChange() && !sim.BuffManager.HasElement((BuffNames)BuffTransmogrify.kBuffTransmogrifyGuid)
												  select sim.SimDescription;
			Sim selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("TransmogrifyDialogTitle"))?.CreatedSim;
			if (selectedSim is null)
			{
				return false;
			}

			CASAgeGenderFlags newSpecies = CASAgeGenderFlags.None;
			List<ObjectPicker.HeaderInfo> list = new()
			{
				new("Ui/Tooltip/CAS/Load:SpeciesAll", "Ui/Tooltip/CAS/Load:SpeciesAll", 300)
			};

			List<ObjectPicker.RowInfo> list2 = new()
			{
				new(CASAgeGenderFlags.Human, new()
				{
					new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(ResourceKey.CreatePNGKey("Select_Human", 0u), ThumbnailSize.ExtraLarge), Localization.LocalizeString("Ui/Tooltip/CAS/Load:SpeciesSim"))
				}),
				new(CASAgeGenderFlags.Dog, new()
				{
					new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(ResourceKey.CreatePNGKey("moodlet_dog", 0x48000000u), ThumbnailSize.ExtraLarge), WonderPowerManager.LocalizeString("LargeDogMenuItem"))
				}),
				new(CASAgeGenderFlags.LittleDog, new()
				{
					new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(ResourceKey.CreatePNGKey("moodlet_puppy", 0x48000000u), ThumbnailSize.ExtraLarge), Localization.LocalizeString("Ui/Tooltip/CAS/Load:DogBodyLittle"))
				}),
				new(CASAgeGenderFlags.Cat, new()
				{
					new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(ResourceKey.CreatePNGKey("moodlet_cats", 0x48000000u), ThumbnailSize.ExtraLarge), Localization.LocalizeString("Ui/Tooltip/CAS/Load:SpeciesCat"))
				}),
				new(CASAgeGenderFlags.Horse, new()
				{
					new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(ResourceKey.CreatePNGKey("moodlet_horsie", 0x48000000u), ThumbnailSize.ExtraLarge), Localization.LocalizeString("Ui/Tooltip/CAS/Load:SpeciesHorse"))
				})
			};
			list2.Remove(list2.Find(row => selectedSim.SimDescription.Species == (CASAgeGenderFlags)row.Item));

			List<ObjectPicker.TabInfo> list3 = new()
			{
				new("shop_all_r2", WonderPowerManager.LocalizeString("SelectSpecies"), list2)
			};

			while ((newSpecies & CASAgeGenderFlags.SpeciesMask) is CASAgeGenderFlags.None)
			{
				List<ObjectPicker.RowInfo> selection = ObjectPickerDialog.Show(true, ModalDialog.PauseMode.PauseSimulator, WonderPowerManager.LocalizeString("TransmogrifyDialogTitle"), Localization.LocalizeString("Ui/Caption/ObjectPicker:OK"),
																				Localization.LocalizeString("Ui/Caption/ObjectPicker:Cancel"), list3, list, 1, false);
				newSpecies = selection is not null ? (CASAgeGenderFlags)selection[0].Item : CASAgeGenderFlags.None;
			}

			StartTransmogrify startTransmogrifyInteraction = new StartTransmogrify.Definition(newSpecies).CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as StartTransmogrify;
			if (!startTransmogrifyInteraction.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(startTransmogrifyInteraction);
			return true;
		}

		public static bool WealthActivation(bool _)
		{
			IEnumerable<SimDescription> targets = from sim in PlumbBob.SelectedActor.LotCurrent.GetSims((sim) => sim.SimDescription.TeenOrAbove && !sim.BuffManager.HasElement((BuffNames)HashString64("Gamefreak130_WealthBuff")))
												  select sim.SimDescription;
			Sim selectedSim = HelperMethods.SelectTarget(targets, WonderPowerManager.LocalizeString("WealthDialogTitle"))?.CreatedSim;

			if (selectedSim is null)
			{
				return false;
			}

			ReceiveMagicalCheck receiveInteraction = new ReceiveMagicalCheck.Definition().CreateInstance(selectedSim, selectedSim, new(InteractionPriorityLevel.CriticalNPCBehavior), false, false) as ReceiveMagicalCheck;
			if (!receiveInteraction.Test())
			{
				return false;
			}
			selectedSim.InteractionQueue.AddNext(receiveInteraction);
			return true;
		}
	}
}
