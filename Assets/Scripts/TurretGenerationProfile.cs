using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[System.Serializable]
public class TurretGenerationProfile
{
	public int seed;
	[SerializeField, ReadOnly] int _lastSeed;
	public int Seed => _lastSeed = seed == 0 ? Random.Range(1, int.MaxValue) : seed;

	public Vector2 baseSizeRange = new Vector2(20, 36);
	public Vector2 baseFeetCountRange = new Vector2(4, 8);
	public Vector2 baseFeetHeightRange = new Vector2(4, 12);

	public Vector2 bodyWidthRange = new Vector2(16, 28);
	public Vector2 bodyHeightRange = new Vector2(10, 18);

	public Vector2 barrelLengthRange = new Vector2(20, 50);
	public Vector2 barrelThicknessRange = new Vector2(3, 6);

	public int sensorCount = 3;
	public Vector2 sensorSizeRange = new Vector2(3, 6);

	public Vector2 corePositionRange = new Vector2(-12, -4);
	public Vector2 coreSizeRange = new Vector2(4, 10);

	public List<Color> colorPalette = new List<Color>();
	public List<Color> emissionPalette = new List<Color>();

	private int cachedFeetHeight;
	private int cachedBodyHeight;
	private int cachedBaseTopY;
	private int cachedBodyWidth;
	private int cachedBodyDepth;
	private int cachedBarrelCount;
	private bool hasBarrel;
	private bool hasSuppressor;
	private bool hasBarrelAttachment;
	private bool hasFuelTanks;

	public void Randomize()
	{
		Random.InitState(Seed);
		GenerateColorPalette();
		cachedFeetHeight = Mathf.RoundToInt(RandomRange(baseFeetHeightRange.x, baseFeetHeightRange.y));
		cachedBodyHeight = Mathf.RoundToInt(RandomRange(bodyHeightRange.x, bodyHeightRange.y));
		cachedBodyWidth = Mathf.RoundToInt(RandomRange(bodyWidthRange.x, bodyWidthRange.y));
		if (cachedBodyWidth % 2 == 0) cachedBodyWidth++;
		cachedBodyDepth = cachedBodyWidth * 3 / 4;
		cachedBaseTopY = 0;
		cachedBarrelCount = RandomRangeInt(1, 4);
		hasBarrel = Random.value > 0.15f;
		hasSuppressor = hasBarrel && Random.value > 0.6f;
		hasBarrelAttachment = hasBarrel && Random.value > 0.5f;
		hasFuelTanks = Random.value > 0.5f;
	}

	public float GetBaseTopY()
	{
		return cachedBaseTopY;
	}

	private void GenerateColorPalette()
	{
		colorPalette.Clear();
		emissionPalette.Clear();

		Color primaryColor = RandomColor(0.2f, 0.5f);
		Color secondaryColor = RandomColor(0.3f, 0.6f);
		Color accentColor = RandomColor(0.4f, 0.7f);
		Color darkColor = RandomColor(0.05f, 0.2f);
		Color metalColor = new Color(RandomRange(0.4f, 0.6f), RandomRange(0.4f, 0.6f), RandomRange(0.4f, 0.6f));

		colorPalette.Add(primaryColor);
		colorPalette.Add(secondaryColor);
		colorPalette.Add(accentColor);
		colorPalette.Add(primaryColor * 0.8f);
		colorPalette.Add(darkColor);
		colorPalette.Add(darkColor * 1.2f);
		colorPalette.Add(primaryColor * 0.6f);
		colorPalette.Add(Color.black);
		colorPalette.Add(metalColor);

		emissionPalette.Add(new Color(RandomRange(0.3f, 1f), RandomRange(0.3f, 1f), RandomRange(0.8f, 1f)));
		emissionPalette.Add(new Color(RandomRange(0.8f, 1f), RandomRange(0.1f, 0.3f), RandomRange(0.1f, 0.3f)));
		emissionPalette.Add(new Color(RandomRange(0.2f, 0.5f), RandomRange(0.4f, 0.7f), RandomRange(0.8f, 1f)));
	}

	private Color RandomColor(float min, float max)
	{
		return new Color(RandomRange(min, max), RandomRange(min, max), RandomRange(min, max));
	}

	private float RandomRange(float min, float max)
	{
		return Random.Range(min, max);
	}

	private int RandomRangeInt(int min, int max)
	{
		return Random.Range(min, max);
	}

	public Color GetColor(int index)
	{
		return index >= 0 && index < colorPalette.Count ? colorPalette[index] : Color.white;
	}

	public Color GetEmissionColor(int index)
	{
		return index >= 0 && index < emissionPalette.Count ? emissionPalette[index] : Color.white;
	}

	private void AddChamferedBox(TurretVoxelGenerator.TurretPart part,
	                             Vector3 position,
	                             int width,
	                             int height,
	                             int depth,
	                             int chamferSize,
	                             int colorIndex,
	                             int emissionColorIndex = -1,
	                             float emissionIntensity = 0f)
	{
		int halfWidth = width / 2;
		int halfDepth = depth / 2;

		for (int y = 0; y < height; y++)
		{
			for (int x = -halfWidth; x <= halfWidth; x++)
			{
				for (int z = -halfDepth; z <= halfDepth; z++)
				{
					int distX = Mathf.Abs(x);
					int distZ = Mathf.Abs(z);

					bool isChamferedCorner = (distX >= halfWidth - chamferSize + 1 && distZ >= halfDepth - chamferSize + 1);

					if (isChamferedCorner && (y < chamferSize || y >= height - chamferSize))
					{
						int cornerDist = (distX - (halfWidth - chamferSize)) + (distZ - (halfDepth - chamferSize));
						if (cornerDist > chamferSize) continue;
					}

					part.voxels.Add(new TurretVoxelGenerator.VoxelData
					{
						position = position + new Vector3(x, y, z),
						size = Vector3.one,
						colorIndex = colorIndex,
						emissionColorIndex = emissionColorIndex,
						emissionIntensity = emissionIntensity
					});
				}
			}
		}
	}

	public void Generate(TurretVoxelGenerator.TurretPart basePlate, TurretVoxelGenerator.TurretPart turret)
	{
		GenerateBasePlate(basePlate);
		GenerateRotationRing(basePlate);
		GenerateTurretBody(turret);
		GenerateBarrelHousing(turret);
		if (hasBarrel) GenerateBarrel(turret);
		GenerateSensors(turret);
		GeneratePowerCore(turret);
		GenerateArmorPanels(turret);
		GenerateCoolingVents(turret);
		if (hasFuelTanks) GenerateFuelTanks(turret);
	}

	private void GenerateBasePlate(TurretVoxelGenerator.TurretPart basePlate)
	{
		int baseSize = Mathf.RoundToInt(RandomRange(baseSizeRange.x, baseSizeRange.y));
		if (baseSize % 2 == 0) baseSize++;

		AddChamferedBox(basePlate, new Vector3(0, cachedFeetHeight, 0), baseSize, 1, baseSize, 0, 0);

		int baseBorderThickness = 4;
		int halfSize = baseSize / 2;

		for (int y = cachedFeetHeight + 1; y < cachedFeetHeight + baseBorderThickness; y++)
		{
			for (int side = 0; side < 4; side++)
			{
				for (int i = -halfSize + 1; i < halfSize; i++)
				{
					int x = side == 0 ? halfSize : (side == 2 ? -halfSize : i);
					int z = side == 1 ? halfSize : (side == 3 ? -halfSize : i);

					basePlate.voxels.Add(new TurretVoxelGenerator.VoxelData
					{
						position = new Vector3(x, y, z),
						size = Vector3.one,
						colorIndex = 1
					});
				}
			}
		}

		int feetCount = Mathf.RoundToInt(RandomRange(baseFeetCountRange.x, baseFeetCountRange.y));
		float radius = baseSize * 0.4f;

		for (int i = 0; i < feetCount; i++)
		{
			float angle = (360f / feetCount * i) * Mathf.Deg2Rad;
			int x = Mathf.RoundToInt(Mathf.Cos(angle) * radius);
			int z = Mathf.RoundToInt(Mathf.Sin(angle) * radius);

			for (int fy = 0; fy < cachedFeetHeight; fy++)
			{
				int footWidth = 4 + (cachedFeetHeight - fy) / 3;
				AddChamferedBox(basePlate, new Vector3(x, fy, z), footWidth, 1, footWidth, 0, 1);
			}
		}

		cachedBaseTopY = cachedFeetHeight + baseBorderThickness;
	}

	private void GenerateRotationRing(TurretVoxelGenerator.TurretPart basePlate)
	{
		int segments = RandomRangeInt(32, 48);
		float radius = cachedBodyWidth * 0.6f;

		for (int layer = 0; layer < 2; layer++)
		{
			for (int i = 0; i < segments; i++)
			{
				float angle = (360f / segments * i + layer * 3.75f) * Mathf.Deg2Rad;
				int x = Mathf.RoundToInt(Mathf.Cos(angle) * radius);
				int z = Mathf.RoundToInt(Mathf.Sin(angle) * radius);

				basePlate.voxels.Add(new TurretVoxelGenerator.VoxelData
				{
					position = new Vector3(x, cachedBaseTopY + layer, z),
					size = Vector3.one,
					colorIndex = 2
				});
			}
		}

		cachedBaseTopY += 2;
	}

	private void GenerateTurretBody(TurretVoxelGenerator.TurretPart turret)
	{
		int baseHeight = cachedBodyHeight / 3;

		AddChamferedBox(turret, new Vector3(0, baseHeight / 2, 0), cachedBodyWidth, baseHeight, cachedBodyDepth, 2, 0);
	}

	private void GenerateBarrelHousing(TurretVoxelGenerator.TurretPart turret)
	{
		int housingHeight = cachedBodyHeight * 2 / 3;
		int housingWidth = cachedBodyWidth * 2 / 3;
		int housingDepth = cachedBodyDepth / 2;
		int baseHeight = cachedBodyHeight / 3;

		AddChamferedBox(turret, new Vector3(0, baseHeight + housingHeight / 2, cachedBodyDepth / 4), housingWidth, housingHeight, housingDepth, 2, 3);
	}

	private void GenerateBarrel(TurretVoxelGenerator.TurretPart turret)
	{
		int barrelLength = Mathf.RoundToInt(RandomRange(barrelLengthRange.x, barrelLengthRange.y));
		int barrelThickness = Mathf.RoundToInt(RandomRange(barrelThicknessRange.x, barrelThicknessRange.y));
		int barrelY = cachedBodyHeight * 2 / 3;
		int startZ = cachedBodyDepth / 2 + 2;

		if (cachedBarrelCount == 1)
		{
			bool centered = Random.value > 0.5f;
			int xOffset = centered ? 0 : (Random.value > 0.5f ? 1 : -1) * (cachedBodyWidth / 6);

			AddChamferedBox(turret, new Vector3(xOffset, barrelY, startZ + barrelLength / 2), barrelThickness, barrelThickness, barrelLength, 0, 4);

			if (hasSuppressor)
			{
				int suppressorLength = RandomRangeInt(6, 12);
				int suppressorThickness = barrelThickness + 2;
				AddChamferedBox(turret, new Vector3(xOffset, barrelY, startZ + barrelLength + suppressorLength / 2), suppressorThickness,
					suppressorThickness, suppressorLength, 0, 1);
			}
			else
			{
				for (int mz = 0; mz < 8; mz++)
				{
					turret.voxels.Add(new TurretVoxelGenerator.VoxelData
					{
						position = new Vector3(xOffset, barrelY, startZ + barrelLength + mz),
						size = Vector3.one,
						colorIndex = 5
					});
				}
			}

			if (hasBarrelAttachment)
			{
				int attachmentPos = RandomRangeInt(startZ + 5, startZ + barrelLength / 2);
				int attachmentSize = RandomRangeInt(3, 6);
				AddChamferedBox(turret, new Vector3(xOffset, barrelY - barrelThickness / 2 - attachmentSize / 2, attachmentPos), barrelThickness + 2,
					attachmentSize, attachmentSize, 1, 1);
			}

			int supportLength = RandomRangeInt(6, 12);
			AddChamferedBox(turret, new Vector3(xOffset, barrelY - 3, startZ - supportLength / 2), barrelThickness + 4, 4, supportLength, 1, 1);
		}
		else if (cachedBarrelCount == 2)
		{
			int spacing = cachedBodyWidth / 5;
			for (int side = -1; side <= 1; side += 2)
			{
				AddChamferedBox(turret, new Vector3(side * spacing, barrelY, startZ + barrelLength / 2), barrelThickness, barrelThickness,
					barrelLength, 0, 4);

				if (hasSuppressor)
				{
					int suppressorLength = RandomRangeInt(6, 10);
					int suppressorThickness = barrelThickness + 2;
					AddChamferedBox(turret, new Vector3(side * spacing, barrelY, startZ + barrelLength + suppressorLength / 2), suppressorThickness,
						suppressorThickness, suppressorLength, 0, 1);
				}
				else
				{
					for (int mz = 0; mz < 6; mz++)
					{
						turret.voxels.Add(new TurretVoxelGenerator.VoxelData
						{
							position = new Vector3(side * spacing, barrelY, startZ + barrelLength + mz),
							size = Vector3.one,
							colorIndex = 5
						});
					}
				}

				if (hasBarrelAttachment)
				{
					int attachmentPos = RandomRangeInt(startZ + 5, startZ + barrelLength / 2);
					int attachmentSize = RandomRangeInt(3, 5);
					AddChamferedBox(turret, new Vector3(side * spacing, barrelY - barrelThickness / 2 - attachmentSize / 2, attachmentPos),
						barrelThickness + 2, attachmentSize, attachmentSize, 1, 1);
				}

				int supportLength = RandomRangeInt(6, 12);
				AddChamferedBox(turret, new Vector3(side * spacing, barrelY - 2, startZ - supportLength / 2), barrelThickness + 2, 3, supportLength,
					1, 1);
			}
		}
		else
		{
			int spacing = cachedBodyWidth / 6;
			for (int i = -1; i <= 1; i++)
			{
				AddChamferedBox(turret, new Vector3(i * spacing, barrelY, startZ + barrelLength / 2), barrelThickness, barrelThickness, barrelLength,
					0, 4);

				if (hasSuppressor)
				{
					int suppressorLength = RandomRangeInt(5, 9);
					int suppressorThickness = barrelThickness + 1;
					AddChamferedBox(turret, new Vector3(i * spacing, barrelY, startZ + barrelLength + suppressorLength / 2), suppressorThickness,
						suppressorThickness, suppressorLength, 0, 1);
				}
				else
				{
					for (int mz = 0; mz < 5; mz++)
					{
						turret.voxels.Add(new TurretVoxelGenerator.VoxelData
						{
							position = new Vector3(i * spacing, barrelY, startZ + barrelLength + mz),
							size = Vector3.one,
							colorIndex = 5
						});
					}
				}

				int supportLength = RandomRangeInt(5, 10);
				AddChamferedBox(turret, new Vector3(i * spacing, barrelY - 2, startZ - supportLength / 2), barrelThickness + 2, 2, supportLength, 1,
					1);
			}
		}
	}

	private void GenerateSensors(TurretVoxelGenerator.TurretPart turret)
	{
		bool symmetric = Random.value > 0.5f;
		int halfWidth = cachedBodyWidth / 2;
		int halfDepth = cachedBodyDepth / 2;

		for (int i = 0; i < sensorCount; i++)
		{
			int sensorSize = Mathf.RoundToInt(RandomRange(sensorSizeRange.x, sensorSizeRange.y));
			bool protruding = Random.value > 0.5f;

			int xPos, yPos, zPos;

			if (symmetric && i < 2)
			{
				int side = i == 0 ? 1 : -1;
				xPos = side * halfWidth / 2;
				yPos = cachedBodyHeight / 2;
				zPos = halfDepth / 2;
			}
			else
			{
				xPos = RandomRangeInt(-halfWidth / 2, halfWidth / 2 + 1);
				yPos = RandomRangeInt(cachedBodyHeight / 3, cachedBodyHeight);
				zPos = RandomRangeInt(-halfDepth / 3, halfDepth);
			}

			if (protruding)
			{
				int protrusion = RandomRangeInt(2, 4);
				AddChamferedBox(turret, new Vector3(xPos, yPos - protrusion / 2, zPos), sensorSize - 1, protrusion, sensorSize - 1, 0, 1);
			}

			AddChamferedBox(turret, new Vector3(xPos, yPos + sensorSize / 2, zPos), sensorSize, sensorSize, sensorSize, 1, i % 2 == 0 ? 6 : 7, i % 2,
				RandomRange(1.5f, 3f));
		}
	}

	private void GeneratePowerCore(TurretVoxelGenerator.TurretPart turret)
	{
		int coreSize = Mathf.RoundToInt(RandomRange(coreSizeRange.x, coreSizeRange.y));
		int coreY = cachedBodyHeight / 4;
		int coreZ = -cachedBodyDepth / 3;

		AddChamferedBox(turret, new Vector3(0, coreY, coreZ), coreSize, coreSize, coreSize, 0, 1);

		AddChamferedBox(turret, new Vector3(0, coreY, coreZ), coreSize - 2, coreSize - 2, coreSize - 2, 0, 8, 2, RandomRange(2f, 4f));
	}

	private void GenerateArmorPanels(TurretVoxelGenerator.TurretPart turret)
	{
		int halfWidth = cachedBodyWidth / 2;
		int halfDepth = cachedBodyDepth / 2;

		for (int side = 0; side < 2; side++)
		{
			int panelCount = RandomRangeInt(2, 4);
			for (int p = 0; p < panelCount; p++)
			{
				int xPos = (side == 0 ? 1 : -1) * (halfWidth + RandomRangeInt(1, 3));
				int yPos = RandomRangeInt(cachedBodyHeight / 4, cachedBodyHeight * 3 / 4);
				int panelHeight = RandomRangeInt(4, 8);
				int panelDepth = RandomRangeInt(4, 8);

				AddChamferedBox(turret, new Vector3(xPos, yPos, halfDepth / 3), 2, panelHeight, panelDepth, 0, 1);
			}
		}
	}

	private void GenerateCoolingVents(TurretVoxelGenerator.TurretPart turret)
	{
		int ventCount = RandomRangeInt(2, 4);

		for (int i = 0; i < ventCount; i++)
		{
			int halfWidth = cachedBodyWidth / 2;
			int side = i % 2 == 0 ? 1 : -1;
			int ventX = side * halfWidth;
			int ventHeight = RandomRangeInt(cachedBodyHeight / 4, cachedBodyHeight / 2);
			int ventY = RandomRangeInt(cachedBodyHeight / 5, cachedBodyHeight / 2);
			int ventDepth = RandomRangeInt(6, 12);
			int ventZ = RandomRangeInt(-cachedBodyDepth / 3, cachedBodyDepth / 3);

			AddChamferedBox(turret, new Vector3(ventX, ventY + ventHeight / 2, ventZ), 2, ventHeight, ventDepth, 0, 4, 0, 0.3f);
		}
	}

	private void GenerateFuelTanks(TurretVoxelGenerator.TurretPart turret)
	{
		int tankCount = RandomRangeInt(1, 3);
		int halfWidth = cachedBodyWidth / 2;
		int halfDepth = cachedBodyDepth / 2;

		for (int i = 0; i < tankCount; i++)
		{
			int tankWidth = RandomRangeInt(4, 7);
			int tankHeight = RandomRangeInt(6, 10);
			int tankDepth = RandomRangeInt(4, 7);

			int side = i % 2 == 0 ? 1 : -1;
			int xPos = side * (halfWidth - tankWidth / 2);
			int yPos = cachedBodyHeight / 4;
			int zPos = RandomRangeInt(-halfDepth / 2, 0);

			AddChamferedBox(turret, new Vector3(xPos, yPos + tankHeight / 2, zPos), tankWidth, tankHeight, tankDepth, 1, 3);

			int capSize = 2;
			AddChamferedBox(turret, new Vector3(xPos, yPos + tankHeight + capSize / 2, zPos), capSize, capSize, capSize, 0, 1);
		}
	}
}