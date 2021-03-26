using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Gamefreak130.WonderPowersSpace.Helpers;
using Sims3.Gameplay;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Passport;
using Sims3.Gameplay.Tutorial;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.CAS;
using Sims3.UI.Dialogs;
using Sims3.UI.GameEntry;
using Sims3.UI.Hud;
using static Sims3.SimIFace.ResourceUtils;

namespace Gamefreak130.WonderPowersSpace.UI
{
	public class WonderModeMenu : ModalDialog
	{
		private enum ControlIds : uint
		{
			kGoodPowerGrid = 0xd6da8d05,
			kBadPowerGrid,
			kSelectedPowerPreview = 0xd6da8f11,
			kSelectedPowerPoints,
			kSelectedPowerName = 0xd6da8f15,
			kSelectedPowerDesc,
			kOkayButton = 0x05ef6bd1,
			kCancelButton
		}

		private const string kBrowseWonderPowersMusic = "music_mode_karma";

		//public delegate void PowerSelectHandler(string power);

		private readonly bool sIncrementalButtonIndexing = false;

		/*private List<WonderPower> mGoodPowers = new List<WonderPower>();

		private List<WonderPower> mBadPowers = new List<WonderPower>();*/

		private float mKarma;

		//private PowerSelectHandler PowerSelected;

		private readonly uint mMusicHandle;

		private readonly MusicMode mPreviousMusicMode;

		private readonly Button mAcceptButton;

		private readonly Button mCloseButton;

		private readonly ItemGrid mBadPowerGrid;

		private readonly ItemGrid mGoodPowerGrid;

		private readonly Window mPowerPreview;

		private readonly Text mPowerPoints;

		private readonly Text mPowerName;

		private readonly TextEdit mPowerDesc;

		private readonly FillBarController mKarmaMeter;

		private static WonderModeMenu sMenu;

		public static void Show()
		{
			if (sMenu is null)
			{
				using (WonderModeMenu menu = new())
				{
					menu.StartModal();
				}
				sMenu = null;
			}
		}

        private void SetKarma(float karma) => mKarma = karma;

        private WonderModeMenu() : base("WonderMode", 1, true, PauseMode.PauseSimulator, null)
		{
			if (mModalDialogWindow != null)
			{
				/*for (int i = 0; i < 13; i++)
				{
					mWonderPowerCosts[i] = 0;
				}
				SetKarma(WonderPowers.GetKarma());
				//PowerSelected = (PowerSelectHandler)(object)new PowerSelectHandler(WonderPowerSelected);*/
				mPreviousMusicMode = AudioManager.MusicMode;
				AudioManager.SetMusicMode(MusicMode.None);
				mMusicHandle = Audio.StartSound(kBrowseWonderPowersMusic);

				mPowerPreview = mModalDialogWindow.GetChildByID((uint)ControlIds.kSelectedPowerPreview, true) as Window;
				mPowerName = mModalDialogWindow.GetChildByID((uint)ControlIds.kSelectedPowerName, true) as Text;
				mPowerPoints = mModalDialogWindow.GetChildByID((uint)ControlIds.kSelectedPowerPoints, true) as Text;
				mPowerDesc = mModalDialogWindow.GetChildByID((uint)ControlIds.kSelectedPowerDesc, true) as TextEdit;
				mGoodPowerGrid = mModalDialogWindow.GetChildByID((uint)ControlIds.kGoodPowerGrid, true) as ItemGrid;
				mBadPowerGrid = mModalDialogWindow.GetChildByID((uint)ControlIds.kBadPowerGrid, true) as ItemGrid;
				mGoodPowerGrid.ItemClicked += OnPowerSelect;
				mBadPowerGrid.ItemClicked += OnPowerSelect;
				PopulatePowerGrid();
				mAcceptButton = mModalDialogWindow.GetChildByID((uint)ControlIds.kOkayButton, true) as Button;
				mAcceptButton.Click += OnAcceptClick;
				mCloseButton = mModalDialogWindow.GetChildByID((uint)ControlIds.kCancelButton, true) as Button;
				mCloseButton.Click += OnClose;
				mCloseButton.TooltipText = Localization.LocalizeString("Ui/Caption/ObjectPicker:Cancel");
				mGoodPowerGrid.SelectedItem = 0;
				SetPowerInfo(mGoodPowerGrid.SelectedTag as WonderPower);

				mKarmaMeter = mModalDialogWindow.GetChildByID(5u, true) as FillBarController;
				if (mKarmaMeter != null)
				{
					mKarmaMeter.Initialize(-100f, 100f, 0.5f);
					/*if (this.mHudModel.CheatsEnabled)
					{
						fillBarController.EnableCheatWindow(simDesc);
						fillBarController.CheatBarDragged += new FillBarController.CheatFillBarDragHandler(this.OnRelationshipDrag);
					}*/
				}

				mModalDialogWindow.GetChildByID(7, true).Caption = 999.ToString();
			}
		}

        private void OnPowerSelect(ItemGrid sender, ItemGridCellClickEvent eventArgs)
        {
			Audio.StartSound("ui_secondary_button");
			WonderPower power = sender.SelectedTag as WonderPower;
			ItemGrid gridToClear = power.IsBadPower ? mGoodPowerGrid : mBadPowerGrid;
			gridToClear.SelectedItem = -1;
			SetPowerInfo(power);
        }

        private void SetPowerInfo(WonderPower power)
        {
			if (WonderPowerManager.HasEnoughKarma(power.Cost()))
            {
				//TODO Color
				mAcceptButton.Enabled = true;
				mAcceptButton.TooltipText = Localization.LocalizeString("Ui/Caption/Global:Accept");
            }
			else
            {//TODO Color
				mAcceptButton.Enabled = false;
				mAcceptButton.TooltipText = LocalizeString("NotEnoughKarma");
            }
			(mPowerPreview.Drawable as ImageDrawable).Image = UIManager.LoadUIImage(ResourceKey.CreatePNGKey(power.WonderPowerName + "_Preview", 0u));
			mPowerName.Caption = LocalizeString(power.WonderPowerName);
			mPowerPoints.Caption = power.Cost() + " " + LocalizeString("Points");
			mPowerDesc.Caption = LocalizeString(power.WonderPowerName + "Description");
			mPowerPreview.Invalidate();
		}

        private void PopulatePowerGrid()
        {
			mGoodPowerGrid.Clear();
			mBadPowerGrid.Clear();
			ResourceKey resKey = ResourceKey.CreateUILayoutKey("WonderPowerEntry", 0u);
			foreach (WonderPower power in WonderPowerManager.GetWonderPowerList())
            {
				Window window = UIManager.LoadLayout(resKey).GetWindowByExportID(1) as Window;
				if (window != null)
                {
					StdDrawable thumbBg = window.Drawable as StdDrawable;
					((window.GetChildByID(3604647233u, true) as Window).Drawable as ImageDrawable).Image = UIManager.LoadUIImage(ResourceKey.CreatePNGKey(power.WonderPowerName, 0u));
					if (power.IsBadPower)
					{
						thumbBg[DrawableBase.ControlStates.kNormal] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("bad_power_thumb_bg", 0u));
						thumbBg[DrawableBase.ControlStates.kHighlighted] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("bad_power_thumb_bg_hl", 0u));
						thumbBg[DrawableBase.ControlStates.kActive] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("bad_power_thumb_bg_ac", 0u));
						thumbBg[DrawableBase.ControlStates.kCheckedNormal] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("bad_power_thumb_bg_ac", 0u));
						thumbBg[DrawableBase.ControlStates.kCheckedHighlighted] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("bad_power_thumb_bg_ac", 0u));
						thumbBg[DrawableBase.ControlStates.kCheckedActive] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("bad_power_thumb_bg_ac", 0u));
						mBadPowerGrid.AddItem(new(window, power));
					}
					else
                    {
						thumbBg[DrawableBase.ControlStates.kNormal] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg", 0u));
						thumbBg[DrawableBase.ControlStates.kHighlighted] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg_hl", 0u));
						thumbBg[DrawableBase.ControlStates.kActive] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg_ac", 0u));
						thumbBg[DrawableBase.ControlStates.kCheckedNormal] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg_ac", 0u));
						thumbBg[DrawableBase.ControlStates.kCheckedHighlighted] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg_ac", 0u));
						thumbBg[DrawableBase.ControlStates.kCheckedActive] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg_ac", 0u));
						mGoodPowerGrid.AddItem(new(window, power));
                    }
                }
			}
        }

		private static string LocalizeString(string name, params object[] parameters) => Localization.LocalizeString("UI/WonderMode/KarmaMenu:" + name, parameters);

		public override bool OnEnd(uint endID)
		{
			AudioManager.SetMusicMode(mPreviousMusicMode);
			if (mMusicHandle != 0u)
			{
				Audio.StopSound(mMusicHandle);
			}

			if (endID == 1)
			{
				WonderPower power = null;
				if (mGoodPowerGrid.SelectedItem != -1)
				{
					power = mGoodPowerGrid.SelectedTag as WonderPower;
				}
				else if (mBadPowerGrid.SelectedItem != -1)
				{
					power = mBadPowerGrid.SelectedTag as WonderPower;
				}

				if (power != null && WonderPowerManager.HasEnoughKarma(power.Cost()))
				{
					Simulator.AddObject(new OneShotFunctionWithParams(power.Run, false));
				}
			}
			return true;
		}

		private void OnAcceptClick(WindowBase sender, UIButtonClickEventArgs eventArgs)
		{
			EndDialog(1u);
			eventArgs.Handled = true;
		}

		private void OnClose(WindowBase sender, UIButtonClickEventArgs eventArgs)
        {
			EndDialog(0u);
			eventArgs.Handled = true;
        }
	}

	public static class CASWonderMode
    {
		public static void OnOptionsClick(WindowBase sender, UIButtonClickEventArgs _)
		{
			if (CASPuck.Instance is CASPuck puck && !puck.UiBusy)
			{
				Button button = puck.GetChildByID((uint)CASPuck.ControlIDs.OptionsButton, true) as Button;
				List<PuckCommon.OptionsMenuItems> list = DownloadDashboard.Enable
                    ? new()
                    {
                        PuckCommon.OptionsMenuItems.Tutorials,
                        PuckCommon.OptionsMenuItems.Options,
                        PuckCommon.OptionsMenuItems.MainMenu,
                        PuckCommon.OptionsMenuItems.QuitToWindows,
                        PuckCommon.OptionsMenuItems.QuitToLauncher,
                        PuckCommon.OptionsMenuItems.DownloadDashboard
                    }
                    : new()
                    {
                        PuckCommon.OptionsMenuItems.Tutorials,
                        PuckCommon.OptionsMenuItems.Options,
                        PuckCommon.OptionsMenuItems.MainMenu,
                        PuckCommon.OptionsMenuItems.QuitToWindows,
                        PuckCommon.OptionsMenuItems.QuitToLauncher
                    };
                if (puck.mPuckCommon.CanShowOptionsMenu() && PuckCommon.mOptionsTask is null)
				{
					PuckCommon.OptionsMenuInfo param = new(sender, list, puck.mPuckCommon);
					PuckCommon.mOptionsTask = new OneShotFunctionWithParams(OptionsMenuTask, param);
					Simulator.AddObject(PuckCommon.mOptionsTask);
				}
			}
		}

		private static void OptionsMenuTask(object optionMenuInfoParam)
		{
			if (CASPuck.Instance?.mPuckCommon is PuckCommon puckCommon)
			{
				PuckCommon.OptionsMenuInfo optionsMenuInfo = optionMenuInfoParam as PuckCommon.OptionsMenuInfo;
				if (!puckCommon.CanShowOptionsMenu())
				{
					PuckCommon.mOptionsTask = null;
					return;
				}
				puckCommon.mOptionsMenuInfo = optionsMenuInfo;
				int num = PopupMenu.Show(optionsMenuInfo.OptionStrings, optionsMenuInfo.ScreenPos);
				PuckCommon.mOptionsTask = null;
				if (num >= 0)
				{
					PuckCommon.OptionsMenuItems item = optionsMenuInfo.OptionIds[num];
					OnOptionsMenuSelect(item);
				}
				puckCommon.mOptionsMenuInfo = null;
			}
		}

		private static void OnOptionsMenuSelect(PuckCommon.OptionsMenuItems item)
		{
			if (CASPuck.Instance?.mPuckCommon is PuckCommon puckCommon)
			{
				if (Sims3.UI.Responder.Instance.PassportModel.WorldIsCurrentlyHostingASimViaPassport() && item is PuckCommon.OptionsMenuItems.MainMenu or PuckCommon.OptionsMenuItems.SaveGame or PuckCommon.OptionsMenuItems.SaveGameAs or PuckCommon.OptionsMenuItems.QuitToWindows or PuckCommon.OptionsMenuItems.QuitToWindowsAndSave)
				{
					if (!Sims3.UI.Responder.Instance.PassportModel.IsShowComplete() && !TwoButtonDialog.Show(Sims3.UI.Responder.Instance.LocalizationModel.LocalizeString("UI/Caption/Passport:VerifyQuitWhileHosting"), 
																													 Sims3.UI.Responder.Instance.LocalizationModel.LocalizeString("UI/Caption/Passport:Yes"), 
																													 Sims3.UI.Responder.Instance.LocalizationModel.LocalizeString("UI/Caption/Passport:No")))
					{
						return;
					}
					try
					{
						Sims3.UI.Responder.Instance.PassportModel.SendSimHomeImmediately();
					}
					catch (Exception)
					{
					}
				}
				switch (item)
				{
					case PuckCommon.OptionsMenuItems.SaveGame:
						puckCommon.OnSave();
						return;
					case PuckCommon.OptionsMenuItems.SaveGameAs:
						puckCommon.OnSaveAs();
						return;
					case PuckCommon.OptionsMenuItems.EditTown:
						if (Sims3.UI.Responder.Instance.PassportModel.GetIsHostingSim())
						{
							return;
						}
						puckCommon.OnEditTown();
						return;
					case PuckCommon.OptionsMenuItems.PlayFlow:
						puckCommon.OnPlayFlow();
						return;
					case PuckCommon.OptionsMenuItems.Tutorials:
						puckCommon.OnTutorialette();
						return;
					case PuckCommon.OptionsMenuItems.Options:
						puckCommon.OnOptions();
						return;
					case PuckCommon.OptionsMenuItems.ReturnToLive:
						puckCommon.OnReturnToLive();
						return;
					case PuckCommon.OptionsMenuItems.ReturnToPlayFlow:
						puckCommon.OnReturnToPlayFlow();
						return;
					case PuckCommon.OptionsMenuItems.MainMenu:
						GameStates.mQuitting = true;
						Simulator.AddObject(new Sims3.UI.OneShotFunctionTask(QuitToMenuTask));
						return;
					case PuckCommon.OptionsMenuItems.QuitToWindowsAndSave:
						puckCommon.OnQuitToWindowsAndSave();
						return;
					case PuckCommon.OptionsMenuItems.QuitToWindows:
						GameStates.mQuitting = true;
						Simulator.AddObject(new Sims3.UI.OneShotFunctionTask(QuitToWindowsTask));
						return;
					case PuckCommon.OptionsMenuItems.QuitToLauncher:
						puckCommon.OnQuitToLauncher();
						return;
					case PuckCommon.OptionsMenuItems.EditTownTutorial:
						puckCommon.OnEditTownTutorial();
						return;
					case PuckCommon.OptionsMenuItems.DownloadDashboard:
					case PuckCommon.OptionsMenuItems.Login:
					case PuckCommon.OptionsMenuItems.InGameWall:
					case PuckCommon.OptionsMenuItems.Passport:
						puckCommon.mQueuedMenuOption = item;
						LoginDialog.PromptIfSuccessCall(new(puckCommon.DoMenuOptionTask));
						return;
					default:
						return;
				}
			}
		}

		private static void QuitToMenuTask()
		{
			if (GameStates.IsCurrentlySwitchingSubStates || GameStates.mQuittingTaskStarted || GameStates.QuitDisabled)
			{
				return;
			}
			GameStates.mQuittingTaskStarted = true;
			if (GameStates.sSingleton.mInWorldState.mStateMachine.CurState.StateId == (int)HashString32("CASWonderModeState") || GameStates.IsCasState || GameStates.IsBuildBuyLikeState || Passport.Instance.IsCurrentlyHosting())
			{
				if (TwoButtonDialog.Show(Localization.LocalizeString("Ui/Caption/QuitDialog:PromptMainMenuNoSave"), Localization.LocalizeString("Ui/Caption/Global:Exit"), Localization.LocalizeString("Ui/Caption/QuitDialog:Cancel")))
				{
					GameStates.TransitionToLeaveInWorld();
				}
			}
			else
			{
				string promptText2 = Localization.LocalizeString("Ui/Caption/QuitToMainMenuDialog:Prompt");
				string firstButton = Localization.LocalizeString("Ui/Caption/QuitToMainMenuDialog:SaveAndQuit");
				string secondButton = Localization.LocalizeString("Ui/Caption/QuitToMainMenuDialog:QuitWinhoutSaving");
				string thirdButton = Localization.LocalizeString("Ui/Caption/QuitToMainMenuDialog:Cancel");
				ThreeButtonDialog.ButtonPressed buttonPressed = ThreeButtonDialog.Show(promptText2, firstButton, secondButton, thirdButton);
				if (buttonPressed == ThreeButtonDialog.ButtonPressed.FirstButton)
				{
					OptionsModel optionsModel = Sims3.Gameplay.UI.Responder.Instance.OptionsModel as OptionsModel;
					if (optionsModel.SaveGame(true))
					{
						GameStates.TransitionToLeaveInWorld();
					}
				}
				else if (buttonPressed == ThreeButtonDialog.ButtonPressed.SecondButton)
				{
					GameStates.TransitionToLeaveInWorld();
				}
			}
			GameStates.mQuitting = false;
			GameStates.mQuittingTaskStarted = false;
		}

		private static void QuitToWindowsTask()
		{
			bool inCasWonderMode = GameStates.sSingleton.mInWorldState.mStateMachine.CurState.StateId == (int)HashString32("CASWonderModeState");
			if (GameStates.mQuittingTaskStarted || GameStates.mQuitDisableCount > 0 || GameStates.IsGameShuttingDown || MiniLoad.Loading)
			{
				GameStates.mQuitting = GameStates.mQuittingTaskStarted;
				return;
			}
			if ((inCasWonderMode || GameStates.IsCasState) && CASController.Singleton is not null && (CASController.Singleton.CurrentState.mTopState == CASTopState.None || CASController.Singleton.UiBusy || LoadScreen.Loading()))
			{
				GameStates.mQuitting = false;
				return;
			}
			GameStates.mQuittingTaskStarted = true;
			if (GameStates.sSingleton.mStateMachine.CurStateId == 4)
			{
				if (GameStates.sSingleton.mInWorldState is not null && (GameStates.sSingleton.mInWorldState.IsCurrentlySwitchingSubStates || GameStates.IsTravelling))
				{
					GameStates.mQuittingTaskStarted = false;
					GameStates.mQuitting = false;
					return;
				}
				if (inCasWonderMode || GameStates.IsCasState || GameStates.IsBuildBuyLikeState)
				{
					if (TwoButtonDialog.Show(Localization.LocalizeString("Ui/Caption/QuitDialog:PromptNoSave"), Localization.LocalizeString("Ui/Caption/Global:Exit"), Localization.LocalizeString("Ui/Caption/QuitDialog:Cancel")))
					{
						GameStates.GotoState(GameState.InWorldToQuit);
					}
				}
				else if (!IntroTutorial.IsRunning || IntroTutorial.AreYouExitingTutorial())
				{
					if (Sims3.UI.Responder.Instance.PassportModel.WorldIsCurrentlyHostingASimViaPassport())
					{
						if (!TwoButtonDialog.Show(Sims3.UI.Responder.Instance.LocalizationModel.LocalizeString("UI/Caption/Passport:VerifyQuitWhileHosting"),
												  Sims3.UI.Responder.Instance.LocalizationModel.LocalizeString("UI/Caption/Passport:Yes"), Sims3.UI.Responder.Instance.LocalizationModel.LocalizeString("UI/Caption/Passport:No")))
						{
							GameStates.mQuitting = false;
							GameStates.mQuittingTaskStarted = false;
							return;
						}
						Sims3.UI.Responder.Instance.PassportModel.SendSimHomeImmediately();
					}
					if (InWorldState.PromptedQuit())
					{
						GameStates.GotoState(GameState.InWorldToQuit);
					}
				}
			}
			else if (GameStates.sSingleton.mStateMachine.CurStateId == 2)
			{
				if (CommandLine.FindSwitch("ccinstall") is null || CommandLine.FindSwitch("ccuninstall") is null || AcceptCancelDialog.Show(Localization.LocalizeString("Gameplay/Gameflow:QuitSims3DialogTitle")))
				{
					GameStates.GotoState(GameState.Quit);
				}
			}
			GameStates.mQuitting = false;
			GameStates.mQuittingTaskStarted = false;
		}

		public static void OnCloseClick(WindowBase sender, UIButtonClickEventArgs eventArgs)
		{
			if (CASPuck.Instance is not CASPuck puck || puck.UiBusy || puck.mLeaveCAS)
			{
				return;
			}
			puck.UiBusy = true;
			Simulator.AddObject(new Sims3.UI.OneShotFunctionTask(delegate () {
				ICASModel casmodel = Sims3.UI.Responder.Instance.CASModel;
				ILocalizationModel localizationModel = Sims3.UI.Responder.Instance.LocalizationModel;
				if (TwoButtonDialog.Show(localizationModel.LocalizeString("Ui/Caption/CAS/ExitDialog:AlternatePrompt"), localizationModel.LocalizeString("Ui/Caption/Global:Yes"), localizationModel.LocalizeString("Ui/Caption/Global:No")))
				{
					CASController singleton = CASController.Singleton;
					singleton.AllowCameraMovement(false);
					while (casmodel.IsProcessing())
					{
						Simulator.Sleep(0U);
					}
					sender.Enabled = false;
					casmodel.RequestClearChangeReport();
					singleton.SetCurrentState(CASState.None);
					return;
				}
				puck.UiBusy = false;
			}));
			eventArgs.Handled = true;
		}

		public static void HideCharacterSheetElement(uint id)
		{
			if (CASCharacterSheet.gSingleton?.GetChildByID(id, true) is WindowBase window)
			{
				window.Visible = false;
			}
		}
	}

	internal static class OptionsInjector
	{
		private static bool mOptionsInjectionHandled;

		internal static bool InjectOptions()
		{
			try
			{
				if (OptionsDialog.sDialog is not null)
				{
					if (!mOptionsInjectionHandled)
					{
						OptionsDialog.sDialog.mMusicData.Add(new());
						OptionsDialog.sDialog.mMusicData.Add(new());
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
			if (Simulator.LoadXML(xmlFileName)?.GetElementsByTagName("MusicSelection")[0] is XmlElement xmlElement)
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

	/*public class KarmaDial
	{
		public delegate void WitchingHourCompleted();

		public delegate void WishFulfilledCompleted();

		public static float mPreviousKarma;

		public static float mCurrentKarma;

		public static WitchingHourCompleted WitchingHourCompletedFunction = null;

		public static WishFulfilledCompleted WishFulfilledCompletedFunction = null;

		private static KarmaDial sInstance = null;

		private bool mbVisible;

		private bool mbMovieLoaded;

		private static bool mIsMidnight = false;

		private LoadCompletedCallback mMovieLoaded;

		private MovieDoneCallback mMovieDone;

		private GameSpeedChangedCallback mGameSpeedChanged;

		public static bool IsVisible
		{
			get
			{
				return sInstance != null && sInstance.mbVisible;
			}
		}

		internal static KarmaDial Instance
		{
			get
			{
				return sInstance;
			}
		}

		public static void Show()
		{
			if (sInstance == null)
			{
				return;
			}
			if (!sInstance.mbMovieLoaded)
			{
				return;
			}
			sInstance.mbVisible = true;
			ScaleformManager.KarmaDialInit((int)mPreviousKarma, (int)mCurrentKarma);
			if (mIsMidnight)
			{
				ScaleformManager.ScaleformInvoke("KarmaDial", "_root.KarmaDial_Panel.KD_SETMIDNIGHT");
			}
			ScaleformManager.ScaleformInvoke("KarmaDial", "_root.KarmaDial_Panel.KD_SHOW");
		}

		private void OnGameSpeedChange(Gameflow.GameSpeed newSpeed, bool locked)
		{
			if (sInstance != null)
			{
				if (newSpeed == Gameflow.GameSpeed.Pause)
				{
					ScaleformManager.ScaleformInvoke("KarmaDial", "_root.KarmaDial_Panel.KD_PAUSE");
					return;
				}
				ScaleformManager.ScaleformInvoke("KarmaDial", "_root.KarmaDial_Panel.KD_PLAY");
			}
		}

		public static void Hide()
		{
			if (sInstance == null)
			{
				return;
			}
			Unload();
		}

		public static void Load(float prevKarma, float newKarma, bool isMidnight)
		{
			if (sInstance == null)
			{
				sInstance = new KarmaDial();
				sInstance.mMovieLoaded = new LoadCompletedCallback(sInstance.MovieLoaded);
				sInstance.mMovieDone = new MovieDoneCallback(sInstance.MovieDone);
				sInstance.mGameSpeedChanged = new GameSpeedChangedCallback(sInstance.OnGameSpeedChange);
				mPreviousKarma = prevKarma;
				mCurrentKarma = newKarma;
				mIsMidnight = isMidnight;
				WitchingHourCompletedFunction = null;
				WishFulfilledCompletedFunction = null;
				ScaleformManager.LoadScaleformMovie("KarmaDial", sInstance.mMovieLoaded, sInstance.mMovieDone, 3);
				Responder.Instance.GameSpeedChanged += sInstance.mGameSpeedChanged;
			}
		}

		public static void Unload()
		{
			if (sInstance != null)
			{
				ScaleformManager.UnLoadScaleformMovie("KarmaDial");
				Responder.Instance.GameSpeedChanged -= sInstance.mGameSpeedChanged;
				sInstance = null;
			}
		}

		public static void SetMidnight()
		{
			mIsMidnight = true;
		}

		private void MovieLoaded()
		{
			mbMovieLoaded = true;
			Show();
		}

		public void MovieDone()
		{
			if (!mIsMidnight)
			{
				WishFulfilledCompletedFunction?.Invoke();
			}
			else
			{
				WitchingHourCompletedFunction?.Invoke();
			}
			Hide();
		}

		private KarmaDial()
		{
			mCurrentKarma = 0f;
			mPreviousKarma = 0f;
		}
	}*/

	public class MapTagPickerUncancellable : MapTagPickerDialog
    {
		private MapTagPickerUncancellable(List<IMapTagPickerInfo> mapTagPickerInfos, string titleText, string confirmText, string alternateConfirmText, bool forceShowCost, float exclusivityMultiplier, bool toggleForwardingEventsToGame, ModalDialog.PauseMode pauseMode, bool modal) 
			: base(mapTagPickerInfos, titleText, confirmText, alternateConfirmText, forceShowCost, exclusivityMultiplier, toggleForwardingEventsToGame, pauseMode, modal) 
			=> mCancelButton.Visible = false;

		new public static IMapTagPickerInfo Show(List<IMapTagPickerInfo> mapTagPickerInfos, string titleText, string confirmText) 
			=> Show(mapTagPickerInfos, titleText, confirmText, false);

		new public static IMapTagPickerInfo Show(List<IMapTagPickerInfo> mapTagPickerInfos, string titleText, string confirmText, bool forceShowCost) 
			=> Show(mapTagPickerInfos, titleText, confirmText, forceShowCost, 0f, out _);

		new public static IMapTagPickerInfo Show(List<IMapTagPickerInfo> mapTagPickerInfos, string titleText, string confirmText, bool forceShowCost, float exclusivityMultiplier, out bool hasExclusiveAccess) 
			=> Show(mapTagPickerInfos, titleText, confirmText, null, forceShowCost, exclusivityMultiplier, true, out hasExclusiveAccess);

		new public static IMapTagPickerInfo Show(List<IMapTagPickerInfo> mapTagPickerInfos, string titleText, string confirmText, string alternateConfirmText, bool forceShowCost, float exclusivityMultiplier, bool toggleForwardingEventsToGame, out bool hasExclusiveAccess)
		{
			hasExclusiveAccess = false;
			PopupMenu.CloseOptions();
			if ((IntroTutorial.IsRunning && !IntroTutorial.AreYouExitingTutorial()) || !UIUtils.IsOkayToStartModalDialog())
			{
				return null;
			}
			UserToolUtils.OnClose();
			Sims3.UI.Responder.Instance.HudModel.RestoreUIVisibility();
			if (EnableModalDialogs && sDialog is null)
			{
				MapTagFilterType sLastFilterType = MapViewController.sLastFilterType;
				MapViewController.sLastFilterType = (MapTagFilterType)4294967295U;
				MapTagController.Instance.MapTagFilter = (MapTagFilterType)4294967295U;
				sDialog = new MapTagPickerUncancellable(mapTagPickerInfos, titleText, confirmText, alternateConfirmText, forceShowCost, exclusivityMultiplier, toggleForwardingEventsToGame, PauseMode.PauseSimulator, false);
				sDialog.StartModal();
				IMapTagPickerInfo result = sDialog.mResult;
				hasExclusiveAccess = sDialog.mHasExclusiveAccess;
				sDialog = null;
				MapViewController.sLastFilterType = sLastFilterType;
				Sims3.UI.Responder.Instance.HudModel.RefreshMapTags();
				return result;
			}
			return null;
		}
	}
}