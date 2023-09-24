using System.IO;
using UnityEngine;

namespace PinGUReplay.ReplayModule.Core
{
    public abstract class ReplayComponentBase : MonoBehaviour, IReplayComponent
    {
        public abstract void SetupToReplay();
        public abstract void OnExitReplay();
        public abstract void SaveState(BinaryWriter writer);
        public abstract void LoadState(BinaryReader reader);
    }
}