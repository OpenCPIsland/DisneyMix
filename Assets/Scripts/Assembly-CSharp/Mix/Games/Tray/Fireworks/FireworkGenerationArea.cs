using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	[RequireComponent(typeof(PolygonCollider2D))]
	public class FireworkGenerationArea : MonoBehaviour
	{
		public FireworkTapPlane fireworkPlane;

		private Camera mGameCamera;

		private PolygonCollider2D mPolygonCollider;

		private int mMaxSearchIterations = 10;

		private void Start()
		{
			mPolygonCollider = GetComponent<PolygonCollider2D>();
			mGameCamera = Toolbox.Instance.mFireworkGame.GameController.MixGameCamera;
		}

		public Vector3 GetRandomFireworkLocation()
		{
			if (mPolygonCollider == null)
			{
				return Vector3.zero;
			}
			Vector2 point = Vector2.zero;
			float x = mPolygonCollider.bounds.min.x;
			float y = mPolygonCollider.bounds.min.y;
			float x2 = mPolygonCollider.bounds.max.x;
			float y2 = mPolygonCollider.bounds.max.y;
			for (int i = 0; i < mMaxSearchIterations; i++)
			{
				point = new Vector2(Random.Range(x, x2), Random.Range(y, y2));
				if (mPolygonCollider.OverlapPoint(point))
				{
					break;
				}
			}
			Vector3 pointProjectedOntoTapPlane = fireworkPlane.GetPointProjectedOntoTapPlane(new Vector3(point.x, point.y, base.transform.position.z), mGameCamera);
			Debug.DrawLine(mGameCamera.transform.position, pointProjectedOntoTapPlane, Color.cyan, 30f);
			return pointProjectedOntoTapPlane;
		}
	}
}
