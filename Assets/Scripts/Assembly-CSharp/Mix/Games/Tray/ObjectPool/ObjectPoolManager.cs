using System.Collections.Generic;
using UnityEngine;

namespace Mix.Games.Tray.ObjectPool
{
	public class ObjectPoolManager : MonoBehaviour
	{
		private static ObjectPoolManager mInstance;

		private Dictionary<EntityId, ObjectPool> mPools = new Dictionary<EntityId, ObjectPool>();

		private Dictionary<EntityId, EntityId> mObjToPrefabMap = new Dictionary<EntityId, EntityId>();

		public static ObjectPoolManager Instance
		{
			get
			{
				return mInstance;
			}
		}

		private void Awake()
		{
			if (mInstance == null)
			{
				mInstance = this;
			}
		}

		public static bool HasPoolForObj(GameObject aPrefab)
		{
			EntityId instanceID = aPrefab.GetEntityId();
			return Instance.mPools.ContainsKey(instanceID);
		}

		public static GameObject InstantiatePooledObj(GameObject aPrefab)
		{
			return InstantiatePooledObj(aPrefab, aPrefab.transform.position, aPrefab.transform.rotation);
		}

		public static GameObject InstantiatePooledObj(GameObject aPrefab, Vector3 aPosition, Quaternion aRotation)
		{
			EntityId instanceID = aPrefab.GetEntityId();
			ObjectPool value;
			if (Instance.mPools.TryGetValue(instanceID, out value))
			{
				return value.InstantiatePooled(aPosition, aRotation);
			}
			Debug.LogWarningFormat("Trying to Instantiate pooled object {0} before a pool has been created", aPrefab.name);
			return null;
		}

		public static void DestroyPooledObj(GameObject aObj)
		{
			if (Instance != null)
			{
				ObjectPool objectPoolFromObj = Instance.GetObjectPoolFromObj(aObj);
				if (objectPoolFromObj != null)
				{
					objectPoolFromObj.DestroyPooled(aObj);
					return;
				}
				Debug.LogWarningFormat("Destroying an unpooled object {0}", aObj.name);
				Object.Destroy(aObj);
			}
		}

		public static void CreatePool(GameObject aPrefab, int aInitSize)
		{
			EntityId instanceID = aPrefab.GetEntityId();
			ObjectPool value;
			if (!Instance.mPools.TryGetValue(instanceID, out value))
			{
				value = ObjectPool.CreateNewPool(aPrefab, aInitSize);
				Instance.mPools.Add(instanceID, value);
				value.transform.parent = Instance.transform;
			}
			else
			{
				Debug.LogWarningFormat("Trying to create Object Pool {0}, but one already exists", aPrefab.name);
			}
		}

		public static void DestroyPooled(GameObject aPrefab)
		{
			EntityId instanceID = aPrefab.GetEntityId();
			ObjectPool value;
			if (Instance.mPools.TryGetValue(instanceID, out value))
			{
				value.Clear();
				Instance.mPools.Remove(instanceID);
				Object.Destroy(value);
			}
			else
			{
				Debug.LogWarningFormat("Trying to destroy Object Pool {0}, but one does not exist", aPrefab.name);
			}
		}

		public static void ExpandPool(GameObject aPrefab, int aAddSize)
		{
			EntityId instanceID = aPrefab.GetEntityId();
			ObjectPool value;
			if (Instance.mPools.TryGetValue(instanceID, out value))
			{
				value.Expand(aAddSize);
				return;
			}
			Debug.LogWarningFormat("Could not expand pool r {0}", aPrefab.name);
		}

		public static void ClearAllPools()
		{
			foreach (ObjectPool value in Instance.mPools.Values)
			{
				value.Clear();
				Object.Destroy(value);
			}
			Instance.mPools.Clear();
			Instance.mObjToPrefabMap.Clear();
		}

		private ObjectPool GetObjectPoolFromObj(GameObject aObj)
		{
			EntityId value;
			ObjectPool value2;
			if (mObjToPrefabMap.TryGetValue(aObj.GetEntityId(), out value) && mPools.TryGetValue(value, out value2))
			{
				return value2;
			}
			return null;
		}

		public void RegisterObjectToPrefab(GameObject aObj, GameObject aPrefab)
		{
			mObjToPrefabMap.Add(aObj.GetEntityId(), aPrefab.GetEntityId());
		}

		public void UnregisterObject(GameObject aObj)
		{
			mObjToPrefabMap.Remove(aObj.GetEntityId());
		}

		public void DrawInspectorGUI()
		{
		}
	}
}
