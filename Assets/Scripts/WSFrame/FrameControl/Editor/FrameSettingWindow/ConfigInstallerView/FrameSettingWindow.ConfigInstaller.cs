using UnityEngine.UIElements;

namespace WS_Modules
{
    public partial class FrameSettingWindow
    {
        private ConfigInstallerView configInstallerView;

        private void DrawConfigInstallerSettings(VisualElement container)
        {
            ConfigInstallerViewModel viewModel = new ConfigInstallerViewModel();
            configInstallerView = new ConfigInstallerView(container, viewModel);
            configInstallerView.Bind();
        }
    }
}
