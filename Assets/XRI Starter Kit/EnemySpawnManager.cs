using System.Collections.Generic;
using MikeNspired.XRIStarterKit;
using NUnit.Framework;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public Transform spawnParent;
    List<Transform> spawnPointList = new List<Transform>();
    public GameObject prefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (Transform t in spawnParent)
        {
            spawnPointList.Add(t);
        }
        SpawnToPoints();
    }

    public void SpawnToPoints()
    {
         for(int i = 0; i < spawnPointList.Count; i++)
        {
            var p = Instantiate(prefab, spawnPointList[i].position, spawnPointList[i].rotation);
            //드론이 시작시 해야될것이 이후 실행
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
