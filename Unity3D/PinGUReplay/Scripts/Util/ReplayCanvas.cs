using System;
using PinGUReplay.ReplayModule.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PinGUReplay.Util
{
    public class ReplayCanvas : MonoBehaviour
    {
        [SerializeField] private bool _allowRecording;
        [SerializeField] private TextMeshProUGUI _replayStateText;
        [SerializeField] private Button _recordBtn;
        [SerializeField] private Button _stopRecordBtn;
        [SerializeField] private Button _playBtn;
        [SerializeField] private Button _pauseBtn;
        [SerializeField] private Button _stopBtn;
        [SerializeField] private Slider _replaySlider;
        [SerializeField] private TextMeshProUGUI _replaySliderText;

        private void Awake()
        {
            _replaySlider.minValue = 0;
            _replaySlider.maxValue = 0;
            UpdateLayout();
            ReplayController.Instance.OnChangeReplayState.AddListener(OnChangeReplayState);
            ReplayController.Instance.OnChangeReplayTickOrTime.AddListener(OnChangeReplayTickOrTime);
        }

        private void OnChangeReplayState(ReplaySystemState arg0)
        {
            UpdateLayout();
        }
        
        private void OnChangeReplayTickOrTime()
        {
            _replaySlider.SetValueWithoutNotify(ReplayController.Instance.GetCurrentReplayTick());
            UpdateLayout();
        }
        
        private void Update()
        {
            if(!ReplayController.Initialized)
                return;
            
            if(ReplayController.Instance.ReplaySystemState != ReplaySystemState.ReplayPlaying)
                return;

            _replaySlider.SetValueWithoutNotify(ReplayController.Instance.GetCurrentReplayTick());
            _replaySliderText.SetText($"{ReplayController.Instance.GetCurrentReplayTick()}/{ReplayController.Instance.GetLastReplayTick()}");
        }


        #region Update Layouts
        private void UpdateLayout()
        {
            if(!ReplayController.Initialized)
                return;
            
            switch (ReplayController.Instance.ReplaySystemState)
            {
                case ReplaySystemState.Stopped:
                    StoppedLayout();
                    break;
                case ReplaySystemState.Recording:
                    RecordingLayout();
                    break;
                case ReplaySystemState.ReplayPlaying:
                    PlayingLayout();
                    break;
                case ReplaySystemState.ReplayPaused:
                    PausedLayout();
                    break;
                case ReplaySystemState.ReplayFinished:
                    PausedLayout();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void StoppedLayout()
        {
            _replayStateText.SetText(ReplayController.Instance.ReplaySystemState.ToString());
            _recordBtn.interactable = _allowRecording;
            _stopRecordBtn.interactable = false;
            _playBtn.interactable = true;
            _pauseBtn.interactable = false;
            _stopBtn.interactable = false;
            _replaySlider.interactable = false;
            _replaySliderText.SetText("");
            _replaySlider.minValue = 0;
            _replaySlider.maxValue = 0;
        }
        
        private void RecordingLayout()
        {
            _replayStateText.SetText(ReplayController.Instance.ReplaySystemState.ToString());
            _recordBtn.interactable = false;
            _stopRecordBtn.interactable = true;
            _playBtn.interactable = false;
            _pauseBtn.interactable = false;
            _stopBtn.interactable = false;
            _replaySlider.interactable = false;
            _replaySliderText.SetText("");
        }
        
        private void PlayingLayout()
        {
            _replayStateText.SetText(ReplayController.Instance.ReplaySystemState.ToString());
            _recordBtn.interactable = false;
            _stopRecordBtn.interactable = false;
            _playBtn.interactable = false;
            _pauseBtn.interactable = true;
            _stopBtn.interactable = true;
            _replaySlider.interactable = true;
            _replaySliderText.SetText($"{ReplayController.Instance.GetCurrentReplayTick()}/{ReplayController.Instance.GetLastReplayTick()}");
            if (_replaySlider.maxValue == 0)
            {
                _replaySlider.minValue = 0;
                _replaySlider.maxValue = ReplayController.Instance.GetLastReplayTick();
            }
        }
        
        private void PausedLayout()
        {
            _replayStateText.SetText(ReplayController.Instance.ReplaySystemState.ToString());
            _recordBtn.interactable = false;
            _stopRecordBtn.interactable = false;
            _playBtn.interactable = true;
            _pauseBtn.interactable = false;
            _stopBtn.interactable = true;
            _replaySlider.interactable = true;
            _replaySliderText.SetText($"{ReplayController.Instance.GetCurrentReplayTick()}/{ReplayController.Instance.GetLastReplayTick()}");
        }
        #endregion

        #region UI Buttons and Slider Methods
        public void OnClickRecord()
        {
            ReplayController.Instance.StartRecording();
            UpdateLayout();
        }

        public void OnClickStopRecord()
        {
            ReplayController.Instance.StopRecording();
            ReplayController.Instance.SaveReplayData();
            UpdateLayout();
        }


        public void OnClickPlay()
        {
            if(ReplayController.Instance.ReplaySystemState == ReplaySystemState.ReplayPaused)
                ReplayController.Instance.ResumeReplay();
            else
            {
                ReplayController.Instance.LoadReplayData();
                ReplayController.Instance.StartReplay();
            }
            
            UpdateLayout();
        }

        public void OnClickPause()
        {
            ReplayController.Instance.PauseReplay();
            UpdateLayout();
        }
        

        public void OnClickStop()
        {
            ReplayController.Instance.StopReplay();
            UpdateLayout();
        }

        public void OnChangeToTick(Single tick)
        {
            ReplayController.Instance.PauseReplay();
            ReplayController.Instance.PutReplayAtTick((int)tick);
        }
        #endregion
        
    }
}
