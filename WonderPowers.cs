﻿using Gamefreak130.Common;
using Gamefreak130.WonderPowersSpace.Helpers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Environment = System.Environment;

namespace Gamefreak130.Common
{
    public delegate T GenericDelegate<T>();

    public class RepeatingFunctionTask : Task
    {
        private StopWatch mTimer;

        private readonly int mDelay;

        private readonly GenericDelegate<bool> mFunction;

        public RepeatingFunctionTask(GenericDelegate<bool> function)
        {
            mFunction = function;
            mDelay = 500;
        }

        public RepeatingFunctionTask(GenericDelegate<bool> function, int delay)
        {
            mFunction = function;
            mDelay = delay;
        }

        public override void Dispose()
        {
            if (mTimer != null)
            {
                mTimer.Dispose();
                mTimer = null;
            }
            if (ObjectId != ObjectGuid.InvalidObjectGuid)
            {
                Simulator.DestroyObject(ObjectId);
                ObjectId = ObjectGuid.InvalidObjectGuid;
            }
            base.Dispose();
        }

        public override void Simulate()
        {
            mTimer = StopWatch.Create(StopWatch.TickStyles.Milliseconds);
            mTimer.Start();
            do
            {
                mTimer.Restart();
                while (mTimer != null && mTimer.GetElapsedTime() < mDelay)
                {
                    if (Simulator.CheckYieldingContext(false))
                    {
                        Simulator.Sleep(0u);
                    }
                }
                if (!mFunction())
                {
                    Dispose();
                    break;
                }
                if (Simulator.CheckYieldingContext(false))
                {
                    Simulator.Sleep(0u);
                }
            }
            while (mTimer != null);
        }
    }

    public static class Tunings
    {
        internal static InteractionTuning Inject(Type oldType, Type oldTarget, Type newType, Type newTarget, bool clone)
        {
            InteractionTuning interactionTuning = null;
            InteractionTuning result;
            try
            {
                interactionTuning = AutonomyTuning.GetTuning(newType.FullName, newTarget.FullName);
                bool flag = interactionTuning == null;
                if (flag)
                {
                    interactionTuning = AutonomyTuning.GetTuning(oldType, oldType.FullName, oldTarget);
                    bool flag2 = interactionTuning == null;
                    if (flag2)
                    {
                        result = null;
                        return result;
                    }
                    if (clone)
                    {
                        interactionTuning = CloneTuning(interactionTuning);
                    }
                    AutonomyTuning.AddTuning(newType.FullName, newTarget.FullName, interactionTuning);
                }
                InteractionObjectPair.sTuningCache.Remove(new Pair<Type, Type>(newType, newTarget));
            }
            catch (Exception)
            {
            }
            result = interactionTuning;
            return result;
        }

        private static InteractionTuning CloneTuning(InteractionTuning oldTuning) => new InteractionTuning
        {
            mFlags = oldTuning.mFlags,
            ActionTopic = oldTuning.ActionTopic,
            AlwaysChooseBest = oldTuning.AlwaysChooseBest,
            Availability = CloneAvailability(oldTuning.Availability),
            CodeVersion = oldTuning.CodeVersion,
            FullInteractionName = oldTuning.FullInteractionName,
            FullObjectName = oldTuning.FullObjectName,
            mChecks = Methods.CloneList(oldTuning.mChecks),
            mTradeoff = CloneTradeoff(oldTuning.mTradeoff),
            PosturePreconditions = oldTuning.PosturePreconditions,
            ScoringFunction = oldTuning.ScoringFunction,
            ScoringFunctionOnlyAppliesToSpecificCommodity = oldTuning.ScoringFunctionOnlyAppliesToSpecificCommodity,
            ScoringFunctionString = oldTuning.ScoringFunctionString,
            ShortInteractionName = oldTuning.ShortInteractionName,
            ShortObjectName = oldTuning.ShortObjectName
        };

        private static Tradeoff CloneTradeoff(Tradeoff old) => new Tradeoff
        {
            mFlags = old.mFlags,
            mInputs = Methods.CloneList(old.mInputs),
            mName = old.mName,
            mNumParameters = old.mNumParameters,
            mOutputs = Methods.CloneList(old.mOutputs),
            mVariableRestrictions = old.mVariableRestrictions,
            TimeEstimate = old.TimeEstimate
        };

        private static Availability CloneAvailability(Availability old) => new Availability
        {
            mFlags = old.mFlags,
            AgeSpeciesAvailabilityFlags = old.AgeSpeciesAvailabilityFlags,
            CareerThresholdType = old.CareerThresholdType,
            CareerThresholdValue = old.CareerThresholdValue,
            ExcludingBuffs = Methods.CloneList(old.ExcludingBuffs),
            ExcludingTraits = Methods.CloneList(old.ExcludingTraits),
            MoodThresholdType = old.MoodThresholdType,
            MoodThresholdValue = old.MoodThresholdValue,
            MotiveThresholdType = old.MotiveThresholdType,
            MotiveThresholdValue = old.MotiveThresholdValue,
            RequiredBuffs = Methods.CloneList(old.RequiredBuffs),
            RequiredTraits = Methods.CloneList(old.RequiredTraits),
            SkillThresholdType = old.SkillThresholdType,
            SkillThresholdValue = old.SkillThresholdValue,
            WorldRestrictionType = old.WorldRestrictionType,
            OccultRestrictions = old.OccultRestrictions,
            OccultRestrictionType = old.OccultRestrictionType,
            SnowLevelValue = old.SnowLevelValue,
            WorldRestrictionWorldNames = Methods.CloneList(old.WorldRestrictionWorldNames),
            WorldRestrictionWorldTypes = Methods.CloneList(old.WorldRestrictionWorldTypes)
        };
    }

    public class BuffBooter
    {
        private readonly string mXmlResource;

        public BuffBooter(string xmlResource) => mXmlResource = xmlResource;

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

    public abstract class Logger<T>
    {
        static Logger()
        {
            Assembly assembly = typeof(Logger<T>).Assembly;
            sName = assembly.GetName().Name;
            sModVersion = (Attribute.GetCustomAttribute(assembly, typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute).Version;
            sGameVersionData = GameUtils.GetGenericString(GenericStringID.VersionData).Split('\n');
        }

        private static readonly string sName;

        private static readonly string sModVersion;

        private static readonly string[] sGameVersionData;

        public abstract void Log(T input);

        protected void WriteLog(StringBuilder content) => WriteLog(content, $"ScriptError_{sName}_{DateTime.Now:M-d-yyyy_hh-mm-ss}__");

        protected virtual void WriteLog(StringBuilder content, string fileName)
        {
            uint fileHandle = 0;
            try
            {
                Simulator.CreateExportFile(ref fileHandle, fileName);
                if (fileHandle != 0)
                {
                    CustomXmlWriter xmlWriter = new CustomXmlWriter(fileHandle);
                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteToBuffer(GenerateXmlWrapper(content));
                    xmlWriter.FlushBufferToFile();
                }
                Notify();
            }
            finally
            {
                if (fileHandle != 0)
                {
                    Simulator.CloseScriptErrorFile(fileHandle);
                }
            }
        }

        private string GenerateXmlWrapper(StringBuilder content)
        {
            StringBuilder xmlBuilder = new StringBuilder();
            xmlBuilder.AppendLine($"<{sName}>");
            xmlBuilder.AppendLine($"<ModVersion value=\"{sModVersion}\"/>");
            xmlBuilder.AppendLine($"<GameVersion value=\"{sGameVersionData[0]} ({sGameVersionData[5]}) ({sGameVersionData[7]})\"");
            xmlBuilder.AppendLine($"<InstalledPacks value=\"{GameUtils.sProductFlags}\"/>");
            // The logger expects the content to have a new line at the end of it
            // A more new lines are appended here to create exactly one line of padding before and after the XML tags
            xmlBuilder.AppendLine("<Content>" + Environment.NewLine);
            xmlBuilder.Append(content.Replace("&", "&amp;"));
            xmlBuilder.AppendLine(Environment.NewLine + "</Content>");
            xmlBuilder.AppendLine("<LoadedAssemblies>");
            xmlBuilder.Append(GenerateAssemblyList());
            xmlBuilder.AppendLine("</LoadedAssemblies>");
            xmlBuilder.Append($"</{sName}>");
            return xmlBuilder.ToString();
        }

        private StringBuilder GenerateAssemblyList()
        {
            StringBuilder result = new StringBuilder();
            List<string> assemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies())
                                            .ConvertAll((assembly) => assembly.GetName().Name);
            assemblies.Sort();
            foreach (string assembly in assemblies)
            {
                result.AppendLine(" " + assembly);
            }
            return result;
        }

        protected virtual void Notify()
        {
        }
    }

    public abstract class EventLogger<T> : Logger<T>
    {
        public override void Log(T @event) => WriteLog(new StringBuilder(@event.ToString()));

        protected override void WriteLog(StringBuilder content, string fileName)
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine("Logged At:");
            log.AppendLine(" Sim Time: " + SimClock.CurrentTime());
            log.AppendLine(" Real Time: " + DateTime.Now + Environment.NewLine);
            log.Append(content);
            base.WriteLog(content, fileName);
        }
    }

    public static class Methods
    {
        public static void ForceSocial(Sim actor, Sim target, string socialName, InteractionPriorityLevel priority, bool isCancellable)
        {
            SocialInteractionA.Definition definition = null;
            foreach (InteractionObjectPair iop in target.Interactions)
            {
                if (iop.InteractionDefinition is SocialInteractionA.Definition social && social.ActionKey == socialName)
                {
                    definition = social;
                }
            }
            if (definition == null)
            {
                definition = new SocialInteractionA.Definition(socialName, new string[0], null, false);
            }
            InteractionInstance instance = definition.CreateInstance(target, actor, new InteractionPriority(priority), false, isCancellable);
            actor.InteractionQueue.Add(instance);
        }

        public static List<T> CloneList<T>(IEnumerable<T> old)
        {
            bool flag = old != null;
            List<T> result = flag ? new List<T>(old) : null;
            return result;
        }
    }
}

namespace Gamefreak130
{
    //TODO Cleanup LAYO
    //TODO Check code for unused blocks, zero references, notimplementedexceptions
    //TODO Find way to dynamically load XML for custom powers
    //TODO Clear statics on quit to menu, persist on travel
    //TODO Command to set karma
    //CONSIDER Powers implement IWeightable for karmic backlash selection
    public static class WonderPowers
    {
        [Tunable]
        private static readonly bool kCJackB;

        static WonderPowers()
        {
            World.OnStartupAppEventHandler += OnStartupApp;
            World.OnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            LoadSaveManager.ObjectGroupsPostLoad += OnPostLoad;
            World.OnWorldQuitEventHandler += OnWorldQuit;
        }

        private static void OnStartupApp(object sender, EventArgs e)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool flag = Array.Exists(assemblies, (assembly) => assembly.FullName.Contains("Gamefreak130.LTRMenuMusicReplacement"));
            if (!flag)
            {
                Simulator.AddObject(new RepeatingFunctionTask(OptionsInjector.InjectOptions));
            }
        }

        private static void OnPreLoad()
        {
            WonderPowersSpace.Helpers.WonderPowers.PreWorldLoadStartup();
            WonderPower.LoadPowers("KarmaPowers");
            new BuffBooter("Gamefreak130_KarmaBuffs").LoadBuffData();
            Tunings.Inject(GoToLot.Singleton.GetType(), typeof(Lot), typeof(WonderPowersSpace.Interactions.GoToLotAndFight), typeof(Lot), true);
            Tunings.Inject(Urnstone.ResurrectSim.Singleton.GetType(), typeof(Sim), typeof(WonderPowersSpace.Interactions.DivineInterventionResurrect), typeof(Sim), true);
        }

        private static void OnPostLoad()
        {
        }

        private static void OnWorldLoadFinished(object sender, EventArgs e)
        {
            EventTracker.AddListener(EventTypeId.kEnterInWorldSubState, OnEnteredWorld);
        }

        private static void OnWorldQuit(object sender, EventArgs e)
        {
            WonderPowersSpace.Helpers.WonderPowers.WorldLoadShutdown();
        }

        private static ListenerAction OnEnteredWorld(Event e)
        {
            if (Sims3.UI.Hud.RewardTraitsPanel.Instance?.GetChildByID(799350305u, true) is Button button)
            {
                button.Click += (sender, eventArgs) =>
                {
                    Simulator.AddObject(new OneShotFunctionTask(WonderPowersSpace.Helpers.UI.WonderModeMenu.Show));
                    eventArgs.Handled = true;
                };
                //TEST save + reload, travel, etc. while powers running
                button.Enabled = !WonderPowersSpace.Helpers.WonderPowers.IsPowerRunning;
            }
            return ListenerAction.Remove;
        }
    }
}