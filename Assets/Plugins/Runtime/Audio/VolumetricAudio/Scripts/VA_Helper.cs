using UnityEngine;

public static partial class VA_Helper
{
	public static int MeshVertexLimit = 65000;

	private static AudioListener cachedAudioListener;

	public static bool GetListenerPosition(ref Vector3 position)
	{
		if (Enabled(cachedAudioListener) == false)
		{
			// LastLevel changes start.
			AudioListener[] als = Object.FindObjectsOfType<AudioListener>();

			for (int i = 0; i < als.Length; ++i)
			{
				if (als[i].enabled)
				{
					cachedAudioListener = als[i];
					break;
				}
			}
			// LastLevel changes end.
		}

		if (cachedAudioListener != null)
		{
			position = cachedAudioListener.transform.position; return true;
		}

		return false;
	}

	// Return the current camera, or the main camera
	public static Camera GetCamera(Camera camera = null)
	{
		if (camera == null || camera.isActiveAndEnabled == false)
		{
			camera = Camera.main;
		}

		return camera;
	}

	public static Vector2 SinCos(float a)
	{
		return new Vector2(Mathf.Sin(a), Mathf.Cos(a));
	}

	public static void Destroy(Object o)
	{
#if UNITY_EDITOR
		if (Application.isPlaying == false)
		{
			Object.DestroyImmediate(o, true); return;
		}
#endif
		Object.Destroy(o);
	}

	public static bool Enabled(Behaviour b)
	{
		return b != null && b.enabled == true && b.gameObject.activeInHierarchy == true;
	}

	public static float Divide(float a, float b)
	{
		return Zero(b) == false ? a / b : 0.0f;
	}

	public static float Reciprocal(float v)
	{
		return Zero(v) == false ? 1.0f / v : 0.0f;
	}

	public static bool Zero(float v)
	{
		return Mathf.Approximately(v, 0.0f);
	}

	public static Matrix4x4 RotationMatrix(Quaternion q)
	{
		return Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
	}

	public static Matrix4x4 TranslationMatrix(Vector3 xyz)
	{
		return TranslationMatrix(xyz.x, xyz.y, xyz.z);
	}

	public static Matrix4x4 TranslationMatrix(float x, float y, float z)
	{
		var matrix = Matrix4x4.identity;

		matrix.m03 = x;
		matrix.m13 = y;
		matrix.m23 = z;

		return matrix;
	}

	public static Matrix4x4 ScalingMatrix(float xyz)
	{
		return ScalingMatrix(xyz, xyz, xyz);
	}

	public static Matrix4x4 ScalingMatrix(Vector3 xyz)
	{
		return ScalingMatrix(xyz.x, xyz.y, xyz.z);
	}

	public static Matrix4x4 ScalingMatrix(float x, float y, float z)
	{
		var matrix = Matrix4x4.identity;

		matrix.m00 = x;
		matrix.m11 = y;
		matrix.m22 = z;

		return matrix;
	}

	public static float DampenFactor(float dampening, float elapsed)
	{
#if UNITY_EDITOR
		if (Application.isPlaying == false)
		{
			return 1.0f;
		}
#endif
		return 1.0f - Mathf.Pow((float)System.Math.E, - dampening * elapsed);
	}

	public static Quaternion Dampen(Quaternion current, Quaternion target, float dampening, float elapsed, float minStep = 0.0f)
	{
		var factor   = DampenFactor(dampening, elapsed);
		var maxDelta = Quaternion.Angle(current, target) * factor + minStep * elapsed;

		return MoveTowards(current, target, maxDelta);
	}

	public static float Dampen(float current, float target, float dampening, float elapsed, float minStep = 0.0f)
	{
		var factor   = DampenFactor(dampening, elapsed);
		var maxDelta = Mathf.Abs(target - current) * factor + minStep * elapsed;

		return MoveTowards(current, target, maxDelta);
	}

	public static Vector3 Dampen3(Vector3 current, Vector3 target, float dampening, float elapsed, float minStep = 0.0f)
	{
		var factor   = DampenFactor(dampening, elapsed);
		var maxDelta = Mathf.Abs((target - current).magnitude) * factor + minStep * elapsed;

		return Vector3.MoveTowards(current, target, maxDelta);
	}

	public static Quaternion MoveTowards(Quaternion current, Quaternion target, float maxDelta)
	{
		var delta = Quaternion.Angle(current, target);

		return Quaternion.Slerp(current, target, Divide(maxDelta, delta));
	}

	public static float MoveTowards(float current, float target, float maxDelta)
	{
		if (target > current)
		{
			current = System.Math.Min(target, current + maxDelta);
		}
		else
		{
			current = System.Math.Max(target, current - maxDelta);
		}

		return current;
	}

	public static Vector3 ClosestPointToLineSegment(Vector3 a, Vector3 b, Vector3 point)
	{
		var l = (b - a).magnitude;
		var d = (b - a).normalized;

		return a + Mathf.Clamp(Vector3.Dot(point - a, d), 0.0f, l) * d;
	}

	public static bool PointLeftOfLine(Vector2 a, Vector2 b, Vector2 p) // NOTE: CCW
	{
		return ((b.x - a.x) * (p.y - a.y) - (p.x - a.x) * (b.y - a.y)) >= 0.0f;
	}

	public static bool PointLeftOfLine(float ax, float ay, float bx, float by, float px, float py) // NOTE: CCW
	{
		return ((bx - ax) * (py - ay) - (px - ax) * (by - ay)) >= 0.0f;
	}

	public static bool PointRightOfLine(Vector2 a, Vector2 b, Vector2 p) // NOTE: CCW
	{
		return ((b.x - a.x) * (p.y - a.y) - (p.x - a.x) * (b.y - a.y)) <= 0.0f;
	}

	public static Vector2 VectorXY(Vector3 xyz)
	{
		return new Vector2(xyz.x, xyz.y);
	}
}