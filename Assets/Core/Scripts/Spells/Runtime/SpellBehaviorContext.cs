using System.Collections.Generic;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Core.Spells
{
    public readonly struct SpellBehaviorContext
    {
        private readonly List<IResolveResult> resolveResults;
        private readonly List<IExecutionState> executionStates;

        public SpellBehaviorContext(
            GameObject target,
            GameObject source,
            SpellId spellId,
            IStatusReceiver statusReceiver)
        {
            Target = target;
            Source = source;
            SpellId = spellId;
            StatusReceiver = statusReceiver;
            resolveResults = new List<IResolveResult>();
            executionStates = new List<IExecutionState>();
        }

        public GameObject Target { get; }
        public GameObject Source { get; }
        public SpellId SpellId { get; }
        public IStatusReceiver StatusReceiver { get; }

        public void AddResolveResult<T>(T result)
            where T : IResolveResult
        {
            if (resolveResults == null)
            {
                return;
            }

            resolveResults.Add(result);
        }

        public bool TryGetResolveResult<T>(out T result)
            where T : IResolveResult
        {
            if (resolveResults != null)
            {
                for (int resultIndex = resolveResults.Count - 1; resultIndex >= 0; resultIndex--)
                {
                    if (resolveResults[resultIndex] is T typedResult)
                    {
                        result = typedResult;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        public bool TryGetResolveResults<T>(out IReadOnlyList<T> results)
            where T : IResolveResult
        {
            if (resolveResults == null)
            {
                results = default;
                return false;
            }

            List<T> typedResults = new();
            for (int resultIndex = 0; resultIndex < resolveResults.Count; resultIndex++)
            {
                if (resolveResults[resultIndex] is T typedResult)
                {
                    typedResults.Add(typedResult);
                }
            }

            results = typedResults;
            return typedResults.Count > 0;
        }

        public void AddExecutionState<T>(T state)
            where T : IExecutionState
        {
            if (executionStates == null)
            {
                return;
            }

            executionStates.Add(state);
        }

        public bool TryGetExecutionState<T>(out T state)
            where T : IExecutionState
        {
            if (executionStates != null)
            {
                for (int stateIndex = executionStates.Count - 1; stateIndex >= 0; stateIndex--)
                {
                    if (executionStates[stateIndex] is T typedState)
                    {
                        state = typedState;
                        return true;
                    }
                }
            }

            state = default;
            return false;
        }
    }
}
