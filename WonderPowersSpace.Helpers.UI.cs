using System;
using System.Collections.Generic;
using System.Text;
using Sims3.Gameplay.Core;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Hud;

namespace Gamefreak130.WonderPowersSpace.Helpers.UI
{
	public class WonderModeMenu : ModalDialog
	{
		private enum ControlIds : uint
		{

		}

		public const string kBrowseWonderPowersMusic = "music_mode_ltr";

		//public delegate void PowerSelectHandler(string power);

		private readonly bool sIncrementalButtonIndexing = false;

		private readonly string[] mWonderPowerNames;

		private readonly bool[] mWonderPowerStates;

		private readonly int[] mWonderPowerCosts;

		private float mKarma;

		//private PowerSelectHandler PowerSelected;

		private readonly uint mMusicHandle;

		private readonly int mPreviousMusicMode;

		private VisualEffect mEdgeEffect;

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

		public void SetPowerState(string name, bool state, int cost)
		{
			for (int i = 0; i < mWonderPowerNames.Length; i++)
			{
				if (mWonderPowerNames[i].Equals(name))
				{
					mWonderPowerStates[i] = state;
					mWonderPowerCosts[i] = cost;
				}
			}
		}

		public void SetKarma(float karma)
		{
			mKarma = karma;
		}

		private WonderModeMenu() : base("WonderMode", 1, true, PauseMode.PauseSimulator, null)
		{
			if (mModalDialogWindow != null)
			{
				//ScaleformManager.WonderModeInit(mWonderPowerStates[0], mWonderPowerStates[1], mWonderPowerStates[2], mWonderPowerStates[3], mWonderPowerStates[4], mWonderPowerStates[5], mWonderPowerStates[6], mWonderPowerStates[7], mWonderPowerStates[8], mWonderPowerStates[9], mWonderPowerStates[10], mWonderPowerStates[11], mWonderPowerStates[12], mWonderPowerCosts[0], mWonderPowerCosts[1], mWonderPowerCosts[2], mWonderPowerCosts[3], mWonderPowerCosts[4], mWonderPowerCosts[5], mWonderPowerCosts[6], mWonderPowerCosts[7], mWonderPowerCosts[8], mWonderPowerCosts[9], mWonderPowerCosts[10], mWonderPowerCosts[11], mWonderPowerCosts[12], (int)mKarma);
				mWonderPowerNames = new string[13];
				mWonderPowerStates = new bool[13];
				mWonderPowerCosts = new int[13];
				mWonderPowerNames[0] = "curse";
				mWonderPowerNames[1] = "fire";
				mWonderPowerNames[2] = "Earthquake";
				mWonderPowerNames[3] = "ghosts";
				mWonderPowerNames[4] = "doom";
				mWonderPowerNames[5] = "beauty";
				mWonderPowerNames[6] = "goodmood";
				mWonderPowerNames[7] = "repair";
				mWonderPowerNames[8] = "satisfaction";
				mWonderPowerNames[9] = "strokeOfGenius";
				mWonderPowerNames[10] = "superLucky";
				mWonderPowerNames[11] = "wealth";
				mWonderPowerNames[12] = "ghostify";
				for (int i = 0; i < 13; i++)
				{
					mWonderPowerCosts[i] = 0;
				}
				mKarma = 0f;
				foreach (WonderPower wonderPower in WonderPowers.GetWonderPowerList())
				{
					SetPowerState(wonderPower.WonderPowerName, wonderPower.IsLocked, wonderPower.Cost());
				}
				SetKarma(WonderPowers.GetKarma());
				PowerSelected = (PowerSelectHandler)(object)new PowerSelectHandler(WonderPowerSelected);
				mPreviousMusicMode = (int)AudioManager.MusicMode;
				AudioManager.SetMusicMode(MusicMode.None);
				mMusicHandle = Audio.StartSound(kBrowseWonderPowersMusic);
				StartWonderModeSelectionScreenEffects();
			}
		}

		public override bool OnEnd(uint endID)
		{
			AudioManager.SetMusicMode((MusicMode)mPreviousMusicMode);
			if (mMusicHandle != 0u)
			{
				Audio.StopSound(mMusicHandle);
			}
			return true;
		}

		public void WonderPowerSelected(string powerName)
		{
			if (powerName == null)
			{
				StopWonderModeSelectionScreenEffects();
				return;
			}
			WonderPower byName = WonderPowers.GetByName(powerName);
			if (byName != null)
			{
				bool flag = false;
				flag |= !byName.IsLocked;
				if (flag & (byName.Cost() <= WonderPowers.GetKarma()))
				{
					byName.Run(WonderPowerActivation.ActivationType.UserSelected, null);
				}
			}
			EndDialog(0u);
		}

		private void StartWonderModeSelectionScreenEffects()
		{
			if (mEdgeEffect == null)
			{
				mEdgeEffect = VisualEffect.Create("wonderModeSelectionEffect");
				mEdgeEffect.Start();
			}
		}

		private void StopWonderModeSelectionScreenEffects()
		{
			if (mEdgeEffect != null)
			{
				mEdgeEffect.Stop();
				mEdgeEffect.Dispose();
				mEdgeEffect = null;
			}
		}
	}

	public class KarmaDial
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
	}
}
