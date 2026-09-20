using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Spells;

namespace RPGame.Core.Tests.Spells
{
    public sealed class RuntimeSpellBehaviorExecutorTests
    {
        [Test]
        public void Execute_WhenBehaviorsAreMixed_UsesCanonicalPhaseOrder()
        {
            List<string> executed = new();
            List<IRuntimeSpellBehavior> behaviors = new()
            {
                new TestBehavior("Aftermath", RuntimeSpellBehaviorPhase.Aftermath),
                new TestBehavior("PrimaryEffect", RuntimeSpellBehaviorPhase.PrimaryEffect),
                new TestBehavior("MarkProgress", RuntimeSpellBehaviorPhase.MarkProgress),
                new TestBehavior("PreResolve", RuntimeSpellBehaviorPhase.PreResolve),
                new TestBehavior("PostEffect", RuntimeSpellBehaviorPhase.PostEffect),
                new TestBehavior("MarkConsumption", RuntimeSpellBehaviorPhase.MarkConsumption)
            };

            RuntimeSpellBehaviorExecutor.Execute<ITestBehavior>(
                behaviors,
                behavior => executed.Add(behavior.Name));

            CollectionAssert.AreEqual(
                new[]
                {
                    "PreResolve",
                    "MarkConsumption",
                    "PrimaryEffect",
                    "PostEffect",
                    "MarkProgress",
                    "Aftermath"
                },
                executed);
        }

        [Test]
        public void Execute_WhenMarkPhasesAreReversed_ExecutesConsumptionBeforeProgress()
        {
            List<string> executed = new();
            List<IRuntimeSpellBehavior> behaviors = new()
            {
                new TestBehavior("MarkProgress", RuntimeSpellBehaviorPhase.MarkProgress),
                new TestBehavior("MarkConsumption", RuntimeSpellBehaviorPhase.MarkConsumption)
            };

            RuntimeSpellBehaviorExecutor.Execute<ITestBehavior>(
                behaviors,
                behavior => executed.Add(behavior.Name));

            CollectionAssert.AreEqual(
                new[] { "MarkConsumption", "MarkProgress" },
                executed);
        }

        [Test]
        public void Execute_WhenBehaviorsSharePhase_PreservesInputOrder()
        {
            List<string> executed = new();
            List<IRuntimeSpellBehavior> behaviors = new()
            {
                new TestBehavior("First", RuntimeSpellBehaviorPhase.PrimaryEffect),
                new TestBehavior("Second", RuntimeSpellBehaviorPhase.PrimaryEffect),
                new TestBehavior("Third", RuntimeSpellBehaviorPhase.PrimaryEffect)
            };

            RuntimeSpellBehaviorExecutor.Execute<ITestBehavior>(
                behaviors,
                behavior => executed.Add(behavior.Name));

            CollectionAssert.AreEqual(new[] { "First", "Second", "Third" }, executed);
        }

        [Test]
        public void Execute_WhenBehaviorDoesNotImplementRequestedInterface_IgnoresIt()
        {
            List<string> executed = new();
            List<IRuntimeSpellBehavior> behaviors = new()
            {
                new OtherBehavior(RuntimeSpellBehaviorPhase.PreResolve),
                new TestBehavior("Executed", RuntimeSpellBehaviorPhase.PrimaryEffect)
            };

            RuntimeSpellBehaviorExecutor.Execute<ITestBehavior>(
                behaviors,
                behavior => executed.Add(behavior.Name));

            CollectionAssert.AreEqual(new[] { "Executed" }, executed);
        }

        private interface ITestBehavior : IRuntimeSpellBehavior
        {
            string Name { get; }
        }

        private sealed class TestBehavior : ITestBehavior
        {
            public TestBehavior(string name, RuntimeSpellBehaviorPhase phase)
            {
                Name = name;
                Phase = phase;
            }

            public string Name { get; }
            public RuntimeSpellBehaviorPhase Phase { get; }
        }

        private sealed class OtherBehavior : IRuntimeSpellBehavior
        {
            public OtherBehavior(RuntimeSpellBehaviorPhase phase)
            {
                Phase = phase;
            }

            public RuntimeSpellBehaviorPhase Phase { get; }
        }
    }
}
