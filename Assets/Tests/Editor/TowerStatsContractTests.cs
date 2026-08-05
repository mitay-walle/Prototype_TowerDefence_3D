using System.Collections.Generic;
using NUnit.Framework;
using TD.Towers;

namespace TD.Tests
{
	public class TowerStatsContractTests
	{
		[Test]
		public void AuthoredTowerStatsExposeDistinctRolesAndUpgradePaths()
		{
			var generalist = UnityEngine.Resources.Load<TowerStatsSO>("TowerStats/TowerStatsSO 00 Basic");
			var area = UnityEngine.Resources.Load<TowerStatsSO>("TowerStats/TowerStatsSO 01 Tesla");
			var scaling = UnityEngine.Resources.Load<TowerStatsSO>("TowerStats/TowerStatsSO 02 Clever Girl");

			Assert.That(generalist, Is.Not.Null);
			Assert.That(area, Is.Not.Null);
			Assert.That(scaling, Is.Not.Null);

			var roleKeys = new HashSet<string>
			{
				generalist.Role.TableEntryReference.Key,
				area.Role.TableEntryReference.Key,
				scaling.Role.TableEntryReference.Key
			};

			Assert.That(roleKeys, Has.Count.EqualTo(3));
			Assert.That(generalist.upgradeRules, Is.Empty);
			Assert.That(area.upgradeRules, Is.Not.Empty);
			Assert.That(scaling.upgradeRules, Is.Not.Empty);
			Assert.That(area.DefensiveIdentity.TableEntryReference.Key, Is.Not.EqualTo(generalist.DefensiveIdentity.TableEntryReference.Key));
			Assert.That(scaling.DefensiveIdentity.TableEntryReference.Key, Is.Not.EqualTo(generalist.DefensiveIdentity.TableEntryReference.Key));
		}
	}
}
