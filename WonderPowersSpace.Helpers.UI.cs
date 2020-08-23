using System;
using System.Collections.Generic;
using System.Text;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Tutorial;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Hud;

namespace Gamefreak130.WonderPowersSpace.Helpers.UI
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

		private readonly int mPreviousMusicMode;

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
			if (sMenu == null)
			{
				using (WonderModeMenu menu = new WonderModeMenu())
				{
					menu.StartModal();
				}
				sMenu = null;
			}
		}

		private void SetKarma(float karma)
		{
			mKarma = karma;
		}

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
				mPreviousMusicMode = (int)AudioManager.MusicMode;
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
				mAcceptButton.Click += new UIEventHandler<UIButtonClickEventArgs>(OnAcceptClick);
				mCloseButton = mModalDialogWindow.GetChildByID((uint)ControlIds.kCancelButton, true) as Button;
				mCloseButton.Click += new UIEventHandler<UIButtonClickEventArgs>(OnClose);
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
			if (power.Cost() <= WonderPowers.GetKarma())
            {
				//Color
				mAcceptButton.Enabled = true;
				mAcceptButton.TooltipText = Localization.LocalizeString("Ui/Caption/Global:Accept");
            }
			else
            {//Color
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
			foreach (WonderPower power in WonderPowers.GetWonderPowerList())
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
						mBadPowerGrid.AddItem(new ItemGridCellItem(window, power));
					}
					else
                    {
						thumbBg[DrawableBase.ControlStates.kNormal] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg", 0u));
						thumbBg[DrawableBase.ControlStates.kHighlighted] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg_hl", 0u));
						thumbBg[DrawableBase.ControlStates.kActive] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg_ac", 0u));
						thumbBg[DrawableBase.ControlStates.kCheckedNormal] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg_ac", 0u));
						thumbBg[DrawableBase.ControlStates.kCheckedHighlighted] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg_ac", 0u));
						thumbBg[DrawableBase.ControlStates.kCheckedActive] = UIManager.LoadUIImage(ResourceKey.CreatePNGKey("good_power_thumb_bg_ac", 0u));
						mGoodPowerGrid.AddItem(new ItemGridCellItem(window, power));
                    }
                }
			}
        }

		private static string LocalizeString(string name, params object[] parameters)
        {
			return Localization.LocalizeString("UI/WonderMode/KarmaMenu:" + name, parameters);
        }

		public override bool OnEnd(uint endID)
		{
			AudioManager.SetMusicMode((MusicMode)mPreviousMusicMode);
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

				if (power != null && power.Cost() <= WonderPowers.GetKarma())
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
        {
			mCancelButton.Visible = false;
        }

		new public static IMapTagPickerInfo Show(List<IMapTagPickerInfo> mapTagPickerInfos, string titleText, string confirmText)
		{
			return Show(mapTagPickerInfos, titleText, confirmText, false);
		}

		new public static IMapTagPickerInfo Show(List<IMapTagPickerInfo> mapTagPickerInfos, string titleText, string confirmText, bool forceShowCost)
		{
            return Show(mapTagPickerInfos, titleText, confirmText, forceShowCost, 0f, out _);
        }

		new public static IMapTagPickerInfo Show(List<IMapTagPickerInfo> mapTagPickerInfos, string titleText, string confirmText, bool forceShowCost, float exclusivityMultiplier, out bool hasExclusiveAccess)
		{
			return Show(mapTagPickerInfos, titleText, confirmText, null, forceShowCost, exclusivityMultiplier, true, out hasExclusiveAccess);
		}

		new public static IMapTagPickerInfo Show(List<IMapTagPickerInfo> mapTagPickerInfos, string titleText, string confirmText, string alternateConfirmText, bool forceShowCost, float exclusivityMultiplier, bool toggleForwardingEventsToGame, out bool hasExclusiveAccess)
		{
			hasExclusiveAccess = false;
			PopupMenu.CloseOptions();
			if ((IntroTutorial.IsRunning && !IntroTutorial.AreYouExitingTutorial()) || !UIUtils.IsOkayToStartModalDialog())
			{
				return null;
			}
			UserToolUtils.OnClose();
			Responder.Instance.HudModel.RestoreUIVisibility();
			if (EnableModalDialogs && sDialog == null)
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
				Responder.Instance.HudModel.RefreshMapTags();
				return result;
			}
			return null;
		}
	}
}
