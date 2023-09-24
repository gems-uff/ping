using System.Collections;
using PinGUReplay.ReplayModule.Core;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private static GameController _instance;
    
    [SerializeField] private int _collectableToFinishGame = 5;
    private CollectableSpawner _collectableSpawner;
    private int _currentCollectableCount;

    private void Awake()
    {
        _instance = this;
        _collectableSpawner = FindObjectOfType<CollectableSpawner>();
    }

    private void Start()
    {
        ReplayController.Instance.StartRecording();
        _currentCollectableCount = 0;
        _collectableSpawner.SpawnCollectableOnRandomPosition();
    }

    public static void OnGetCollectable()
    {
        if(_instance == null)
            return;

        _instance.ProcessGetCollectable();
    }

    private void ProcessGetCollectable()
    {
        _currentCollectableCount += 1;

        if (_currentCollectableCount < _collectableToFinishGame)
        {
            _collectableSpawner.SpawnCollectableOnRandomPosition();
            return;
        }
        
        FinishGame();
    }

    private void FinishGame()
    {
        StartCoroutine(nameof(WaitEndOfFrameAndFinish));
    }

    private IEnumerator WaitEndOfFrameAndFinish()
    {
        yield return new WaitForEndOfFrame();
        ReplayController.Instance.StopRecording();
        ReplayController.Instance.SaveReplayData();
        ProvCaptureController.instance.ExportProv();
        yield return new WaitForEndOfFrame();
        Application.Quit();
        #if UNITY_EDITOR
            Debug.Break();
        #endif
    }
}
