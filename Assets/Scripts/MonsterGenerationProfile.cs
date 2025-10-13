using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TD
{
	[System.Serializable]
	public class MonsterGenerationProfile : GenerationProfile
	{
		public List<Color> colorPalette = new List<Color>();
		public List<Color> emissionPalette = new List<Color>();
		public Part basePlate = new Part { name = "Base" };
		public Part Rotating = new Part { name = "Turret" };

		[SerializeField, ReadOnly] private GameObject baseObject;
		[SerializeField, ReadOnly] private GameObject turretObject;

		private enum BodyType { Standing, Crawling }
		private enum SkinPattern { Solid, Spotted, Striped, Noise }

		private BodyType bodyType;
		private int limbCount;
		private bool hasTail;
		private bool hasHair;
		private SkinPattern skinPattern;
		private int bodyWidth;
		private int bodyHeight;
		private int bodyDepth;
		private int headSize;
		private int eyeCount;
		private int baseTopY;

		protected override void Randomize()
		{
			Random.InitState(Seed);
			GenerateColorPalette();

			bodyType = Random.value > 0.5f ? BodyType.Standing : BodyType.Crawling;
			limbCount = RandomRangeInt(2, 8);
			hasTail = Random.value > 0.5f;
			hasHair = Random.value > 0.4f;
			skinPattern = (SkinPattern)RandomRangeInt(0, 4);

			bodyWidth = RandomRangeInt(12, 24);
			if (bodyWidth % 2 == 0) bodyWidth++;

			bodyDepth = bodyType == BodyType.Standing ? bodyWidth * 2 / 3 : bodyWidth * 3 / 2;
			bodyHeight = bodyType == BodyType.Standing ? RandomRangeInt(20, 35) : RandomRangeInt(8, 15);

			headSize = RandomRangeInt(8, 16);
			eyeCount = RandomRangeInt(1, 4);
			baseTopY = 0;
		}

		public override void Generate(VoxelGenerator generator)
		{
			base.Generate(generator);

			basePlate.voxels.Clear();
			Rotating.voxels.Clear();

			Generate(basePlate, Rotating);
			GenerateTurret(generator);
		}

		private void GenerateTurret(VoxelGenerator generator)
		{
			var transform = generator.transform;
			float baseTopY = GetBaseTopY();

			if (baseObject == null)
			{
				baseObject = new GameObject("Base");
				baseObject.transform.SetParent(transform);
				baseObject.transform.localPosition = Vector3.zero;
			}

			if (turretObject == null)
			{
				turretObject = new GameObject("Turret");
				turretObject.transform.SetParent(transform);
			}

			turretObject.transform.localPosition = new Vector3(0, baseTopY * generator.voxelSize, 0);

			ClearMeshes(baseObject.transform);
			ClearMeshes(turretObject.transform);

			GeneratePart(generator, basePlate, baseObject.transform, Vector3.zero);
			GeneratePart(generator, Rotating, turretObject.transform, Vector3.zero);
		}

		public float GetBaseTopY() => baseTopY;

		private void GenerateColorPalette()
		{
			colorPalette.Clear();
			emissionPalette.Clear();

			Color primaryColor = RandomColor(0.2f, 0.6f);
			Color secondaryColor = RandomColor(0.3f, 0.7f);
			Color accentColor = RandomColor(0.4f, 0.8f);
			Color darkColor = RandomColor(0.05f, 0.2f);

			colorPalette.Add(primaryColor);
			colorPalette.Add(secondaryColor);
			colorPalette.Add(accentColor);
			colorPalette.Add(primaryColor * 0.7f);
			colorPalette.Add(darkColor);
			colorPalette.Add(secondaryColor * 0.6f);
			colorPalette.Add(Color.Lerp(primaryColor, secondaryColor, 0.5f));
			colorPalette.Add(Color.white * 0.9f);

			emissionPalette.Add(new Color(RandomRange(0.8f, 1f), RandomRange(0.3f, 0.6f), RandomRange(0.1f, 0.3f)));
			emissionPalette.Add(new Color(RandomRange(0.2f, 0.4f), RandomRange(0.7f, 1f), RandomRange(0.2f, 0.4f)));
			emissionPalette.Add(new Color(RandomRange(0.7f, 1f), RandomRange(0.7f, 1f), RandomRange(0.1f, 0.3f)));
		}

		private Color RandomColor(float min, float max)
		{
			return new Color(RandomRange(min, max), RandomRange(min, max), RandomRange(min, max));
		}

		private float RandomRange(float min, float max) => Random.Range(min, max);
		private int RandomRangeInt(int min, int max) => Random.Range(min, max);

		protected override Color GetColor(int index)
		{
			return index >= 0 && index < colorPalette.Count ? colorPalette[index] : Color.white;
		}

		protected override Color GetEmissionColor(int index)
		{
			return index >= 0 && index < emissionPalette.Count ? emissionPalette[index] : Color.white;
		}

		private void AddBox(Part part,
		                    Vector3 position,
		                    int width,
		                    int height,
		                    int depth,
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
						int finalColorIndex = GetPatternColor(position.x + x, position.y + y, position.z + z, colorIndex);

						part.voxels.Add(new VoxelData
						{
							position = position + new Vector3(x, y, z),
							size = Vector3.one,
							colorIndex = finalColorIndex,
							emissionColorIndex = emissionColorIndex,
							emissionIntensity = emissionIntensity
						});
					}
				}
			}
		}

		private int GetPatternColor(float x, float y, float z, int baseColorIndex)
		{
			switch (skinPattern)
			{
				case SkinPattern.Solid:
					return baseColorIndex;

				case SkinPattern.Spotted:
					float spotNoise = Mathf.PerlinNoise(x * 0.15f, z * 0.15f);
					return spotNoise > 0.65f ? (baseColorIndex + 1) % colorPalette.Count : baseColorIndex;

				case SkinPattern.Striped:
					return Mathf.FloorToInt(x * 0.3f) % 2 == 0 ? baseColorIndex : (baseColorIndex + 1) % colorPalette.Count;

				case SkinPattern.Noise:
					float noise = Mathf.PerlinNoise(x * 0.2f + y * 0.1f, z * 0.2f);
					if (noise < 0.33f) return baseColorIndex;
					if (noise < 0.66f) return (baseColorIndex + 1) % colorPalette.Count;

					return (baseColorIndex + 2) % colorPalette.Count;
			}

			return baseColorIndex;
		}

		private void AddEllipsoid(Part part, Vector3 center, int radiusX, int radiusY, int radiusZ, int colorIndex)
		{
			for (int y = -radiusY; y <= radiusY; y++)
			{
				for (int x = -radiusX; x <= radiusX; x++)
				{
					for (int z = -radiusZ; z <= radiusZ; z++)
					{
						float dist = (x * x) / (float)(radiusX * radiusX) + (y * y) / (float)(radiusY * radiusY) +
						             (z * z) / (float)(radiusZ * radiusZ);

						if (dist <= 1f)
						{
							int finalColorIndex = GetPatternColor(center.x + x, center.y + y, center.z + z, colorIndex);

							part.voxels.Add(new VoxelData
							{
								position = center + new Vector3(x, y, z),
								size = Vector3.one,
								colorIndex = finalColorIndex
							});
						}
					}
				}
			}
		}

		public void Generate(Part basePlate, Part turret)
		{
			GenerateLimbs(basePlate);
			GenerateBody(turret);
			GenerateHead(turret);
			GenerateEyes(turret);
			if (hasTail) GenerateTail(turret);
			if (hasHair) GenerateHair(turret);
			GenerateDetails(turret);
		}

		private void GenerateLimbs(Part basePlate)
		{
			float radius = bodyWidth * 0.6f;
			int limbHeight = bodyType == BodyType.Standing ? RandomRangeInt(6, 12) : RandomRangeInt(3, 6);

			for (int i = 0; i < limbCount; i++)
			{
				float angle = (360f / limbCount * i) * Mathf.Deg2Rad;
				int x = Mathf.RoundToInt(Mathf.Cos(angle) * radius);
				int z = Mathf.RoundToInt(Mathf.Sin(angle) * radius);

				int limbThickness = RandomRangeInt(3, 6);
				AddBox(basePlate, new Vector3(x, limbHeight / 2, z), limbThickness, limbHeight, limbThickness, 1);

				int footSize = limbThickness + 2;
				AddBox(basePlate, new Vector3(x, 0, z), footSize, 2, footSize, 2);
			}

			baseTopY = limbHeight;
		}

		private void GenerateBody(Part turret)
		{
			int bodyY = bodyType == BodyType.Standing ? bodyHeight / 2 : bodyHeight / 2;
			AddEllipsoid(turret, new Vector3(0, bodyY, 0), bodyWidth / 2, bodyHeight / 2, bodyDepth / 2, 0);
		}

		private void GenerateHead(Part turret)
		{
			int headY = bodyType == BodyType.Standing ? bodyHeight + headSize / 2 : bodyHeight;
			int headZ = bodyType == BodyType.Standing ? 0 : bodyDepth / 2 + headSize / 3;

			AddEllipsoid(turret, new Vector3(0, headY, headZ), headSize / 2, headSize / 2, headSize / 2, 3);

			int jawSize = headSize / 2;
			AddBox(turret, new Vector3(0, headY - headSize / 3, headZ + headSize / 2), jawSize, jawSize / 2, jawSize / 2, 4);
		}

		private void GenerateEyes(Part turret)
		{
			int headY = bodyType == BodyType.Standing ? bodyHeight + headSize / 2 : bodyHeight;
			int headZ = bodyType == BodyType.Standing ? 0 : bodyDepth / 2 + headSize / 3;
			int eyeSize = RandomRangeInt(2, 4);

			if (eyeCount == 1)
			{
				AddBox(turret, new Vector3(0, headY, headZ + headSize / 2), eyeSize, eyeSize, eyeSize, 7, 0, 2f);
			}
			else
			{
				int spacing = headSize / 3;
				for (int i = 0; i < eyeCount; i++)
				{
					float offsetX = (i - (eyeCount - 1) / 2f) * spacing;
					AddBox(turret, new Vector3(offsetX, headY + (i % 2) * 2, headZ + headSize / 2), eyeSize, eyeSize, eyeSize, 7, 0, 2f);
				}
			}
		}

		private void GenerateTail(Part turret)
		{
			int tailLength = RandomRangeInt(15, 30);
			int tailThickness = RandomRangeInt(3, 6);
			int tailStartZ = -bodyDepth / 2 - 2;

			for (int i = 0; i < tailLength; i++)
			{
				float taper = 1f - (i / (float)tailLength) * 0.7f;
				int currentThickness = Mathf.Max(2, Mathf.RoundToInt(tailThickness * taper));

				float wave = Mathf.Sin(i * 0.3f) * 2;
				int yOffset = bodyHeight / 3 - i / 5;

				AddBox(turret, new Vector3(wave, yOffset, tailStartZ - i), currentThickness, currentThickness, 1, 5);
			}
		}

		private void GenerateHair(Part turret)
		{
			int headY = bodyType == BodyType.Standing ? bodyHeight + headSize / 2 : bodyHeight;
			int headZ = bodyType == BodyType.Standing ? 0 : bodyDepth / 2 + headSize / 3;

			int hairCount = RandomRangeInt(5, 12);
			int hairHeight = RandomRangeInt(4, 10);

			for (int i = 0; i < hairCount; i++)
			{
				float angle = (360f / hairCount * i) * Mathf.Deg2Rad;
				int x = Mathf.RoundToInt(Mathf.Cos(angle) * headSize * 0.4f);
				int z = Mathf.RoundToInt(Mathf.Sin(angle) * headSize * 0.4f);

				for (int h = 0; h < hairHeight; h++)
				{
					float bend = Mathf.Sin(h * 0.5f) * 2;
					turret.voxels.Add(new VoxelData
					{
						position = new Vector3(x + bend, headY + headSize / 2 + h, headZ + z),
						size = Vector3.one,
						colorIndex = 6
					});
				}
			}
		}

		private void GenerateDetails(Part turret)
		{
			int detailCount = RandomRangeInt(3, 8);

			for (int i = 0; i < detailCount; i++)
			{
				int x = RandomRangeInt(-bodyWidth / 2, bodyWidth / 2);
				int y = RandomRangeInt(bodyHeight / 4, bodyHeight * 3 / 4);
				int z = RandomRangeInt(-bodyDepth / 2, bodyDepth / 2);

				int detailSize = RandomRangeInt(2, 5);
				bool isSpike = Random.value > 0.5f;

				if (isSpike)
				{
					for (int s = 0; s < detailSize; s++)
					{
						turret.voxels.Add(new VoxelData
						{
							position = new Vector3(x, y + s, z),
							size = Vector3.one * Mathf.Max(0.5f, 1f - s * 0.2f),
							colorIndex = 2
						});
					}
				}
				else
				{
					AddBox(turret, new Vector3(x, y, z), detailSize, detailSize, detailSize, 2, 1, 1.5f);
				}
			}
		}
	}
}