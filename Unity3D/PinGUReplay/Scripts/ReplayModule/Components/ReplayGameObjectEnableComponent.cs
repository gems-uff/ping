using System.IO;
using PinGUReplay.ReplayModule.Core;

namespace PinGUReplay.ReplayModule.Components
{
    public class ReplayGameObjectEnableComponent : ReplayComponentBase
    {
        private bool _objectOriginalState;
        
        public override void SetupToReplay()
        {
            _objectOriginalState = gameObject.activeSelf;
            gameObject.SetActive(false);
        }

        public override void OnExitReplay()
        {
            gameObject.SetActive(_objectOriginalState);
        }

        public override void SaveState(BinaryWriter writer)
        {
            var state = gameObject.activeSelf;
            writer.Write(state);
        }
        
        public override void LoadState(BinaryReader reader)
        {
            bool isActive = reader.ReadBoolean();
            gameObject.SetActive(isActive);
        }
    }
}