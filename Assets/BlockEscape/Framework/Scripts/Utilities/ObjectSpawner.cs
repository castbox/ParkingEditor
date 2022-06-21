using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG
{
	public class ObjectSpawner : UIMonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] protected SpawnObject	spawnObjectPrefab;
		[SerializeField] protected float		spawnRate;

		#endregion

		#region Member Variables

		protected GameObjectPool	spawnObjectPool;
		protected float			timer;

		#endregion

		#region Unity Methods

		protected virtual void Start()
		{
			spawnObjectPool = new GameObjectPool(spawnObjectPrefab.gameObject, 0, transform, GameObjectPool.PoolBehaviour.CanvasGroup);
		}

		protected virtual void Update()
		{
			timer -= Time.deltaTime;

			if (timer <= 0)
			{
				SpawnObject();

				timer = spawnRate;
			}
		}

		#endregion

		#region Protected Methods

		protected virtual void SpawnObject()
		{
			spawnObjectPool.GetObject<SpawnObject>().Spawned();
		}

		#endregion
	}
}
