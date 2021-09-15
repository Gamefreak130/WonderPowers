using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using System;
using System.Collections.Generic;

namespace Gamefreak130.Common
{
    public static class Tunings
    {
        internal static InteractionTuning Inject(Type oldType, Type oldTarget, Type newType, Type newTarget, bool clone)
        {
            InteractionTuning interactionTuning = AutonomyTuning.GetTuning(newType.FullName, newTarget.FullName);
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
            InteractionObjectPair.sTuningCache.Remove(new Pair<Type, Type>(newType, newTarget));
            return interactionTuning;
        }

        private static InteractionTuning CloneTuning(InteractionTuning oldTuning) => new InteractionTuning()
        {
            mFlags = oldTuning.mFlags,
            ActionTopic = oldTuning.ActionTopic,
            AlwaysChooseBest = oldTuning.AlwaysChooseBest,
            Availability = CloneAvailability(oldTuning.Availability),
            CodeVersion = oldTuning.CodeVersion,
            FullInteractionName = oldTuning.FullInteractionName,
            FullObjectName = oldTuning.FullObjectName,
            mChecks = oldTuning.mChecks is null ? null : new List<CheckWhat>(oldTuning.mChecks),
            mTradeoff = CloneTradeoff(oldTuning.mTradeoff),
            PosturePreconditions = oldTuning.PosturePreconditions,
            ScoringFunction = oldTuning.ScoringFunction,
            ScoringFunctionOnlyAppliesToSpecificCommodity = oldTuning.ScoringFunctionOnlyAppliesToSpecificCommodity,
            ScoringFunctionString = oldTuning.ScoringFunctionString,
            ShortInteractionName = oldTuning.ShortInteractionName,
            ShortObjectName = oldTuning.ShortObjectName
        };

        private static Tradeoff CloneTradeoff(Tradeoff old) => new Tradeoff()
        {
            mFlags = old.mFlags,
            mInputs = old.mInputs is null ? null : new List<CommodityChange>(old.mInputs),
            mName = old.mName,
            mNumParameters = old.mNumParameters,
            mOutputs = old.mOutputs is null ? null : new List<CommodityChange>(old.mOutputs),
            mVariableRestrictions = old.mVariableRestrictions,
            TimeEstimate = old.TimeEstimate
        };

        private static Availability CloneAvailability(Availability old) => new Availability()
        {
            mFlags = old.mFlags,
            AgeSpeciesAvailabilityFlags = old.AgeSpeciesAvailabilityFlags,
            CareerThresholdType = old.CareerThresholdType,
            CareerThresholdValue = old.CareerThresholdValue,
            ExcludingBuffs = old.ExcludingBuffs is null ? null : new List<BuffNames>(old.ExcludingBuffs),
            ExcludingTraits = old.ExcludingTraits is null ? null : new List<TraitNames>(old.ExcludingTraits),
            MoodThresholdType = old.MoodThresholdType,
            MoodThresholdValue = old.MoodThresholdValue,
            MotiveThresholdType = old.MotiveThresholdType,
            MotiveThresholdValue = old.MotiveThresholdValue,
            RequiredBuffs = old.RequiredBuffs is null ? null : new List<BuffNames>(old.RequiredBuffs),
            RequiredTraits = old.RequiredTraits is null ? null : new List<TraitNames>(old.RequiredTraits),
            SkillThresholdType = old.SkillThresholdType,
            SkillThresholdValue = old.SkillThresholdValue,
            WorldRestrictionType = old.WorldRestrictionType,
            OccultRestrictions = old.OccultRestrictions,
            OccultRestrictionType = old.OccultRestrictionType,
            SnowLevelValue = old.SnowLevelValue,
            WorldRestrictionWorldNames = old.WorldRestrictionWorldNames is null ? null : new List<WorldName>(old.WorldRestrictionWorldNames),
            WorldRestrictionWorldTypes = old.WorldRestrictionWorldTypes is null ? null : new List<WorldType>(old.WorldRestrictionWorldTypes)
        };
    }
}
