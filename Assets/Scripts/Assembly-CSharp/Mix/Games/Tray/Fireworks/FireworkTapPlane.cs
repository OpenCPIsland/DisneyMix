using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class FireworkTapPlane : MonoBehaviour
	{
		private Ray mTouchRay;

		private float mTouchDist;

		private Plane mTapPlane;

		public void Start()
		{
			GetComponent<Renderer>().enabled = false;
			mTapPlane = CreateTapPlane();
			Debug.DrawRay(base.transform.position, mTapPlane.normal, Color.white, 20f);
		}

		public Plane CreateTapPlane()
		{
			return new Plane(base.transform.TransformDirection(Vector3.back), base.transform.position);
		}

		public Vector3 GetTouchOnTapPlane(Camera camera)
		{
			mTouchRay = Toolbox.Instance.mFireworkGame.GameController.MixGameCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero);
			if (mTapPlane.Raycast(mTouchRay, out mTouchDist))
			{
				return mTouchRay.GetPoint(mTouchDist);
			}
			return Vector3.zero;
		}

		public Vector3 GetPointProjectedOntoTapPlane(Vector3 point, Camera camera)
		{
			Ray ray = new Ray(camera.transform.position, point - camera.transform.position);
			if (mTapPlane.Raycast(ray, out mTouchDist))
			{
				return ray.GetPoint(mTouchDist);
			}
			throw new UnityException("Point cannot be projected onto tap plane!");
		}

		public Vector3 GetRightVector()
		{
			return base.transform.right;
		}

		public Vector3 GetOrigin()
		{
			return base.transform.parent.TransformPoint(base.transform.localPosition);
		}
	}
}
