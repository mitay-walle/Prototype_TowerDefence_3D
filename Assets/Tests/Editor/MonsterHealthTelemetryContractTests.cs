using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using TD.Monsters;

namespace TD.Tests
{
	public class MonsterHealthTelemetryContractTests
	{
		[Test]
		public void DamageTelemetryReportsDamageButNotLeakHealthReset()
		{
			var healthObject = new GameObject("MonsterHealthTelemetryContract");
			var health = healthObject.AddComponent<MonsterHealth>();
			health.onDamageTaken = new UnityEvent<float>();
			health.Initialize(100f);
			var damageReported = 0f;
			health.onDamageTaken.AddListener(damage => damageReported += damage);

			health.TakeDamage(7f);
			Assert.That(damageReported, Is.EqualTo(7f));

			health.TryLeak(null);
			Assert.That(damageReported, Is.EqualTo(7f));
			Object.DestroyImmediate(healthObject);
		}
	}
}
