using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages in an eficient way the creation and destruction of spawned objects. Keeps the elements in scene clean and organized.
/// </summary>
namespace CNB
{
    [ExecuteInEditMode]
	public class PoolManCNB : MonoBehaviour
	{
        [SerializeField]
		public static Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        
        [SerializeField]
		static PoolManCNB _instance;

		public static PoolManCNB instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindFirstObjectByType<PoolManCNB>();
				}
				return _instance;
			}
		}

        private void Reset()
        {
			DestroyAllPools();
        }

        public void CreatePool(GameObject prefab, int poolSize)
		{
			GameObject poolHolder = GameObject.Find(prefab.name);
			bool poolHolderExists = poolHolder != null;
			string poolKey = prefab.name;

            if (poolDictionary.ContainsKey(poolKey) && poolHolderExists && poolDictionary[poolKey].Count==poolSize)
            {
				return;
            }

            else if (poolDictionary.ContainsKey(poolKey) && poolHolderExists && poolDictionary[poolKey].Count < poolSize)
            {
                int differenceToAdd = poolSize - poolDictionary[poolKey].Count;
                for (int i = 0; i < differenceToAdd; i++)
                {
                    GameObject newObject = Instantiate(prefab) as GameObject;
					newObject.SetActive(false);
                    poolDictionary[poolKey].Enqueue(newObject);
                    newObject.transform.parent = poolHolder.transform;
                }
				return;
			}
			else if (poolDictionary.ContainsKey(poolKey) && poolHolderExists && poolDictionary[poolKey].Count > poolSize)
			{
                int differenceToRemove = poolDictionary[poolKey].Count - poolSize;
                for (int i = 0; i < differenceToRemove; i++)
                {
                    GameObject dequed = poolDictionary[poolKey].Dequeue();
					DestroyImmediate(dequed);
                }
				return;
            }

            if (poolHolder==null)
            {
				poolHolder = new GameObject(prefab.name);
				poolHolder.transform.parent = transform;
			}

            if (poolDictionary.ContainsKey(poolKey))
            {
				if (poolDictionary[poolKey].Count == 0)
				{
					for (int i = 0; i < poolSize; i++)
					{
						GameObject newObject = Instantiate(prefab) as GameObject;
						newObject.SetActive(false);
						poolDictionary[poolKey].Enqueue(newObject);
						newObject.transform.parent = poolHolder.transform;
					}
                }
                else if (poolDictionary[poolKey].Count<poolSize)
                {
					int differenceToAdd = poolSize - poolDictionary[poolKey].Count;
					for (int i = 0; i < differenceToAdd; i++)
					{
						GameObject newObject = Instantiate(prefab) as GameObject;
						newObject.SetActive(false);
						poolDictionary[poolKey].Enqueue(newObject);
						newObject.transform.parent = poolHolder.transform;
					}
                }
                else if (poolDictionary[poolKey].Count > poolSize)
				{
					int differenceToRemove = poolDictionary[poolKey].Count-poolSize;
					for (int i = 0; i < differenceToRemove; i++)
					{
						poolDictionary[poolKey].Dequeue();
					}
                }
                else
                {
					return;
                }
			}
			else
			{
				poolDictionary.Add(poolKey, new Queue<GameObject>());
				for (int i = 0; i < poolSize; i++)
				{
					GameObject newObject = Instantiate(prefab) as GameObject;
					newObject.SetActive(false);
					poolDictionary[poolKey].Enqueue(newObject);
					newObject.transform.parent = poolHolder.transform;
				}
			}
		}

		public GameObject ReuseObject(GameObject prefab, Vector3 position, Quaternion rotation)
		{
			string poolKey = prefab.name;

			if (poolDictionary.ContainsKey(poolKey))
			{
                if (poolDictionary[poolKey].Count>0)
                {
					GameObject objectToReuse = poolDictionary[poolKey].Dequeue();
					if (objectToReuse != null)
					{
						poolDictionary[poolKey].Enqueue(objectToReuse);

						objectToReuse.SetActive(true);
						objectToReuse.transform.localPosition = position;
						objectToReuse.transform.rotation = rotation;
						return objectToReuse;
					}
				}
			}
			return null;
		}

		public void DesactivatePoolGOs()
        {
			int spawnableObjTypes = this.gameObject.transform.childCount;
            for (int i = 0; i < spawnableObjTypes; i++)
            {
				Transform child = this.gameObject.transform.GetChild(i);
                for (int j = 0; j < child.childCount; j++)
                {
					Transform instancedChild = child.GetChild(j);
					instancedChild.gameObject.SetActive(false);
				}
			}            
        }

		public void FillPoolDictionary()
		{
			poolDictionary.Clear();
			if (poolDictionary.Count==0)
            {
				int spawnableObjTypes = this.gameObject.transform.childCount;
				for (int i = 0; i < spawnableObjTypes; i++)
				{
					Transform spawnableObj = this.gameObject.transform.GetChild(i);
					string instanceID = spawnableObj.gameObject.name;
					poolDictionary.Add(instanceID, new Queue<GameObject>());

					for (int j = 0; j < spawnableObj.childCount; j++)
					{
						Transform instancedChild = spawnableObj.GetChild(j);
						poolDictionary[instanceID].Enqueue(instancedChild.gameObject);
					}
				}
			}
		}
		
		void DestroyAllPools()
        {
			while (transform.childCount > 0)
			{
				DestroyImmediate(transform.GetChild(0).gameObject);
			}
        }
	}
}
