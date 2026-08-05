using NUnit.Framework;
using TD.Monsters;
using UnityEngine;
using UnityEngine.Events;

namespace TD.Tests
{
	public class MonsterTerminalContractTests
	{
		[Test]
		public void LeakIsIdempotentAndDoesNotRaiseDeath()
		{
			var enemy = new GameObject("MonsterTerminalContractTest");
			var health = enemy.AddComponent<MonsterHealth>();
			Assert.That(health, Is.Not.Null);
			health.onDeath = new UnityEvent();
			health.onLeak = new UnityEvent();
			var deathCount = 0;
			var leakCount = 0;
			health.onDeath.AddListener(() => deathCount++);
			health.onLeak.AddListener(() => leakCount++);
			health.Initialize(10f);

			Assert.That(health.TryLeak(null), Is.True);
			Assert.That(health.TryLeak(null), Is.False);
			Assert.That(health.IsAlive, Is.False);
			Assert.That(health.TerminalReason, Is.EqualTo(MonsterTerminalReason.Leak));
			Assert.That(deathCount, Is.EqualTo(0));
			Assert.That(leakCount, Is.EqualTo(1));

			Object.DestroyImmediate(enemy);
		}
	}
}
