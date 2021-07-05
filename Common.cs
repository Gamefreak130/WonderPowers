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
using System.Linq;
using System.Reflection;
using System.Text;
using Environment = System.Environment;
using Responder = Sims3.UI.Responder;

namespace Gamefreak130.Common
{
    public abstract class CommonTask : Task 
    {
        public override void Dispose()
        {
            if (ObjectId != ObjectGuid.InvalidObjectGuid)
            {
                Simulator.DestroyObject(ObjectId);
                ObjectId = ObjectGuid.InvalidObjectGuid;
            }
            base.Dispose();
        }

        protected abstract void Run();

        public override void Simulate()
        {
            try
            {
                Run();
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

    public class RepeatingFunctionTask : CommonTask
    {
        private StopWatch mTimer;

        private readonly int mDelay;

        private readonly Func<bool> mFunction;

        public RepeatingFunctionTask(Func<bool> function) : this(function, 500)
        {
        }

        public RepeatingFunctionTask(Func<bool> function, int delay)
        {
            mFunction = function;
            mDelay = delay;
        }

        public override void Dispose()
        {
            mTimer?.Dispose();
            mTimer = null;
            base.Dispose();
        }

        protected override void Run()
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
            mChecks = new(oldTuning.mChecks),
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
            mInputs = new(old.mInputs),
            mName = old.mName,
            mNumParameters = old.mNumParameters,
            mOutputs = new(old.mOutputs),
            mVariableRestrictions = old.mVariableRestrictions,
            TimeEstimate = old.TimeEstimate
        };

        private static Availability CloneAvailability(Availability old) => new()
        {
            mFlags = old.mFlags,
            AgeSpeciesAvailabilityFlags = old.AgeSpeciesAvailabilityFlags,
            CareerThresholdType = old.CareerThresholdType,
            CareerThresholdValue = old.CareerThresholdValue,
            ExcludingBuffs = new(old.ExcludingBuffs),
            ExcludingTraits = new(old.ExcludingTraits),
            MoodThresholdType = old.MoodThresholdType,
            MoodThresholdValue = old.MoodThresholdValue,
            MotiveThresholdType = old.MotiveThresholdType,
            MotiveThresholdValue = old.MotiveThresholdValue,
            RequiredBuffs = new(old.RequiredBuffs),
            RequiredTraits = new(old.RequiredTraits),
            SkillThresholdType = old.SkillThresholdType,
            SkillThresholdValue = old.SkillThresholdValue,
            WorldRestrictionType = old.WorldRestrictionType,
            OccultRestrictions = old.OccultRestrictions,
            OccultRestrictionType = old.OccultRestrictionType,
            SnowLevelValue = old.SnowLevelValue,
            WorldRestrictionWorldNames = new(old.WorldRestrictionWorldNames),
            WorldRestrictionWorldTypes = new(old.WorldRestrictionWorldTypes)
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

    public interface IGraph<T>
    {
        interface IEdge<TItem>
        {
        }

        /// <summary>
        /// Gets a sequence of all items contained within the graph
        /// </summary>
        public IEnumerable<T> Nodes { get; }

        /// <summary>
        /// Gets a sequence of all edges in the graph, represented as an <see cref="IEdge{T}"/>
        /// </summary>
        public IEnumerable<IEdge<T>> Edges { get; }

        /// <summary>
        /// Gets the number of nodes in the graph
        /// </summary>
        public int NodeCount { get; }

        /// <summary>
        /// Gets the number of edges in the graph
        /// </summary>
        public int EdgeCount { get; }

        /// <summary>
        /// Adds a new node to the graph representing an item <typeparamref name="T"/>
        /// </summary>
        /// <param name="item">The item the new node will represent</param>
        public void AddNode(T item);

        /// <summary>
        /// Determines whether the graph contains a given item
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns><c>true</c> if <paramref name="item"/> is contained in the graph; otherwise, <c>false</c></returns>
        public bool ContainsNode(T item);

        /// <summary>
        /// Removes an item from the graph, if it exists
        /// </summary>
        /// <param name="item">The item to remove</param>
        public void RemoveNode(T item);

        /// <summary>
        /// Determines whether the graph contains an edge between two nodes within it
        /// </summary>
        /// <param name="u">The first vertex of the edge</param>
        /// <param name="v">The second vertex of the edge</param>
        /// <returns></returns>
        public bool ContainsEdge(T u, T v);

        /// <summary>
        /// Removes an edge between two nodes in the graph, if such an edge exists
        /// </summary>
        /// <param name="u">The first vertex of the edge</param>
        /// <param name="v">The second vertex of the edge</param>
        public void RemoveEdge(T u, T v);

        /// <summary>
        /// Returns a sequence of the items connected to a given item in the graph
        /// </summary>
        /// <param name="item">The starting item</param>
        public IEnumerable<T> GetNeighbors(T item);
    }

    public interface IWeightedGraph<T> : IGraph<T>
    {
        /// <summary>
        /// Sets the weight of the edge between two nodes in the graph, adding it if it does not already exist
        /// </summary>
        /// <param name="u">The first vertex of the edge</param>
        /// <param name="v">The second vertex of the edge</param>
        /// <param name="weight">The weight value the edge will have</param>
        public void SetEdge(T u, T v, int weight = 1);

        /// <summary>
        /// Gets the weight of the edge between two nodes in the graph, if such an edge exists
        /// </summary>
        /// <param name="u">The first vertex of the edge</param>
        /// <param name="v">The second vertex of the edge</param>
        /// <param name="weight">The weight value of the edge, if it exists</param>
        /// <returns><c>true</c> if an edge between <paramref name="u"/> and <paramref name="v"/> exists in the graph; otherwise, <c>false</c></returns>
        public bool TryGetEdge(T u, T v, out int weight);
    }

    public interface IUnweightedGraph<T> : IGraph<T>
    {
        /// <summary>
        /// A simple data structure representing an edge between two vertices of an unweighted graph
        /// </summary>
        /// <typeparam name="TItem">The type of the edge vertices</typeparam>
        public class Edge<TItem> : IGraph<TItem>.IEdge<TItem>
        {
            public TItem Vertex1 { get; private set; }

            public TItem Vertex2 { get; private set; }

            public Edge(TItem item1, TItem item2)
            {
                Vertex1 = item1;
                Vertex2 = item2;
            }
        }

        /// <summary>
        /// Adds an edge between two nodes in the graph if one does not already exist
        /// </summary>
        /// <param name="u">The first vertex of the edge</param>
        /// <param name="v">The second vertex of the edge</param>
        public void AddEdge(T u, T v);
    }

    /// <summary>
    /// A simple unweighted and directed adjacency list graph implementation with unique node values
    /// </summary>
    /// <typeparam name="T">The type of item the graph will contain</typeparam>
    [Persistable]
    public class UDGraph<T> : IUnweightedGraph<T>
    {
        [Persistable]
        private class Node<TItem>
        {
            private readonly List<Node<TItem>> mNeighbors = new();

            public TItem Item { get; private set; }

            public IEnumerable<TItem> Neighbors => mNeighbors.Select(node => node.Item);

            public Node()
            {
            }

            public Node(TItem item) => Item = item;

            public void AddNeighbor(Node<TItem> item) => mNeighbors.Add(item);

            public void RemoveNeighbor(Node<T> item)
            {
                int index = mNeighbors.FindIndex(x => x.Equals(item));
                if (index >= 0)
                {
                    mNeighbors.RemoveAt(index);
                }
            }
        }

        private readonly List<Node<T>> mSpine = new();

        public IEnumerable<T> Nodes => mSpine.Select(node => node.Item);

        IEnumerable<IGraph<T>.IEdge<T>> IGraph<T>.Edges => Edges.Cast<IGraph<T>.IEdge<T>>();

        public IEnumerable<IUnweightedGraph<T>.Edge<T>> Edges => mSpine.SelectMany(node => node.Neighbors.Select(item => new IUnweightedGraph<T>.Edge<T>(node.Item, item)));

        public int NodeCount => mSpine.Count;

        public int EdgeCount => mSpine.Sum(node => node.Neighbors.Count());

        public UDGraph()
        {
        }

        public UDGraph(params T[] items)
        {
            foreach (T item in items)
            {
                AddNode(item);
            }
        }

        public void AddNode(T item)
        {
            if (item is null)
            {
                throw new ArgumentNullException();
            }
            if (ContainsNode(item))
            {
                throw new ArgumentException("Item already exists in graph");
            }
            mSpine.Add(new(item));
        }

        public bool ContainsNode(T item) => mSpine.Exists(node => node.Item.Equals(item));

        private Node<T> GetNode(T item) => mSpine.Find(node => node.Item.Equals(item));

        public void RemoveNode(T item)
        {
            if (ContainsNode(item))
            {
                Node<T> itemNode = GetNode(item);
                mSpine.Remove(itemNode);
                foreach (Node<T> node in mSpine)
                {
                    node.RemoveNeighbor(itemNode);
                }
            }
        }

        public void AddEdge(T from, T to)
        {
            if (!ContainsNode(from))
            {
                throw new ArgumentException("Item does not exist in graph", "u");
            }
            if (!ContainsNode(to))
            {
                throw new ArgumentException("Item does not exist in graph", "v");
            }
            if (ContainsEdge(from, to))
            {
                throw new ArgumentException("Edge already exists in graph");
            }
            GetNode(from).AddNeighbor(GetNode(to));
        }

        public bool ContainsEdge(T from, T to) => !ContainsNode(from)
                ? throw new ArgumentException("Item does not exist in graph", "u")
                : !ContainsNode(to) ? throw new ArgumentException("Item does not exist in graph", "v") : GetNeighbors(from).Contains(to);

        public void RemoveEdge(T from, T to)
        {
            if (!ContainsNode(from))
            {
                throw new ArgumentException("Item does not exist in graph", "u");
            }
            if (!ContainsNode(to))
            {
                throw new ArgumentException("Item does not exist in graph", "v");
            }
            Node<T> fromNode = GetNode(from), toNode = GetNode(to);
            fromNode.RemoveNeighbor(toNode);
        }

        public IEnumerable<T> GetNeighbors(T item) => !ContainsNode(item) ? throw new ArgumentException("Item does not exist in graph", "u") : GetNode(item).Neighbors;
    }

    /// <summary>
    /// A simple unweighted and undirected adjacency list graph implementation with unique node values
    /// </summary>
    /// <typeparam name="T">The type of item the graph will contain</typeparam>
    [Persistable]
    public class UUGraph<T> : IUnweightedGraph<T>
    {
        private readonly UDGraph<T> mGraph;

        public IEnumerable<T> Nodes => mGraph.Nodes;

        IEnumerable<IGraph<T>.IEdge<T>> IGraph<T>.Edges => Edges.Cast<IGraph<T>.IEdge<T>>();

        public IEnumerable<IUnweightedGraph<T>.Edge<T>> Edges
        {
            get
            {
                List<T> visited = new();
                foreach (var edge in mGraph.Edges)
                {
                    if (!visited.Contains(edge.Vertex1))
                    {
                        visited.Add(edge.Vertex1);
                    }
                    if (!visited.Contains(edge.Vertex2) || edge.Vertex1.Equals(edge.Vertex2))
                    {
                        yield return edge;
                    }
                }
            }
        }

        public int NodeCount => mGraph.NodeCount;

        public int EdgeCount => Edges.Count();

        public UUGraph() => mGraph = new();

        public UUGraph(params T[] items) => mGraph = new(items);

        public void AddNode(T item) => mGraph.AddNode(item);

        public bool ContainsNode(T item) => mGraph.ContainsNode(item);

        public void RemoveNode(T item) => mGraph.RemoveNode(item);

        public void AddEdge(T u, T v)
        {
            mGraph.AddEdge(u, v);
            if (!u.Equals(v))
            {
                mGraph.AddEdge(v, u);
            }
        }

        public bool ContainsEdge(T u, T v) => mGraph.ContainsEdge(u, v);

        public void RemoveEdge(T u, T v)
        {
            mGraph.RemoveEdge(u, v);
            if (!u.Equals(v))
            {
                mGraph.RemoveEdge(v, u);
            }
        }

        public IEnumerable<T> GetNeighbors(T item) => mGraph.GetNeighbors(item);
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
            IEnumerable<string> assemblyNames = from assembly in AppDomain.CurrentDomain.GetAssemblies() 
                                                select assembly.GetName().Name 
                                                into name orderby name select name;
            foreach (string name in assemblyNames)
            {
                result.AppendLine(" " + name);
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
    /*public static class Ferry<T>
    {
        private static readonly Dictionary<FieldInfo, object> mCargo;

        static Ferry()
        {
            IEnumerable<FieldInfo> fields = FindPersistableStatics();
            if (fields.Count() == 0)
            {
                throw new NotSupportedException($"There are no PersistableStatic fields declared in {typeof(T)}.");
            }
            mCargo = new(fields.Count());
            foreach (FieldInfo current in fields)
            {
                mCargo[current] = null;
            }
        }

        private static IEnumerable<FieldInfo> FindPersistableStatics()
        {
            MemberInfo[] fieldMembers = typeof(T).FindMembers(MemberTypes.Field,
                BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic,
                (info, criteria) => info.GetCustomAttributes(typeof(PersistableStaticAttribute), false).Length > 0, null);

            return fieldMembers.Cast<FieldInfo>();
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
    }*/

    public static class Helpers
    {
        public static void ForceSocial(Sim actor, Sim target, string socialName, InteractionPriorityLevel priority, bool isCancellable)
        {
            SocialInteractionA.Definition definition = target.Interactions.Find(iop => iop.InteractionDefinition is SocialInteractionA.Definition social && social.ActionKey == socialName)?.InteractionDefinition as SocialInteractionA.Definition 
                ?? new(socialName, new string[0], null, false);
            InteractionInstance instance = definition.CreateInstance(target, actor, new(priority), false, isCancellable);
            actor.InteractionQueue.Add(instance);
        }

        public static T CoinFlipSelect<T>(T obj1, T obj2) => RandomUtil.CoinFlip() ? obj1 : obj2;
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
                if (traitManager.HasElement(trait) || (!hidden && TraitsAdded.Where(guid => TraitManager.GetTraitFromDictionary(guid).IsVisible).Count() == traitManager.CountVisibleTraits()))
                {
                    return;
                }
                IEnumerable<Trait> conflictingTraits = traitManager.GetDictionaryConflictingTraits(trait).Where(x => mTargetSim.HasTrait(x.Guid));
                if (conflictingTraits.Count() > 0)
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