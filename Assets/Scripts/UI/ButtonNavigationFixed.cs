using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Navigation;

namespace TD.UI
{
	public class ButtonNavigationFixed : Button
	{
		// Selection logic

		/// <summary>
		/// Finds the selectable object next to this one.
		/// </summary>
		/// <remarks>
		/// The direction is determined by a Vector3 variable.
		/// </remarks>
		/// <param name="dir">The direction in which to search for a neighbouring Selectable object.</param>
		/// <returns>The neighbouring Selectable object. Null if none found.</returns>
		/// <example>
		/// <code>
		/// <![CDATA[
		/// using UnityEngine;
		/// using System.Collections;
		/// using UnityEngine.UI; // required when using UI elements in scripts
		///
		/// public class ExampleClass : MonoBehaviour
		/// {
		///     //Sets the direction as "Up" (Y is in positive).
		///     public Vector3 direction = new Vector3(0, 1, 0);
		///     public Button btnMain;
		///
		///     public void Start()
		///     {
		///         //Finds and assigns the selectable above the main button
		///         Selectable newSelectable = btnMain.FindSelectable(direction);
		///
		///         Debug.Log(newSelectable.name);
		///     }
		/// }
		/// ]]>
		///</code>
		/// </example>
		public Selectable FindSelectableByWorldPosition(Vector3 dir)
		{
			dir = dir.normalized;

			Vector3 pos = transform.position; // центр текущего Selectable
			float maxScore = Mathf.NegativeInfinity;
			float maxFurthestScore = Mathf.NegativeInfinity;
			float score = 0;

			bool wantsWrapAround = navigation.wrapAround && (navigation.mode == Mode.Vertical || navigation.mode == Mode.Horizontal);

			Selectable bestPick = null;
			Selectable bestFurthestPick = null;

			for (int i = 0; i < allSelectablesArray.Length; ++i)
			{
				Selectable sel = allSelectablesArray[i];

				if (sel == this)
					continue;

				if (!sel.IsInteractable() || sel.navigation.mode == Mode.None)
					continue;

#if UNITY_EDITOR
				if (Camera.current != null && !UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(sel.gameObject, Camera.current))
					continue;
#endif

				Vector3 selPos = sel.transform.position;
				Vector3 myVector = selPos - pos;

				float dot = Vector3.Dot(dir, myVector);

				if (wantsWrapAround && dot < 0)
				{
					score = -dot * myVector.sqrMagnitude;

					if (score > maxFurthestScore)
					{
						maxFurthestScore = score;
						bestFurthestPick = sel;
					}

					continue;
				}

				if (dot <= 0)
					continue;

				// Приоритет ближних и более «в направлении» элементов
				score = dot / myVector.sqrMagnitude;

				if (score > maxScore)
				{
					maxScore = score;
					bestPick = sel;
				}
			}

			if (wantsWrapAround && bestPick == null)
				return bestFurthestPick;

			return bestPick;
		}

		public override Selectable FindSelectableOnLeft()
		{
			if (navigation.mode == Mode.Explicit)
			{
				return navigation.selectOnLeft;
			}

			if ((navigation.mode & Mode.Horizontal) != 0)
			{
				return FindSelectableByWorldPosition(transform.rotation * Vector3.left);
			}

			return null;
		}

		public override Selectable FindSelectableOnRight()
		{
			if (navigation.mode == Mode.Explicit)
			{
				return navigation.selectOnRight;
			}

			if ((navigation.mode & Mode.Horizontal) != 0)
			{
				return FindSelectableByWorldPosition(transform.rotation * Vector3.right);
			}

			return null;
		}

		public override Selectable FindSelectableOnUp()
		{
			if (navigation.mode == Mode.Explicit)
			{
				return navigation.selectOnUp;
			}

			if ((navigation.mode & Mode.Vertical) != 0)
			{
				return FindSelectableByWorldPosition(transform.rotation * Vector3.up);
			}

			return null;
		}

		public override Selectable FindSelectableOnDown()
		{
			if (navigation.mode == Mode.Explicit)
			{
				return navigation.selectOnDown;
			}

			if ((navigation.mode & Mode.Vertical) != 0)
			{
				return FindSelectableByWorldPosition(transform.rotation * Vector3.down);
			}

			return null;
		}
	}
}