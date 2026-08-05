using NUnit.Framework;
using TD.Monsters;
using TD.Towers;
using UnityEngine;

namespace TD.Tests
{
	public class MonsterMoveContractTests
	{
		[Test]
		public void MovementUsesExplicitBaseTarget()
		{
			var enemy = new GameObject("MonsterMoveContractTests.Enemy");
			var baseObject = new GameObject("MonsterMoveContractTests.Base");

			try
			{
				var movement = enemy.AddComponent<MonsterMove>();
				baseObject.AddComponent<Rigidbody>();
				baseObject.AddComponent<BoxCollider>();
				var playerBase = baseObject.AddComponent<PlayerBase>();

				Assert.That(movement, Is.Not.Null);
				Assert.That(playerBase, Is.Not.Null);
				Assert.That(movement.BaseTarget, Is.Null);
				Assert.That(movement.Initialize(null), Is.False, "Null base target must be rejected.");
				Assert.That(movement.Initialize(playerBase), Is.True, "A valid PlayerBase must be accepted.");
				Assert.That(movement.BaseTarget, Is.SameAs(playerBase));
			}
			finally
			{
				Object.DestroyImmediate(baseObject);
				Object.DestroyImmediate(enemy);
			}
		}


		[Test]
		public void NonFinitePathDistanceCannotCountAsRouteProgress()
		{
			Assert.That(MonsterMove.IsRouteProgressFinite(float.PositiveInfinity, 4f), Is.False);
			Assert.That(MonsterMove.IsRouteProgressFinite(4f, float.PositiveInfinity), Is.False);
			Assert.That(MonsterMove.IsRouteProgressFinite(4f, 3f), Is.True);
		}
	}
}
