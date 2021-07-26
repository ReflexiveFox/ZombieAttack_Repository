﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ZombieAttack
{
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField] List<Transform> spawnPoints = null;
        [SerializeField] Transform finalObjectiveTransform = null;
        [SerializeField] Wave[] waves = null;
        public int currentWave = 0;
        int killedEnemies = 0;
        int currentMaxEnemies = 0;
        int[] spawnedEnemies;

        public static EnemyManager instance;

        private void Awake()
        {
            instance = this;
            finalObjectiveTransform = GameObject.FindGameObjectWithTag("Finish").transform;
        }

        private void Start()
        {
            for(int i = 0; i < transform.childCount; i++)
                spawnPoints.Add(transform.GetChild(i));
            
            currentWave = 0;
            foreach (Wave wave in waves)
                wave.InitializeEnemyTypesIndexList();
            spawnedEnemies = new int[waves[0].maxEnemyTypes.Length];
        }

        public void SpawnWave()
        {
            if (waves.Length > 0)
            {
                if (currentWave >= 0 && currentWave < waves.Length)
                { 
                    InvokeRepeating(nameof(SpawnEnemy), 0f, waves[currentWave].timeBetweenSpawns);
                }
            }
        }

        public void SpawnEnemy()
        {
            //Choose enemy type to spawn
            int enemyTypeIndex = waves[currentWave].SelectEnemyType();

            //If the list of callable type of enemies is not empty
            if (enemyTypeIndex != -1)
            {
                //If spawned enemies haven't reached the maximum number allowed
                if (spawnedEnemies[enemyTypeIndex] < waves[currentWave].maxEnemyTypes[enemyTypeIndex])
                {
                    GameObject enemy; 
                    switch(enemyTypeIndex)
                    {
                        case 0:
                            enemy = ObjectPooler.SharedInstance.GetPooledObject("Enemy", "EnemySmall");
                            break;

                        case 1:
                            enemy = ObjectPooler.SharedInstance.GetPooledObject("Enemy", "EnemyMedium");
                            break;

                        case 2:
                            enemy = ObjectPooler.SharedInstance.GetPooledObject("Enemy", "EnemyBig");
                            break;

                        default:
                            Debug.LogWarning("Indice del tipo di nemico non riconosciuto!");
                            enemy = null;
                            break;
                    }
                    SetupEnemy(enemy);
                    enemy.SetActive(true);
                    //Increase enemy counting for this type
                    spawnedEnemies[enemyTypeIndex]++;
                }
                //else remove the index from the list and restart the method
                else
                {
                    waves[currentWave].DiscardEnemyType(enemyTypeIndex);
                    SpawnEnemy();
                }
            }
            else
            {
                CancelInvoke(nameof(SpawnEnemy));
                currentWave++;
            }
        }

        private void SetupEnemy(GameObject enemy)
        {
            enemy.GetComponent<Rigidbody>().velocity = Vector3.zero;
            //Choose spawnpoint
            enemy.transform.position = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
            enemy.GetComponent<EnemyMovement>().SetDestination(finalObjectiveTransform);          
            enemy.GetComponent<Health>().OnEnemyDead += IncreaseKillCount;
        }

        private void IncreaseKillCount(Health enemyHealth)
        {
            killedEnemies++;
            enemyHealth.OnEnemyDead -= IncreaseKillCount;
            if (killedEnemies >= currentMaxEnemies)
            {
                if (currentWave > waves.Length - 1)
                {
                    currentWave = 0;
                    //Vittoria del gioco
                    GameManager.instance.SetStatusGame(GameManager.GameState.Won);
                    UI_Manager.instance.SetFinishScreen(GameManager.GameState.Won);
                }
                else
                {
                    //Vittoria dell'ondata
                    GameManager.instance.SetStatusGame(GameManager.GameState.WaveWon);
                    UI_Manager.instance.SetFinishScreen(GameManager.GameState.WaveWon);
                }
            }
        }
    }
}