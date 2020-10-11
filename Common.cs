using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
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

        private void AddBuffs(ResourceKey[] resourceKeys)
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
            xmlBuilder.AppendLine($"<GameVersion value=\"{sGameVersionData[0]} ({sGameVersionData[5]}) ({sGameVersionData[7]})\"/>");
            xmlBuilder.AppendLine($"<InstalledPacks value=\"{GameUtils.sProductFlags}\"/>");
            // The logger expects the content to have a new line at the end of it
            // More new lines are appended here to create exactly one line of padding before and after the XML tags
            xmlBuilder.AppendLine("<Content>" + System.Environment.NewLine);
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

    /// <summary>Logger for one-time events.</summary>
    /// <remarks>Any received input to log is immediately converted to a string and written to a new log file,
    /// along with timestamps and the rest of the standard log info.</remarks>
    public abstract class EventLogger<T> : Logger<T>
    {
        public override void Log(T input) => WriteLog(new StringBuilder(input.ToString()));

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

    /// <summary>Transfers (or "Ferries") PersistableStatic type members ("Cargo") across worlds upon travelling, starting a new game, or loading a different save.</summary>
    /// <remarks><para>Using the Ferry, only one copy of a type's PersistableStatic data can carry across multiple worlds,
    /// as opposed to each world creating and maintaining its own separate copy.</para>
    /// <para>Only one static instance of a Ferry should exist per associated type.</para></remarks>
    /// <typeparam name="T">The type containing PersistableStatic data to be ferried</typeparam>
    public class Ferry<T> where T : class
    {
        private readonly Dictionary<FieldInfo, object> mCargo;

        public Ferry()
        {
            FieldInfo[] fields = FindPersistableStatics();
            if (fields.Length == 0)
            {
                throw new NotSupportedException($"There are no PersistableStatic fields declared in {typeof(T)}.");
            }
            mCargo = new Dictionary<FieldInfo, object>(fields.Length);
            foreach (FieldInfo current in fields)
            {
                mCargo[current] = null;
            }
        }

        private FieldInfo[] FindPersistableStatics()
        {
            MemberInfo[] fieldMembers = typeof(T).FindMembers(MemberTypes.Field,
                BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic,
                (info, criteria) => info.GetCustomAttributes(typeof(PersistableStaticAttribute), false).Length > 0, null);

            return Array.ConvertAll(fieldMembers, (x) => (FieldInfo)x);
        }

        public void UnloadCargo()
        {
            foreach (FieldInfo current in new List<FieldInfo>(mCargo.Keys))
            {
                current.SetValue(null, mCargo[current]);
                mCargo[current] = null;
            }
        }

        public void LoadCargo()
        {
            foreach (FieldInfo current in new List<FieldInfo>(mCargo.Keys))
            {
                mCargo[current] = current.GetValue(null);
            }
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
