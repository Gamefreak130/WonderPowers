using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.WorldBuilderUtil;
using Sims3.SimIFace;
using Sims3.SimIFace.Enums;
using Sims3.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Environment = System.Environment;
using Responder = Sims3.UI.Responder;

namespace Gamefreak130.Common
{
    public delegate T GenericDelegate<T>();

    public class RepeatingFunctionTask : Task
    {
        private StopWatch mTimer;

        private readonly int mDelay;

        private readonly GenericDelegate<bool> mFunction;

        public RepeatingFunctionTask(GenericDelegate<bool> function) : this(function, 500)
        {
        }

        public RepeatingFunctionTask(GenericDelegate<bool> function, int delay)
        {
            mFunction = function;
            mDelay = delay;
        }

        public override void Dispose()
        {
            mTimer?.Dispose();
            mTimer = null;
            if (ObjectId != ObjectGuid.InvalidObjectGuid)
            {
                Simulator.DestroyObject(ObjectId);
                ObjectId = ObjectGuid.InvalidObjectGuid;
            }
            base.Dispose();
        }

        public override void Simulate()
        {
            try
            {
                mTimer = StopWatch.Create(StopWatch.TickStyles.Milliseconds);
                mTimer.Start();
                do
                {
                    mTimer.Restart();
                    while (mTimer?.GetElapsedTime() < mDelay)
                    {
                        if (Simulator.CheckYieldingContext(false))
                        {
                            Simulator.Sleep(0u);
                        }
                    }
                    if (!mFunction())
                    {
                        break;
                    }
                    if (Simulator.CheckYieldingContext(false))
                    {
                        Simulator.Sleep(0u);
                    }
                }
                while (mTimer is not null);
            }
            catch (Exception e)
            {
                ExceptionLogger.sInstance.Log(e);
            }
            finally
            {
                Dispose();
            }
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
                if (interactionTuning is null)
                {
                    interactionTuning = AutonomyTuning.GetTuning(oldType, oldType.FullName, oldTarget);
                    if (interactionTuning is null)
                    {
                        return null;
                    }
                    if (clone)
                    {
                        interactionTuning = CloneTuning(interactionTuning);
                    }
                    AutonomyTuning.AddTuning(newType.FullName, newTarget.FullName, interactionTuning);
                }
                InteractionObjectPair.sTuningCache.Remove(new(newType, newTarget));
            }
            catch (Exception)
            {
            }
            result = interactionTuning;
            return result;
        }

        private static InteractionTuning CloneTuning(InteractionTuning oldTuning) => new()
        {
            mFlags = oldTuning.mFlags,
            ActionTopic = oldTuning.ActionTopic,
            AlwaysChooseBest = oldTuning.AlwaysChooseBest,
            Availability = CloneAvailability(oldTuning.Availability),
            CodeVersion = oldTuning.CodeVersion,
            FullInteractionName = oldTuning.FullInteractionName,
            FullObjectName = oldTuning.FullObjectName,
            mChecks = Helpers.CloneList(oldTuning.mChecks),
            mTradeoff = CloneTradeoff(oldTuning.mTradeoff),
            PosturePreconditions = oldTuning.PosturePreconditions,
            ScoringFunction = oldTuning.ScoringFunction,
            ScoringFunctionOnlyAppliesToSpecificCommodity = oldTuning.ScoringFunctionOnlyAppliesToSpecificCommodity,
            ScoringFunctionString = oldTuning.ScoringFunctionString,
            ShortInteractionName = oldTuning.ShortInteractionName,
            ShortObjectName = oldTuning.ShortObjectName
        };

        private static Tradeoff CloneTradeoff(Tradeoff old) => new()
        {
            mFlags = old.mFlags,
            mInputs = Helpers.CloneList(old.mInputs),
            mName = old.mName,
            mNumParameters = old.mNumParameters,
            mOutputs = Helpers.CloneList(old.mOutputs),
            mVariableRestrictions = old.mVariableRestrictions,
            TimeEstimate = old.TimeEstimate
        };

        private static Availability CloneAvailability(Availability old) => new()
        {
            mFlags = old.mFlags,
            AgeSpeciesAvailabilityFlags = old.AgeSpeciesAvailabilityFlags,
            CareerThresholdType = old.CareerThresholdType,
            CareerThresholdValue = old.CareerThresholdValue,
            ExcludingBuffs = Helpers.CloneList(old.ExcludingBuffs),
            ExcludingTraits = Helpers.CloneList(old.ExcludingTraits),
            MoodThresholdType = old.MoodThresholdType,
            MoodThresholdValue = old.MoodThresholdValue,
            MotiveThresholdType = old.MotiveThresholdType,
            MotiveThresholdValue = old.MotiveThresholdValue,
            RequiredBuffs = Helpers.CloneList(old.RequiredBuffs),
            RequiredTraits = Helpers.CloneList(old.RequiredTraits),
            SkillThresholdType = old.SkillThresholdType,
            SkillThresholdValue = old.SkillThresholdValue,
            WorldRestrictionType = old.WorldRestrictionType,
            OccultRestrictions = old.OccultRestrictions,
            OccultRestrictionType = old.OccultRestrictionType,
            SnowLevelValue = old.SnowLevelValue,
            WorldRestrictionWorldNames = Helpers.CloneList(old.WorldRestrictionWorldNames),
            WorldRestrictionWorldTypes = Helpers.CloneList(old.WorldRestrictionWorldTypes)
        };
    }

    public class BuffBooter
    {
        public string mXmlResource;

        public BuffBooter(string xmlResource) => mXmlResource = xmlResource;

        public void LoadBuffData()
        {
            AddBuffs(null);
            UIManager.NewHotInstallStoreBuffData += AddBuffs;
        }

        public void AddBuffs(ResourceKey[] resourceKeys)
        {
            if (XmlDbData.ReadData(mXmlResource) is XmlDbData xmlDbData)
            {
                BuffManager.ParseBuffData(xmlDbData, true);
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

        protected static readonly string sName;

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
                    CustomXmlWriter xmlWriter = new(fileHandle);
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
            StringBuilder xmlBuilder = new();
            xmlBuilder.AppendLine($"<{sName}>");
            xmlBuilder.AppendLine($"<ModVersion value=\"{sModVersion}\"/>");
            xmlBuilder.AppendLine($"<GameVersion value=\"{sGameVersionData[0]} ({sGameVersionData[5]}) ({sGameVersionData[7]})\"/>");
            xmlBuilder.AppendLine($"<InstalledPacks value=\"{GameUtils.sProductFlags}\"/>");
            // The logger expects the content to have a new line at the end of it
            // More new lines are appended here to create exactly one line of padding before and after the XML tags
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
            StringBuilder result = new();
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
        public override void Log(T input) => WriteLog(new(input.ToString()));

        protected override void WriteLog(StringBuilder content, string fileName)
        {
            StringBuilder log = new();
            log.AppendLine("Logged At:");
            log.AppendLine($" Sim Time: {SimClock.CurrentTime()}");
            log.AppendLine(" Real Time: " + DateTime.Now + Environment.NewLine);
            log.Append(content);
            base.WriteLog(content, fileName);
        }
    }

    public class ExceptionLogger : EventLogger<Exception>
    {
        private ExceptionLogger()
        {
        }

        internal static readonly ExceptionLogger sInstance = new();

        protected override void Notify() => StyledNotification.Show(new($"Error occurred in {sName}\n\nAn error log has been created in your user directory. Please send it to Gamefreak130 for further review.", StyledNotification.NotificationStyle.kSystemMessage));
    }

    /// <summary>Transfers (or "Ferries") values of PersistableStatic type members ("Cargo") across worlds when traveling</summary>
    /// <remarks><para>Using the Ferry, one copy of a type's PersistableStatic data can be shared across multiple worlds in a save,
    /// as opposed to each world creating and maintaining its own separate copy.</para>
    /// <para>Client code is responsible for setting any default values for Cargo after it has been loaded,
    /// should such values be necessary for new games or newly-exposed saves.</para>
    /// <para>Types derived from <typeparamref name="T">T</typeparamref> and types from which <typeparamref name="T">T</typeparamref> is derived 
    /// will not have their declared Cargo saved unless a separate Ferry is called for them as well.</para></remarks>
    /// <typeparam name="T">The type containing PersistableStatic data to be ferried</typeparam>
    /// <exception cref="NotSupportedException"><typeparamref name="T">T</typeparamref> does not contain PersistableStatic members</exception>
    public static class Ferry<T>
    {
        private static readonly Dictionary<FieldInfo, object> mCargo;

        static Ferry()
        {
            FieldInfo[] fields = FindPersistableStatics();
            if (fields.Length == 0)
            {
                throw new NotSupportedException($"There are no PersistableStatic fields declared in {typeof(T)}.");
            }
            mCargo = new(fields.Length);
            foreach (FieldInfo current in fields)
            {
                mCargo[current] = null;
            }
        }

        private static FieldInfo[] FindPersistableStatics()
        {
            MemberInfo[] fieldMembers = typeof(T).FindMembers(MemberTypes.Field,
                BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic,
                (info, criteria) => info.GetCustomAttributes(typeof(PersistableStaticAttribute), false).Length > 0, null);

            return Array.ConvertAll(fieldMembers, (x) => (FieldInfo)x);
        }

        public static void UnloadCargo()
        {
            if (GameStates.IsTravelling)
            {
                foreach (FieldInfo current in new List<FieldInfo>(mCargo.Keys))
                {
                    current.SetValue(null, mCargo[current]);
                    mCargo[current] = null;
                }
            }
        }

        public static void LoadCargo()
        {
            if (GameStates.IsTravelling)
            {
                foreach (FieldInfo current in new List<FieldInfo>(mCargo.Keys))
                {
                    mCargo[current] = current.GetValue(null);
                }
            }
        }
    }

    public static class Helpers
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
            if (definition is null)
            {
                definition = new(socialName, new string[0], null, false);
            }
            InteractionInstance instance = definition.CreateInstance(target, actor, new(priority), false, isCancellable);
            actor.InteractionQueue.Add(instance);
        }

        public static List<T> CloneList<T>(IEnumerable<T> old) => old is not null ? new(old) : null;

        public static object CoinFlipSelect(object obj1, object obj2) => RandomUtil.CoinFlip() ? obj1 : obj2;
    }
}

namespace Gamefreak130.Common.Buffs
{
    /// <summary>An extension of the BuffTemporaryTrait class which supports the addition of more than one trait. It also allows for the addition of hidden/reward traits.</summary>
    public abstract class BuffTemporaryTraitEx : Buff
    {
        public class BuffInstanceTemporaryTraitEx : BuffInstance
        {
            private SimDescription mTargetSim;

            public List<TraitNames> TraitsAdded { get; } = new();

            public List<TraitNames> TraitsRemoved { get; } = new();

            public BuffInstanceTemporaryTraitEx()
            {
            }

            public BuffInstanceTemporaryTraitEx(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount) : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            public override BuffInstance Clone() => new BuffInstanceTemporaryTraitEx(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);

            public override void SetTargetSim(SimDescription targetSim) => mTargetSim = targetSim;

            public void AddTemporaryTrait(TraitNames trait) => AddTemporaryTrait(trait, false);

            public void AddTemporaryTrait(TraitNames trait, bool hidden)
            {
                TraitManager traitManager = mTargetSim.TraitManager;
                if (traitManager.HasElement(trait) || (!hidden && TraitsAdded.FindAll(guid => TraitManager.GetTraitFromDictionary(guid).IsVisible).Count == traitManager.CountVisibleTraits()))
                {
                    return;
                }
                List<Trait> conflictingTraits = traitManager.GetDictionaryConflictingTraits(trait).FindAll(x => mTargetSim.HasTrait(x.Guid));
                if (conflictingTraits.Count > 0)
                {
                    foreach (Trait conflictingTrait in conflictingTraits)
                    {
                        TraitsRemoved.Add(conflictingTrait.Guid);
                        traitManager.RemoveElement(conflictingTrait.Guid);
                    }
                }
                else if (!hidden)
                {
                    TraitNames randomVisibleElement;
                    do
                    {
                        randomVisibleElement = traitManager.GetRandomVisibleElement().Guid;
                    }
                    while (TraitsAdded.Contains(randomVisibleElement));
                    TraitsRemoved.Add(randomVisibleElement);
                    traitManager.RemoveElement(randomVisibleElement);
                }
                if (traitManager.CanAddTrait((ulong)trait) && traitManager.AddElement(trait))
                {
                    TraitsAdded.Add(trait);
                    Trait addedTrait = TraitManager.GetTraitFromDictionary(trait);
                    if (hidden && addedTrait.IsReward)
                    {
                        traitManager.mRewardTraits.Remove(addedTrait);
                    }
                }
                if (hidden)
                {
                    Sims3.UI.Hud.RewardTraitsPanel.Instance?.PopulateTraits();
                }
                if (mTargetSim.CreatedSim is not null)
                {
                    (Responder.Instance.HudModel as HudModel).OnSimAgeChanged(mTargetSim.CreatedSim.ObjectId);
                }
            }
        }

        public BuffTemporaryTraitEx(BuffData info) : base(info)
        {
        }

        public override BuffInstance CreateBuffInstance() => new BuffInstanceTemporaryTraitEx(this, BuffGuid, EffectValue, TimeoutSimMinutes);

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            BuffInstanceTemporaryTraitEx buffInstanceTemporaryTrait = bi as BuffInstanceTemporaryTraitEx;
            TraitManager traitManager = bm.Actor.TraitManager;
            foreach (TraitNames guid in buffInstanceTemporaryTrait.TraitsAdded)
            {
                RemoveElement(traitManager, guid);
            }
            foreach (TraitNames guid in buffInstanceTemporaryTrait.TraitsRemoved)
            {
                traitManager.AddElement(guid);
            }
            buffInstanceTemporaryTrait.TraitsAdded.Clear();
            buffInstanceTemporaryTrait.TraitsRemoved.Clear();
            (Responder.Instance.HudModel as HudModel).OnSimAgeChanged(bm.Actor.ObjectId);
        }

        private static void RemoveElement(TraitManager traitManager, TraitNames guid)
        {
            Trait traitFromDictionary = TraitManager.GetTraitFromDictionary(guid);
            if (traitFromDictionary.TraitListener is not null)
            {
                traitFromDictionary.TraitListener.Remove();
            }
            if (traitFromDictionary != null && traitFromDictionary.IsReward)
            {
                traitManager.mRewardTraits.Remove(traitFromDictionary);
                //this.mSimDescription.IncrementLifetimeHappiness((float)traitFromDictionary.Score);
            }
            Sim actor = traitManager.Actor;
            if (actor is not null)
            {
                actor.UpdateSacsParametersForTraitOrBuff(typeof(TraitNames), (ulong)guid, YesOrNo.no);
                actor.SocialComponent.UpdateTraits();
                if (traitFromDictionary.Guid is TraitNames.FutureSim && !actor.BuffManager.HasElement(BuffNames.EmbracingTheFuture))
                {
                    actor.SkillManager.SubtractFromSkillGainModifier(SkillNames.Future, BuffEmbracingTheFuture.kEmbracingFutureSkillMultiplier - 1f);
                }
                if (traitFromDictionary.Guid is TraitNames.FutureSimLHR)
                {
                    actor.TraitManager.RemoveElement(TraitNames.FutureSim);
                }
            }
            if (traitManager.mValues.TryGetValue((ulong)guid, out Trait trait))
            {
                traitManager.mValues.Remove((ulong)guid);
                traitManager.OnRemoved(trait);
            }
            if (!CharacterImportOnGameLoad.InProgress)
            {
                traitManager.AddDesireAlarm();
                MetaAutonomyManager.UpdatePreferredVenuesForSim(actor);
            }
        }
    }
}