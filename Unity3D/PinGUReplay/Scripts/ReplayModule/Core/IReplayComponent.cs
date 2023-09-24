using System.IO;

namespace PinGUReplay.ReplayModule.Core
{
    public interface IReplayComponent
    {
        void SetupToReplay();
        void OnExitReplay();
        void SaveState(BinaryWriter writer);
        void LoadState(BinaryReader reader);
    }
}