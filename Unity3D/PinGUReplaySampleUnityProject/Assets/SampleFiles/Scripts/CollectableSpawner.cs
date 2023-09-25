using UnityEngine;
public class CollectableSpawner : MonoBehaviour
{
    [SerializeField] private Vector3[] _spawnPositions;
    [SerializeField] private GameObject _collectablePrefab;
    private int _lastSpawnIndex = -1;

    public void SpawnCollectableOnRandomPosition()
    {
        int randomIndex = Random.Range(0, _spawnPositions.Length);

        if (randomIndex == _lastSpawnIndex)
        {
            randomIndex++;
            if (randomIndex >= _spawnPositions.Length)
                randomIndex = 0;
        }
        
        Instantiate(_collectablePrefab, _spawnPositions[randomIndex], Quaternion.identity);
        _lastSpawnIndex = randomIndex;
    }
}
