using System.IO;
using PinGUReplay.ReplayModule.Core;
using UnityEngine;

namespace PinGUReplay.ReplayModule.Components
{
    public class ReplayTransformComponent : ReplayComponentBase
    {
        [SerializeField] private bool _savePosition = true;
        [SerializeField] private bool _localPosition;
        [SerializeField] private bool _saveRotation;
        [SerializeField] private bool _localRotation;
        [SerializeField] private bool _saveScale;

        private Collider[] _colliders;
        private bool[] _collidersOriginalState;
        private Rigidbody[] _rigidbodies;
        private bool[] _rigidBodiesOriginalKinematics;
        private Rigidbody2D[] _rigidbodies2d;
        private bool[] _rigidBodies2dOriginalKinematics;

        public override void SetupToReplay()
        {
            _colliders = gameObject.GetComponentsInChildren<Collider>();
            _collidersOriginalState = new bool[_colliders.Length];

            for (int i = 0; i < _colliders.Length; i++)
            {
                _collidersOriginalState[i] = _colliders[i].enabled;
                _colliders[i].enabled = false;
            }
            
            _rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
            _rigidBodiesOriginalKinematics = new bool[_rigidbodies.Length];
            
            for (int i = 0; i < _rigidbodies.Length; i++)
            {
                _rigidBodiesOriginalKinematics[i] = _rigidbodies[i].isKinematic;
                _rigidbodies[i].isKinematic = true;
            }
            
            _rigidbodies2d = gameObject.GetComponentsInChildren<Rigidbody2D>();
            _rigidBodies2dOriginalKinematics = new bool[_rigidbodies2d.Length];
            
            for (int i = 0; i < _rigidbodies2d.Length; i++)
            {
                _rigidBodies2dOriginalKinematics[i] = _rigidbodies2d[i].isKinematic;
                _rigidbodies2d[i].isKinematic = true;
            }
        }

        public override void OnExitReplay()
        {
            for (int i = 0; i < _colliders.Length; i++)
                _colliders[i].enabled = _collidersOriginalState[i];
            
            for (int i = 0; i < _rigidbodies.Length; i++)
                _rigidbodies[i].isKinematic = _rigidBodiesOriginalKinematics[i];

            for (int i = 0; i < _rigidbodies2d.Length; i++)
                _rigidbodies2d[i].isKinematic = _rigidBodies2dOriginalKinematics[i];
        }

        public override void SaveState(BinaryWriter writer)
        {
            if (_savePosition)
                SaveTransformPosition(writer);
            
            if (_saveRotation)
                SaveTransformRotation(writer);
            
            if (_saveScale)
                SaveTransformScale(writer);
        }
        
        private void SaveTransformPosition(BinaryWriter writer)
        {
            var position = _localPosition ? transform.localPosition : transform.position;

            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);
        }

        private void SaveTransformRotation(BinaryWriter writer)
        {
            var rotation = _localPosition ? transform.localRotation : transform.rotation;

            writer.Write(rotation.eulerAngles.x);
            writer.Write(rotation.eulerAngles.y);
            writer.Write(rotation.eulerAngles.z);
        }
        
        private void SaveTransformScale(BinaryWriter writer)
        {
            var scale = transform.localScale;

            writer.Write(scale.x);
            writer.Write(scale.y);
            writer.Write(scale.z);
        }
        
        public override void LoadState(BinaryReader reader)
        {
            if (_savePosition)
                LoadTransformPosition(reader);
            
            if (_saveRotation)
                LoadTransformRotation(reader);
            
            if (_saveScale)
                LoadTransformScale(reader);
        }
        
        private void LoadTransformPosition(BinaryReader reader)
        {
            var position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            
            if (_localPosition)
                transform.localPosition = position;
            else
                transform.position = position;
        }

        private void LoadTransformRotation(BinaryReader reader)
        {
            var rotation = Quaternion.Euler(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            
            if (_localRotation)
                transform.localRotation = rotation;
            else
                transform.rotation = rotation;
        }
        
        private void LoadTransformScale(BinaryReader reader)
        {
            var scale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            transform.localScale = scale;
        }
    }
}