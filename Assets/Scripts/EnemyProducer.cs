﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProducer : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject prefab; // prefab to be spawned. Set prefab on the editor

    private Bounds spawnArea;
    private int numOfEnemies; // max number of enemies per round
    private int spawnedEnemies = 0; // number of enemies spawned already

    // Start is called before the first frame update
    void Start()
    {
        numOfEnemies = 10;
        spawnArea = this.GetComponent<BoxCollider>().bounds;
        InvokeRepeating("spawnEnemy", 5f, 2f);
    }

    // Returns: Vector3 random coordinates inside the spawnArea
    Vector3 randomSpawnPosition() {
        float x = Random.Range(spawnArea.min.x, spawnArea.max.x);
        float z = Random.Range(spawnArea.min.z, spawnArea.max.z);
        float y = 0.5f;
        return new Vector3(x, y, z);
    }
    
    // self explanatory??
    // TODO add stop spawning based on how many enemies are already there or on a maximun number of enemies
    void spawnEnemy()
    {
        if (spawnedEnemies >= numOfEnemies)
        {
            return;
        }
        Instantiate(prefab, randomSpawnPosition(), Quaternion.identity);
        spawnedEnemies += 1;
    }
    

    // Update is called once per frame
    void Update()
    {
        
    }
}