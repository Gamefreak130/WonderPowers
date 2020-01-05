using System;
using System.Collections.Generic;
using System.Text;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Hud;

namespace Gamefreak130.WonderPowersSpace.Helpers.UI
{
	public class WonderModeMenu
	{
		/*private enum TriggerCode : uint
		{
			Up = 114345184u,
			Down = 114345185u,
			Left = 114345186u,
			Right = 114345187u,
			Select = 114345188u,
			Cancel = 114345189u,
			ScrollTextUp = 114345200u,
			ScrollTextDown = 114345201u
		}*/

		public delegate void PowerSelectHandler(string power);

		public static bool sIncrementalButtonIndexing = false;

		public static string[] mWonderPowerNames;

		public static bool[] mWonderPowerStates;

		public static int[] mWonderPowerCosts;

		public static float mKarma;

		private static WonderModeMenu sInstance = null;

		private static ScHudWonderMode wonderModeHUD;

		private static bool ignoreInput;

		public static PowerSelectHandler PowerSelected;

		private bool mbVisible;

		private uint mTriggerHandle;

		private bool mbMovieLoaded;

		public static bool IsVisible
		{
			get
			{
				return sInstance != null ? sInstance.mbVisible : false;
			}
		}

		internal static WonderModeMenu Instance
		{
			get
			{
				return sInstance;
			}
		}

		public static void Show()
		{
			if (sInstance != null)
			{
				bool mbMovieLoaded2 = sInstance.mbMovieLoaded;
				sInstance.mbVisible = true;
				SceneMgrWindow sceneWindow = UIManager.GetSceneWindow();
				if (sceneWindow != null)
				{
					sInstance.mTriggerHandle = sceneWindow.AddModalTriggerHook("wondermenu", TriggerActivationMode.kPermanent, 17);
					sceneWindow.TriggerDown += sInstance.OnTriggerDown;
					sceneWindow.TriggerUp += sInstance.OnTriggerUp;
				}
				ignoreInput = false;
				//ScaleformManager.WonderModeInit(mWonderPowerStates[0], mWonderPowerStates[1], mWonderPowerStates[2], mWonderPowerStates[3], mWonderPowerStates[4], mWonderPowerStates[5], mWonderPowerStates[6], mWonderPowerStates[7], mWonderPowerStates[8], mWonderPowerStates[9], mWonderPowerStates[10], mWonderPowerStates[11], mWonderPowerStates[12], mWonderPowerCosts[0], mWonderPowerCosts[1], mWonderPowerCosts[2], mWonderPowerCosts[3], mWonderPowerCosts[4], mWonderPowerCosts[5], mWonderPowerCosts[6], mWonderPowerCosts[7], mWonderPowerCosts[8], mWonderPowerCosts[9], mWonderPowerCosts[10], mWonderPowerCosts[11], mWonderPowerCosts[12], (int)mKarma);
			}
		}

		public static void Hide()
		{
			if (sInstance != null)
			{
				sInstance.mbVisible = false;
				SceneMgrWindow sceneWindow = UIManager.GetSceneWindow();
				if (sceneWindow != null)
				{
					sceneWindow.RemoveTriggerHook(sInstance.mTriggerHandle);
					sceneWindow.TriggerDown -= sInstance.OnTriggerDown;
					sceneWindow.TriggerUp -= sInstance.OnTriggerUp;
				}
				Unload();
			}
		}

		public static bool CreateInstance()
		{
			if (sInstance == null)
			{
				sInstance = new WonderModeMenu();
				return true;
			}
			return false;
		}

		public static void Load()
		{
			CreateInstance();
			wonderModeHUD = ScHudWonderMode.Instance;
			wonderModeHUD.MovieLoadedEvent = sInstance.MovieLoaded;
			wonderModeHUD.MovieDoneEvent = sInstance.MovieDone;
			wonderModeHUD.LoadSWF();
		}

		public static void Unload()
		{
			wonderModeHUD.UnloadSWF();
			sInstance = null;
		}

		public static void SetPowerState(string name, bool state, int cost)
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

		public static void SetKarma(float karma)
		{
			mKarma = karma;
		}

		private void MovieLoaded()
		{
			mbMovieLoaded = true;
			Show();
		}

		public void MovieDone()
		{
			Hide();
		}

		/*private void OnTriggerDown(WindowBase sender, UITriggerEventArgs eventArgs)
		{
			if (ignoreInput)
			{
				return;
			}
			switch (eventArgs.TriggerCode)
			{
				case 114345185u:
					ScaleformManager.ScaleformInvoke("WonderMode", "_root.WonderMode_Panel.WMP_SELECTDOWN");
					break;
				case 114345184u:
					ScaleformManager.ScaleformInvoke("WonderMode", "_root.WonderMode_Panel.WMP_SELECTUP");
					break;
				case 114345186u:
					ScaleformManager.ScaleformInvoke("WonderMode", "_root.WonderMode_Panel.WMP_SELECTLEFT");
					break;
				case 114345187u:
					ScaleformManager.ScaleformInvoke("WonderMode", "_root.WonderMode_Panel.WMP_SELECTRIGHT");
					break;
				case 114345188u:
					{
						ScaleformManager.ScaleformInvoke("WonderMode", "_root.WonderMode_Panel.WMP_POWERSELECTED");
						eventArgs.Handled = true;
						int num = -1;
						ignoreInput = true;
						if (ScaleformManager.WonderModeGetSelected(ref num))
						{
							PowerSelected(mWonderPowerNames[num]);
						}
						break;
					}
				case 114345200u:
					ScaleformManager.ScaleformInvoke("WonderMode", "_root.WonderMode_Panel.WMP_RSUP");
					break;
				case 114345201u:
					ScaleformManager.ScaleformInvoke("WonderMode", "_root.WonderMode_Panel.WMP_RSDOWN");
					break;
				case 114345189u:
					if (mbVisible)
					{
						Hide();
						PowerSelected(null);
						eventArgs.Handled = true;
					}
					break;
			}
		}*/

		public static void PowerSelectFailed()
		{
			ignoreInput = false;
		}

		/*private void OnTriggerUp(WindowBase sender, UITriggerEventArgs eventArgs)
		{
			switch (eventArgs.TriggerCode)
			{
			}
		}*/

		private WonderModeMenu()
		{
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
			ignoreInput = false;
		}
	}
}
