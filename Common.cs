using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
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
using System.Xml;
using static Sims3.UI.ObjectPicker;
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

        private readonly StopWatch.TickStyles mTickStyles;

        private readonly Func<bool> mFunction;

        public RepeatingFunctionTask(Func<bool> function, int delay = 500, StopWatch.TickStyles tickStyles = StopWatch.TickStyles.Milliseconds)
        {
            mFunction = function;
            mDelay = delay;
            mTickStyles = tickStyles;
        }

        public override void Dispose()
        {
            mTimer?.Dispose();
            mTimer = null;
            base.Dispose();
        }

        protected override void Run()
        {
            mTimer = StopWatch.Create(mTickStyles);
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

        private readonly Dictionary<T, List<T>> mSpine = new();

        public IEnumerable<T> Nodes => mSpine.Keys;

        IEnumerable<IGraph<T>.IEdge<T>> IGraph<T>.Edges => Edges.Cast<IGraph<T>.IEdge<T>>();

        public IEnumerable<IUnweightedGraph<T>.Edge<T>> Edges => mSpine.SelectMany(kvp => kvp.Value.Select(item => new IUnweightedGraph<T>.Edge<T>(kvp.Key, item)));

        public int NodeCount => mSpine.Count;

        public int EdgeCount => mSpine.Sum(kvp => kvp.Value.Count());

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
            mSpine[item] = new();
        }

        public bool ContainsNode(T item) => mSpine.ContainsKey(item);

        public void RemoveNode(T item)
        {
            if (ContainsNode(item))
            {
                mSpine.Remove(item);
                foreach (T node in mSpine.Keys)
                {
                    mSpine[node].Remove(item);
                }
            }
        }

        public void AddEdge(T from, T to)
        {
            if (!ContainsNode(from))
            {
                throw new ArgumentException("Item does not exist in graph", "from");
            }
            if (!ContainsNode(to))
            {
                throw new ArgumentException("Item does not exist in graph", "to");
            }
            if (mSpine[from].Contains(to))
            {
                throw new ArgumentException("Edge already exists in graph");
            }
            mSpine[from].Add(to);
        }

        public bool ContainsEdge(T from, T to) => !ContainsNode(from)
                ? throw new ArgumentException("Item does not exist in graph", "from")
                : !ContainsNode(to) ? throw new ArgumentException("Item does not exist in graph", "to") : mSpine[from].Contains(to);

        public void RemoveEdge(T from, T to)
        {
            if (!ContainsNode(from))
            {
                throw new ArgumentException("Item does not exist in graph", "from");
            }
            if (!ContainsNode(to))
            {
                throw new ArgumentException("Item does not exist in graph", "to");
            }
            mSpine[from].Remove(to);
        }

        public IEnumerable<T> GetNeighbors(T item) => !ContainsNode(item) ? throw new ArgumentException("Item does not exist in graph", "item") : mSpine[item];
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

    public static class Helpers
    {
        public static void InjectInteraction<TTarget>(ref InteractionDefinition singleton, InteractionDefinition newSingleton, bool requiresTuning) where TTarget : IGameObject
            => InjectInteraction<TTarget, InteractionDefinition>(ref singleton, newSingleton, requiresTuning);

        public static void InjectInteraction<TTarget>(ref ISoloInteractionDefinition singleton, ISoloInteractionDefinition newSingleton, bool requiresTuning) where TTarget : IGameObject
        {
            if (requiresTuning)
            {
                Tunings.Inject(singleton.GetType(), typeof(TTarget), newSingleton.GetType(), typeof(TTarget), true);
            }
            singleton = newSingleton;
        }

        public static void InjectInteraction<TTarget, TDefinition>(ref TDefinition singleton, TDefinition newSingleton, bool requiresTuning) where TTarget : IGameObject where TDefinition : InteractionDefinition
        {
            if (requiresTuning)
            {
                Tunings.Inject(singleton.GetType(), typeof(TTarget), newSingleton.GetType(), typeof(TTarget), true);
            }
            singleton = newSingleton;
        }

        public static void AddInteraction(GameObject gameObject, InteractionDefinition singleton)
        {
            IEnumerable<InteractionObjectPair> iops = gameObject.Interactions;
            if (gameObject.ItemComp?.InteractionsInventory is IEnumerable<InteractionObjectPair> inventoryIops)
            {
                iops = iops.Concat(inventoryIops);
            }
            if (!iops.Any(iop => iop.GetType() == singleton.GetType()))
            {
                gameObject.AddInteraction(singleton);
                gameObject.AddInventoryInteraction(singleton);
            }
        }

        public static void ForceSocial(Sim actor, Sim target, string socialName, InteractionPriorityLevel priority, bool isCancellable)
        {
            SocialInteractionA.Definition definition = target.Interactions.Find(iop => iop.InteractionDefinition is SocialInteractionA.Definition social && social.ActionKey == socialName)?.InteractionDefinition as SocialInteractionA.Definition
                ?? new(socialName, new string[0], null, false);
            InteractionInstance instance = definition.CreateInstance(target, actor, new(priority), false, isCancellable);
            actor.InteractionQueue.Add(instance);
        }

        public static void LoadSocialData(string resourceName)
        {
            XmlDocument xmlDocument = Simulator.LoadXML(resourceName);
            bool isEp5Installed = GameUtils.IsInstalled(ProductVersion.EP5);
            if (xmlDocument is not null)
            {
                foreach (XmlElement current in new XmlElementLookup(xmlDocument)["Action"])
                {
                    XmlElementLookup table = new(current);
                    ParserFunctions.TryParseEnum(current.GetAttribute("com"), out CommodityTypes intendedCom, CommodityTypes.Undefined);
                    ActionData data = new(current.GetAttribute("key"), intendedCom, ProductVersion.BaseGame, table, isEp5Installed);
                    ActionData.Add(data);
                }
            }
        }

        public static T CoinFlipSelect<T>(T obj1, T obj2) => RandomUtil.CoinFlip() ? obj1 : obj2;
    }

    public static class Reflection
    {
        public static bool IsAssemblyLoaded(string str, bool matchExact = true)
            => AppDomain.CurrentDomain.GetAssemblies()
                                      .Any(assembly => matchExact
                                                    ? assembly.GetName().Name == str
                                                    : assembly.GetName().Name.Contains(str));

        public static void StaticInvoke(string assemblyQualifiedTypeName, string methodName, object[] args, Type[] argTypes) => StaticInvoke(Type.GetType(assemblyQualifiedTypeName), methodName, args, argTypes);

        public static void StaticInvoke(Type type, string methodName, object[] args, Type[] argTypes)
        {
            if (type is null)
            {
                throw new ArgumentNullException("type");
            }
            if (type.GetMethod(methodName, argTypes) is not MethodInfo method)
            {
                throw new MissingMethodException("No method found in type with specified name and args");
            }
            method.Invoke(null, args);
        }

        public static T StaticInvoke<T>(string assemblyQualifiedTypeName, string methodName, object[] args, Type[] argTypes) => StaticInvoke<T>(Type.GetType(assemblyQualifiedTypeName), methodName, args, argTypes);

        public static T StaticInvoke<T>(Type type, string methodName, object[] args, Type[] argTypes)
            => type is null
            ? throw new ArgumentNullException("type")
            : type.GetMethod(methodName, argTypes) is not MethodInfo method
            ? throw new MissingMethodException("No method found in type with specified name and args")
            : (T)method.Invoke(null, args);

        public static void InstanceInvoke(string assemblyQualifiedTypeName, object[] ctorArgs, Type[] ctorArgTypes, string methodName, object[] methodArgs, Type[] methodArgTypes)
            => InstanceInvoke(Type.GetType(assemblyQualifiedTypeName), ctorArgs, ctorArgTypes, methodName, methodArgs, methodArgTypes);

        public static void InstanceInvoke(Type type, object[] ctorArgs, Type[] ctorArgTypes, string methodName, object[] methodArgs, Type[] methodArgTypes)
        {
            if (type is null)
            {
                throw new ArgumentNullException("type");
            }
            if (type.GetConstructor(ctorArgTypes) is not ConstructorInfo ctor)
            {
                throw new MissingMethodException(type.FullName, ".ctor()");
            }
            InstanceInvoke(ctor.Invoke(ctorArgs), methodName, methodArgs, methodArgTypes);
        }

        public static void InstanceInvoke(object obj, string methodName, object[] args, Type[] argTypes)
        {
            if (obj is null)
            {
                throw new ArgumentNullException("Instance object");
            }
            if (obj.GetType().GetMethod(methodName, argTypes) is not MethodInfo method)
            {
                throw new MissingMethodException("No method found in instance with specified name and args");
            }
            method.Invoke(obj, args);
        }

        public static T InstanceInvoke<T>(string assemblyQualifiedTypeName, object[] ctorArgs, Type[] ctorArgTypes, string methodName, object[] methodArgs, Type[] methodArgTypes)
            => InstanceInvoke<T>(Type.GetType(assemblyQualifiedTypeName), ctorArgs, ctorArgTypes, methodName, methodArgs, methodArgTypes);

        public static T InstanceInvoke<T>(Type type, object[] ctorArgs, Type[] ctorArgTypes, string methodName, object[] methodArgs, Type[] methodArgTypes)
            => type is null
            ? throw new ArgumentNullException("type")
            : type.GetConstructor(ctorArgTypes) is not ConstructorInfo ctor
            ? throw new MissingMethodException(type.FullName, ".ctor()")
            : InstanceInvoke<T>(ctor.Invoke(ctorArgs), methodName, methodArgs, methodArgTypes);

        public static T InstanceInvoke<T>(object obj, string methodName, object[] args, Type[] argTypes)
            => obj is null
            ? throw new ArgumentNullException("Instance object")
            : obj.GetType().GetMethod(methodName, argTypes) is not MethodInfo method
            ? throw new MissingMethodException("No method found in instance with specified name and args")
            : (T)method.Invoke(obj, args);
    }
}

namespace Gamefreak130.Common.Buffs
{
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

namespace Gamefreak130.Common.UI
{
    public static class UIHelpers
    {
        public static void ShowElementById(WindowBase containingWindow, uint id, bool recursive = true)
        {
            if (containingWindow.GetChildByID(id, recursive) is WindowBase window)
            {
                window.Visible = true;
            }
        }

        public static void EnableElementById(WindowBase containingWindow, uint id, bool recursive = true)
        {
            if (containingWindow.GetChildByID(id, recursive) is WindowBase window)
            {
                window.Enabled = true;
            }
        }

        public static void HideElementById(WindowBase containingWindow, uint id, bool recursive = true)
        {
            if (containingWindow.GetChildByID(id, recursive) is WindowBase window)
            {
                window.Visible = false;
            }
        }

        public static void DisableElementById(WindowBase containingWindow, uint id, bool recursive = true)
        {
            if (containingWindow.GetChildByID(id, recursive) is WindowBase window)
            {
                window.Enabled = false;
            }
        }

        public static void ShowElementByIndex(WindowBase containingWindow, uint index)
        {
            if (containingWindow.GetChildByIndex(index) is WindowBase window)
            {
                window.Visible = true;
            }
        }

        public static void EnableElementByIndex(WindowBase containingWindow, uint index)
        {
            if (containingWindow.GetChildByIndex(index) is WindowBase window)
            {
                window.Enabled = true;
            }
        }

        public static void HideElementByIndex(WindowBase containingWindow, uint index)
        {
            if (containingWindow.GetChildByIndex(index) is WindowBase window)
            {
                window.Visible = false;
            }
        }

        public static void DisableElementByIndex(WindowBase containingWindow, uint index)
        {
            if (containingWindow.GetChildByIndex(index) is WindowBase window)
            {
                window.Enabled = false;
            }
        }
    }

    public struct ColumnDelegateStruct
    {
        public ColumnType mColumnType;

        public Func<ColumnInfo> mInfo;

        public ColumnDelegateStruct(ColumnType colType, Func<ColumnInfo> infoDelegate)
        {
            mColumnType = colType;
            mInfo = infoDelegate;
        }
    }

    public struct RowTextFormat
    {
        public Color mTextColor;

        public bool mBoldTextStyle;

        public string mTooltip;

        public RowTextFormat(Color textColor, bool boldText, string tooltipText)
        {
            mTextColor = textColor;
            mBoldTextStyle = boldText;
            mTooltip = tooltipText;
        }
    }

    /// <summary>
    /// An <see cref="ObjectPickerDialog"/> which allows for the okay button to be clicked with no items selected, in which case an empty RowInfo list is returned
    /// </summary>
    /*public class ObjectPickerDialogEx : ObjectPickerDialog
    {
        public ObjectPickerDialogEx(bool modal, PauseMode pauseMode, string title, string buttonTrue, string buttonFalse, List<TabInfo> listObjs, List<HeaderInfo> headers, int numSelectableRows, Vector2 position, bool viewTypeToggle, List<RowInfo> preSelectedRows, bool showHeadersAndToggle, bool disableCloseButton)
            : base(modal, pauseMode, title, buttonTrue, buttonFalse, listObjs, headers, numSelectableRows, position, viewTypeToggle, preSelectedRows, showHeadersAndToggle, disableCloseButton)
        {
            mOkayButton.Enabled = true;
            mTable.ObjectTable.TableChanged -= OnTableChanged;
            mTable.SelectionChanged -= OnSelectionChanged;
            mTable.SelectionChanged += OnSelectionChangedEx;
            mTable.RowSelected -= OnSelectionChanged;
            mTable.RowSelected += OnSelectionChangedEx;
            mTable.Selected = preSelectedRows;
        }

        new public static List<RowInfo> Show(bool modal, PauseMode pauseType, string title, string buttonTrue, string buttonFalse, List<TabInfo> listObjs, List<HeaderInfo> headers, int numSelectableRows, Vector2 position, bool viewTypeToggle, List<RowInfo> preSelectedRows, bool showHeadersAndToggle, bool disableCloseButton)
        {
            using (ObjectPickerDialogEx objectPickerDialog = new(modal, pauseType, title, buttonTrue, buttonFalse, listObjs, headers, numSelectableRows, position, viewTypeToggle, preSelectedRows, showHeadersAndToggle, disableCloseButton))
            {
                objectPickerDialog.StartModal();
                return objectPickerDialog.Result;
            }
        }

        public override bool OnEnd(uint endID)
        {
            if (endID == OkayID)
            {
                if (!mOkayButton.Enabled)
                {
                    return false;
                }
                mResult = mTable.Selected ?? new();
            }
            else
            {
                mResult = null;
            }
            mTable.Populate(null, null, 0);
            return true;
        }

        private void OnSelectionChangedEx(List<RowInfo> _) => Audio.StartSound("ui_tertiary_button");
    }*/

    /// <summary>
    /// Used by <see cref="MenuController"/> to construct menus using <see cref="MenuObject"/>s with arbitrary functionality
    /// </summary>
    /// <seealso cref="MenuController"/>
    public class MenuContainer
    {
        private List<RowInfo> mRowInformation;

        private readonly string[] mTabImage;

        private readonly string[] mTabText;

        private readonly Func<List<RowInfo>> mRowPopulationDelegate;

        private readonly List<RowInfo> mHiddenRows;

        public string MenuDisplayName { get; }

        public List<HeaderInfo> Headers { get; private set; }

        public List<TabInfo> TabInformation { get; private set; }

        public Action<List<RowInfo>> OnEnd { get; }

        public MenuContainer() : this("")
        {
        }

        public MenuContainer(string title) : this(title, "")
        {
        }

        public MenuContainer(string title, string subtitle) : this(title, new[] { "" }, new[] { subtitle }, null)
        {
        }

        public MenuContainer(string title, string[] tabImage, string[] tabName, Action<List<RowInfo>> onEndDelegate) : this(title, tabImage, tabName, onEndDelegate, null)
        {
        }

        public MenuContainer(string title, string[] tabImage, string[] tabName, Action<List<RowInfo>> onEndDelegate, Func<List<RowInfo>> rowPopulationDelegate)
        {
            mHiddenRows = new();
            MenuDisplayName = title;
            mTabImage = tabImage;
            mTabText = tabName;
            OnEnd = onEndDelegate;
            Headers = new();
            mRowInformation = new();
            TabInformation = new();
            mRowPopulationDelegate = rowPopulationDelegate;
            if (mRowPopulationDelegate is not null)
            {
                RefreshMenuObjects(0);
                if (mRowInformation.Count > 0)
                {
                    for (int i = 0; i < mRowInformation[0].ColumnInfo.Count; i++)
                    {
                        Headers.Add(new("Ui/Caption/ObjectPicker:Sim", "", 200));
                    }
                }
            }
        }

        public void RefreshMenuObjects(int tabnumber)
        {
            mRowInformation = mRowPopulationDelegate();
            TabInformation = new()
            {
                new("", mTabText[tabnumber], mRowInformation)
            };
        }

        public void SetHeaders(List<HeaderInfo> headers) => Headers = headers;

        public void SetHeader(int headerNumber, HeaderInfo headerInfos) => Headers[headerNumber] = headerInfos;

        public void ClearMenuObjects() => TabInformation.Clear();

        public void AddMenuObject(MenuObject menuItem)
        {
            if (TabInformation.Count < 1)
            {
                mRowInformation = new()
                {
                    menuItem.RowInformation
                };
                TabInformation.Add(new(mTabImage[0], mTabText[0], mRowInformation));
                Headers.Add(new("Ui/Caption/ObjectPicker:Name", "", 300));
                Headers.Add(new("Ui/Caption/ObjectPicker:Value", "", 100));
                return;
            }
            TabInformation[0].RowInfo.Add(menuItem.RowInformation);
        }

        public void AddMenuObject(List<HeaderInfo> headers, MenuObject menuItem)
        {

            if (TabInformation.Count < 1)
            {
                mRowInformation = new()
                {
                    menuItem.RowInformation
                };
                TabInformation.Add(new(mTabImage[0], mTabText[0], mRowInformation));
                Headers = headers;
                return;
            }
            TabInformation[0].RowInfo.Add(menuItem.RowInformation);
            Headers = headers;
        }

        public void AddMenuObject(List<HeaderInfo> headers, RowInfo item)
        {
            if (TabInformation.Count < 1)
            {
                mRowInformation = new()
                {
                    item
                };
                TabInformation.Add(new(mTabImage[0], mTabText[0], mRowInformation));
                Headers = headers;
                return;
            }
            TabInformation[0].RowInfo.Add(item);
            Headers = headers;
        }

        public void UpdateRows()
        {
            for (int i = mHiddenRows.Count - 1; i >= 0; i--)
            {
                MenuObject item = mHiddenRows[i].Item as MenuObject;
                if (item.Test())
                {
                    mHiddenRows.RemoveAt(i);
                    AddMenuObject(item);
                }
            }
            for (int i = TabInformation[0].RowInfo.Count - 1; i >= 0; i--)
            {
                MenuObject item = TabInformation[0].RowInfo[i].Item as MenuObject;
                if (item.Test is not null && !item.Test())
                {
                    mHiddenRows.Add(TabInformation[0].RowInfo[i]);
                    TabInformation[0].RowInfo.RemoveAt(i);
                }
            }
        }

        public void UpdateItems()
        {
            UpdateRows();
            foreach (TabInfo current in TabInformation)
            {
                foreach (RowInfo current2 in current.RowInfo)
                {
                    (current2.Item as MenuObject)?.UpdateMenuObject();
                }
            }
        }
    }

    /// <summary>
    /// Modal dialog utilizing <see cref="MenuContainer"/> to construct NRaas-like settings menus
    /// </summary>
    /// <seealso cref="MenuContainer"/>
    /*public class MenuController : ModalDialog
    {
        private enum ControlIds : uint
        {
            ItemTable = 99576784u,
            OkayButton,
            CancelButton,
            TitleText,
            TableBackground,
            TableBezel
        }

        private const int kWinExportID = 1;

        private Vector2 mTableOffset;

        private ObjectPicker mTable;

        private readonly Button mOkayButton;

        private readonly Button mCloseButton;

        private readonly TabContainer mTabsContainer;

        public bool Okay { get; private set; }

        public List<RowInfo> Result { get; private set; }

        public Action<List<RowInfo>> EndDelegates { get; private set; }

        public void ShowModal()
        {
            mModalDialogWindow.Moveable = true;
            StartModal();
        }

        public void Stop() => StopModal();

        public MenuController(string title, string buttonTrue, string buttonFalse, List<TabInfo> listObjs, List<HeaderInfo> headers, bool showHeadersAndToggle, Action<List<RowInfo>> endResultDelegates)
            : this(true, PauseMode.PauseSimulator, title, buttonTrue, buttonFalse, listObjs, headers, showHeadersAndToggle, endResultDelegates)
        {
        }

        public MenuController(bool isModal, PauseMode pauseMode, string title, string buttonTrue, string buttonFalse, List<TabInfo> listObjs, List<HeaderInfo> headers, bool showHeadersAndToggle, Action<List<RowInfo>> endResultDelegates)
            : base("UiObjectPicker", kWinExportID, isModal, pauseMode, null)
        {
            if (mModalDialogWindow is not null)
            {
                Text text = mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text;
                text.Caption = title;
                mTable = mModalDialogWindow.GetChildByID((uint)ControlIds.ItemTable, false) as ObjectPicker;
                mTable.SelectionChanged += OnRowClicked;
                mTabsContainer = mTable.mTabs;
                mTable.mTable.mPopulationCompletedCallback += ResizeWindow;
                mOkayButton = mModalDialogWindow.GetChildByID((uint)ControlIds.OkayButton, false) as Button;
                mOkayButton.TooltipText = buttonTrue;
                mOkayButton.Enabled = true;
                mOkayButton.Click += OnOkayButtonClick;
                OkayID = mOkayButton.ID;
                SelectedID = mOkayButton.ID;
                mCloseButton = mModalDialogWindow.GetChildByID((uint)ControlIds.CancelButton, false) as Button;
                mCloseButton.TooltipText = buttonFalse;
                mCloseButton.Click += OnCloseButtonClick;
                CancelID = mCloseButton.ID;
                mTableOffset = mModalDialogWindow.Area.BottomRight - mModalDialogWindow.Area.TopLeft - (mTable.Area.BottomRight - mTable.Area.TopLeft);
                mTable.ShowHeaders = showHeadersAndToggle;
                mTable.ViewTypeToggle = false;
                mTable.ShowToggle = false;
                mTable.Populate(listObjs, headers, 1);
                ResizeWindow();
            }
            EndDelegates = endResultDelegates;
        }

        public void PopulateMenu(List<TabInfo> tabinfo, List<HeaderInfo> headers, int numSelectableRows) => mTable.Populate(tabinfo, headers, numSelectableRows);

        public override void Dispose() => Dispose(true);

        public void AddRow(int Tabnumber, RowInfo info)
        {
            mTable.mItems[Tabnumber].RowInfo.Clear();
            mTable.mItems[Tabnumber].RowInfo.Add(info);
            Repopulate();
        }

        public void SetTableColor(Color color) => mModalDialogWindow.GetChildByID((uint)ControlIds.TableBezel, false).ShadeColor = color;

        public void SetTitleText(string text) => (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).Caption = text;

        public void SetTitleText(string text, Color textColor)
        {
            (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).Caption = text;
            (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).TextColor = textColor;
        }

        public void SetTitleText(string text, Color textColor, uint textStyle)
        {
            (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).Caption = text;
            (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).TextColor = textColor;
            (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).TextStyle = textStyle;
        }

        public void SetTitleText(string text, Color textColor, bool textStyleBold)
        {
            (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).Caption = text;
            (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).TextColor = textColor;
            if (textStyleBold)
            {
                (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).TextStyle = 2u;
            }
        }

        public void SetTitleTextColor(Color textColor) => (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).TextColor = textColor;

        public void SetTitleTextStyle(uint textStyle) => (mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text).TextStyle = textStyle;

        private void Repopulate()
        {
            if (mTable.RepopulateTable())
            {
                ResizeWindow();
            }
        }

        private void ResizeWindow()
        {
            Rect area = mModalDialogWindow.Parent.Area;
            float width = area.Width;
            float height = area.Height;
            int num = (int)height - (int)(mTableOffset.y * 2f);
            num /= (int)mTable.mTable.RowHeight;
            if (num > mTable.mTable.NumberRows)
            {
                num = mTable.mTable.NumberRows;
            }
            mTable.mTable.VisibleRows = (uint)num;
            mTable.mTable.GridSizeDirty = true;
            mTable.OnPopulationComplete();
            mModalDialogWindow.Area = new(mModalDialogWindow.Area.TopLeft, mModalDialogWindow.Area.TopLeft + mTable.TableArea.BottomRight + mTableOffset);
            Rect area2 = mModalDialogWindow.Area;
            float width2 = area2.Width;
            float height2 = area2.Height;
            float num2 = (float)Math.Round((width - width2) / 2f);
            float num3 = (float)Math.Round((height - height2) / 2f);
            area2.Set(num2, num3, num2 + width2, num3 + height2);
            mModalDialogWindow.Area = area2;
            Text text = mModalDialogWindow.GetChildByID((uint)ControlIds.TitleText, false) as Text;
            Rect area3 = text.Area;
            area3.Set(area3.TopLeft.x, 20f, area3.BottomRight.x, 50f - area2.Height);
            text.Area = area3;
            mModalDialogWindow.Visible = true;
        }

        private void OnRowClicked(List<RowInfo> _)
        {
            Audio.StartSound("ui_tertiary_button");
            EndDialog(OkayID);
        }

        private void OnCloseButtonClick(WindowBase sender, UIButtonClickEventArgs eventArgs)
        {
            eventArgs.Handled = true;
            EndDialog(CancelID);
        }

        private void OnOkayButtonClick(WindowBase sender, UIButtonClickEventArgs eventArgs)
        {
            eventArgs.Handled = true;
            EndDialog(OkayID);
        }

        public override void EndDialog(uint endID)
        {
            if (OnEnd(endID))
            {
                StopModal();
                Dispose();
            }
            mTable = null;
            mModalDialogWindow = null;
        }

        public override bool OnEnd(uint endID)
        {
            if (endID == OkayID)
            {
                EndDelegates?.Invoke(mTable.Selected);
                Result = mTable.Selected;
                Okay = true;
            }
            else
            {
                Result = null;
                Okay = false;
            }
            mTable.Populate(null, null, 0);
            EndDelegates = null;
            return true;
        }

        /// <summary>Creates and shows a new submenu from the given <see cref="MenuContainer"/>, invoking <see cref="MenuObject.OnActivation()"/> when a <see cref="MenuObject"/> is selected</summary>
        /// <param name="container">The <see cref="MenuContainer"/> used to generate the menu</param>
        /// <param name="showHeaders">Whether or not to show headers at the top of the menu table. Defaults to <see langword="true"/>.</param>
        /// <returns>
        ///     <para><see langword="true"/> to terminate the entire menu tree.</para>
        ///     <para><see langword="false"/> to return control to the invoker of the function.</para>
        /// </returns>
        /// <seealso cref="MenuObject.OnActivation()"/>
        public static bool ShowMenu(MenuContainer container, bool showHeaders = true) => ShowMenu(container, 0, showHeaders);

        /// <summary>Creates and shows a new submenu from the given <see cref="MenuContainer"/>, invoking <see cref="MenuObject.OnActivation()"/> when a <see cref="MenuObject"/> is selected</summary>
        /// <param name="container">The <see cref="MenuContainer"/> used to generate the menu</param>
        /// <param name="tab">The index of the tab that the submenu will open in</param>
        /// <param name="showHeaders">Whether or not to show headers at the top of the menu table. Defaults to <see langword="true"/>.</param>
        /// <returns>
        ///     <para><see langword="true"/> to terminate the entire menu tree.</para>
        ///     <para><see langword="false"/> to return control to the invoker of the function.</para>
        /// </returns>
        /// <seealso cref="MenuObject.OnActivation()"/>
        public static bool ShowMenu(MenuContainer container, int tab, bool showHeaders = true)
        {
            try
            {
                while (true)
                {
                    container.UpdateItems();
                    MenuController controller = Show(container, tab, showHeaders);
                    if (controller.Okay)
                    {
                        if (controller.Result?[0]?.Item is MenuObject menuObject)
                        {
                            if (menuObject.OnActivation())
                            {
                                return true;
                            }
                            continue;
                        }
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.sInstance.Log(ex);
                return true;
            }
        }

        private static MenuController Show(MenuContainer container, int tab, bool showHeaders)
        {
            Sims3.Gameplay.Gameflow.SetGameSpeed(Gameflow.GameSpeed.Pause, Sims3.Gameplay.Gameflow.SetGameSpeedContext.GameStates);
            MenuController menuController = new(container.MenuDisplayName, Localization.LocalizeString("Ui/Caption/Global:Ok"), Localization.LocalizeString("Ui/Caption/Global:Cancel"), container.TabInformation, container.Headers, showHeaders, container.OnEnd);
            menuController.SetTitleTextStyle(2u);
            if (tab >= 0)
            {
                if (tab < menuController.mTabsContainer.mTabs.Count)
                {
                    menuController.mTabsContainer.SelectTab(menuController.mTabsContainer.mTabs[tab]);
                }
                else
                {
                    menuController.mTabsContainer.SelectTab(menuController.mTabsContainer.mTabs[menuController.mTabsContainer.mTabs.Count - 1]);
                }
            }
            menuController.ShowModal();
            return menuController;
        }
    }*/

    /// <summary>
    /// Represents a single item within a <see cref="MenuController"/> dialog with arbitrary behavior upon selection
    /// </summary>
    public abstract class MenuObject : IDisposable
    {
        private List<ColumnInfo> mColumnInfoList;

        protected List<ColumnDelegateStruct> mColumnActions;

        private RowTextFormat mTextFormat;

        public Func<bool> Test { get; protected set; }

        public RowInfo RowInformation { get; private set; }

        public MenuObject() : this(new List<ColumnDelegateStruct>(), null)
        {
        }

        public MenuObject(List<ColumnDelegateStruct> columns, Func<bool> test)
        {
            mColumnInfoList = new();
            mColumnActions = columns;
            Test = test;
            PopulateColumnInfo();
            Fillin();
        }

        public MenuObject(string name, Func<bool> test) : this(name, () => "", test)
        {
        }

        public MenuObject(string name, Func<string> getValue, Func<bool> test)
        {
            mColumnInfoList = new();
            Test = test;
            mColumnActions = new()
            {
                new(ColumnType.kText, () => new TextColumn(name)),
                new(ColumnType.kText, () => new TextColumn(getValue()))
            };
            PopulateColumnInfo();
            Fillin();
        }

        public void Fillin() => RowInformation = new(this, mColumnInfoList);

        public void Fillin(Color textColor)
        {
            mTextFormat.mTextColor = textColor;
            Fillin();
        }

        public void Fillin(Color textColor, bool boldTextStyle)
        {
            mTextFormat.mTextColor = textColor;
            Fillin(boldTextStyle);
        }

        public void Fillin(bool boldTextStyle)
        {
            mTextFormat.mBoldTextStyle = boldTextStyle;
            Fillin();
        }

        public void Fillin(string tooltipText)
        {
            mTextFormat.mTooltip = tooltipText;
            Fillin();
        }

        public void Dispose()
        {
            RowInformation = null;
            mColumnInfoList.Clear();
            mColumnInfoList = null;
        }

        public virtual void PopulateColumnInfo()
        {
            foreach (ColumnDelegateStruct column in mColumnActions)
            {
                mColumnInfoList.Add(column.mInfo());
            }
        }

        public virtual void AdaptToMenu(TabInfo tabInfo)
        {
        }

        /// <summary>Callback method raised by <see cref="MenuController"/> when a <see cref="MenuObject"/> is selected</summary>
        /// <returns><see langword="true"/> if entire menu tree should be termined; otherwise, <see langword="false"/> to return control to the containing <see cref="MenuController"/></returns>
        /// <seealso cref="MenuController.ShowMenu(MenuContainer)"/>
        public virtual bool OnActivation() => true;

        public void UpdateMenuObject()
        {
            for (int i = 0; i < mColumnInfoList.Count; i++)
            {
                mColumnInfoList[i] = mColumnActions[i].mInfo();
            }
            Fillin();
        }
    }

    /// <summary>
    /// A <see cref="MenuObject"/> that performs a one-shot function before returning control to the containing <see cref="MenuController"/>
    /// </summary>
    public class GenericActionObject : MenuObject
    {
        protected readonly Function mCallback;

        public GenericActionObject(string name, Func<bool> test, Function action) : base(name, test)
            => mCallback = action;

        public GenericActionObject(string name, Func<string> getValue, Func<bool> test, Function action) : base(name, getValue, test)
            => mCallback = action;

        public GenericActionObject(List<ColumnDelegateStruct> columns, Func<bool> test, Function action) : base(columns, test)
            => mCallback = action;

        public override bool OnActivation()
        {
            mCallback();
            return false;
        }
    }

    /// <summary>
    /// A <see cref="MenuObject"/> that creates and shows a new submenu from a given <see cref="MenuContainer"/> on activation
    /// </summary>
    /*public sealed class GenerateMenuObject : MenuObject
    {
        private readonly MenuContainer mToOpen;

        public GenerateMenuObject(string name, Func<bool> test, MenuContainer toOpen) : base(name, test)
            => mToOpen = toOpen;

        public GenerateMenuObject(List<ColumnDelegateStruct> columns, Func<bool> test, MenuContainer toOpen) : base(columns, test)
            => mToOpen = toOpen;

        public override bool OnActivation() => MenuController.ShowMenu(mToOpen);
    }*/

    /// <summary>
    /// <para>A <see cref="MenuObject"/> that performs a predicate on activation.</para> 
    /// <para>If the predicate returns <see langword="true"/>, then the entire menu tree terminates; if it returns <see langword="false"/>, then control returns to the containing <see cref="MenuController"/></para>
    /// </summary>
    public class ConditionalActionObject : MenuObject
    {
        private readonly Func<bool> mPredicate;

        public ConditionalActionObject(string name, Func<bool> test, Func<bool> action) : base(name, test)
            => mPredicate = action;

        public ConditionalActionObject(string name, Func<string> getValue, Func<bool> test, Func<bool> action) : base(name, getValue, test)
            => mPredicate = action;

        public ConditionalActionObject(List<ColumnDelegateStruct> columns, Func<bool> test, Func<bool> action) : base(columns, test)
            => mPredicate = action;

        public override bool OnActivation() => mPredicate();
    }

    /// <summary>
    /// <para>A <see cref="MenuObject"/> that prompts the user to enter a new string value for a given <typeparamref name="T"/> (or toggles a boolean value).</para> 
    /// <para>Control is returned to the containing <see cref="MenuController"/>, regardless of the result of toggling or converting to <typeparamref name="T"/></para>
    /// </summary>
    /// <typeparam name="T">The type of the value to set</typeparam>
    public abstract class SetSimpleValueObject<T> : MenuObject where T : IConvertible
    {
        protected delegate void SetValueDelegate(T val);

        protected readonly string mMenuTitle;

        protected readonly string mDialogPrompt;

        protected Func<T> mGetValue;

        protected SetValueDelegate mSetValue;

        public SetSimpleValueObject(string menuTitle, string dialogPrompt, Func<bool> test) : this(menuTitle, dialogPrompt, new(), test)
        {
        }

        public SetSimpleValueObject(string menuTitle, string dialogPrompt, List<ColumnDelegateStruct> columns, Func<bool> test) : base(columns, test)
        {
            mMenuTitle = menuTitle;
            mDialogPrompt = dialogPrompt;
        }

        protected void ConstructDefaultColumnInfo()
        {
            mColumnActions = new()
            {
                new(ColumnType.kText, () => new TextColumn(mMenuTitle)),
                new(ColumnType.kText, () => new TextColumn(mGetValue().ToString()))
            };
            PopulateColumnInfo();
            Fillin();
        }

        public override bool OnActivation()
        {
            try
            {
                Type t = typeof(T);
                T val = default;
                if (t == typeof(bool))
                {
                    // Holy boxing Batman
                    val = (T)(object)!(bool)(object)mGetValue();
                }
                else
                {
                    string str = StringInputDialog.Show(mMenuTitle, mDialogPrompt, mGetValue().ToString());
                    if (str is not null)
                    {
                        val = t.IsEnum ? (T)Enum.Parse(t, str) : (T)Convert.ChangeType(str, t);
                    }
                }

                if (val is not null)
                {
                    mSetValue(val);
                }
            }
            catch (FormatException)
            {
            }
            catch (OverflowException)
            {
            }
            catch (ArgumentException)
            {
            }
            return false;
        }
    }

    /// <summary>
    /// A <see cref="SetSimpleValueObject{T}"/> that sets the value of a readable and writable property in a given <see cref="object"/>
    /// </summary>
    /// <typeparam name="T">The type of the given property</typeparam>
    public sealed class SetSimplePropertyObject<T> : SetSimpleValueObject<T> where T : IConvertible
    {
        public SetSimplePropertyObject(string menuTitle, string propertyName, Func<bool> test, object obj) : this(menuTitle, "", propertyName, test, obj)
        {
        }

        public SetSimplePropertyObject(string menuTitle, string dialogPrompt, string propertyName, Func<bool> test, object obj) : base(menuTitle, dialogPrompt, test)
        {
            PropertyInfo mProperty = obj.GetType().GetProperty(propertyName, typeof(T));
            if (mProperty is null)
            {
                throw new ArgumentException("Property with given return type not found in object");
            }
            if (!mProperty.CanWrite || !mProperty.CanRead)
            {
                throw new MissingMethodException("Property must have a get and set accessor");
            }
            mGetValue = () => (T)mProperty.GetValue(obj, null);
            mSetValue = (val) => mProperty.SetValue(obj, val, null);
            ConstructDefaultColumnInfo();
        }

        public SetSimplePropertyObject(string menuTitle, string propertyName, Func<bool> test, object obj, List<ColumnDelegateStruct> columns) : this(menuTitle, "", propertyName, test, obj, columns)
        {
        }

        public SetSimplePropertyObject(string menuTitle, string dialogPrompt, string propertyName, Func<bool> test, object obj, List<ColumnDelegateStruct> columns) : base(menuTitle, dialogPrompt, columns, test)
        {
            PropertyInfo mProperty = obj.GetType().GetProperty(propertyName);
            if (mProperty.PropertyType != typeof(T))
            {
                throw new ArgumentException("Type mismatch between property and return value");
            }
            if (!mProperty.CanWrite || !mProperty.CanRead)
            {
                throw new MissingMethodException("Property must have a get and set accessor");
            }
            mGetValue = () => (T)mProperty.GetValue(obj, null);
            mSetValue = (val) => mProperty.SetValue(obj, val, null);
        }
    }

    /// <summary>
    /// A <see cref="SetSimpleValueObject{T}"/> that sets the value of a given <typeparamref name="TKey"/> in a given <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    /// <typeparam name="TKey">The type of the given dictionary's keys</typeparam>
    /// <typeparam name="TValue">The type of the given dictionary's values</typeparam>
    public sealed class SetSimpleDictionaryValueObject<TKey, TValue> : SetSimpleValueObject<TValue> where TValue : IConvertible
    {
        public SetSimpleDictionaryValueObject(string menuTitle, IDictionary<TKey, TValue> dict, TKey key, Func<bool> test) : this(menuTitle, "", dict, key, test)
        {
        }

        public SetSimpleDictionaryValueObject(string menuTitle, string dialogPrompt, IDictionary<TKey, TValue> dict, TKey key, Func<bool> test) : base(menuTitle, dialogPrompt, test)
        {
            if (!dict.ContainsKey(key))
            {
                throw new ArgumentException("Key not in dictionary");
            }
            mGetValue = () => dict[key];
            mSetValue = (val) => dict[key] = val;
            ConstructDefaultColumnInfo();
        }

        public SetSimpleDictionaryValueObject(string menuTitle, IDictionary<TKey, TValue> dict, TKey key, List<ColumnDelegateStruct> columns, Func<bool> test) : this(menuTitle, "", dict, key, columns, test)
        {
        }

        public SetSimpleDictionaryValueObject(string menuTitle, string dialogPrompt, IDictionary<TKey, TValue> dict, TKey key, List<ColumnDelegateStruct> columns, Func<bool> test) : base(menuTitle, dialogPrompt, columns, test)
        {
            if (!dict.ContainsKey(key))
            {
                throw new ArgumentException("Key not in dictionary");
            }
            mGetValue = () => dict[key];
            mSetValue = (val) => dict[key] = val;
        }
    }
}