using System.Windows;
using Microsoft.Win32;

namespace LauncherSPT.Settings
{
    public partial class SettingsWindow : Window
    {
        public AppConfig Config { get; private set; }

        public SettingsWindow(AppConfig currentConfig)
        {
            InitializeComponent();
            Config = new AppConfig
            {
                ServerExePath = currentConfig.ServerExePath,
                GameExePath = currentConfig.GameExePath,
                ServerIp = currentConfig.ServerIp,
                ServerPort = currentConfig.ServerPort,
                BackgroundImagePath = currentConfig.BackgroundImagePath,
                TitleTopText = currentConfig.TitleTopText,
                TitleMainText = currentConfig.TitleMainText,
                LastProfileId = currentConfig.LastProfileId
            };

            ServerPathBox.Text = Config.ServerExePath;
            GamePathBox.Text = Config.GameExePath;
            PortBox.Text = Config.ServerPort.ToString();
            BackgroundPathBox.Text = Config.BackgroundImagePath;
            TitleTopBox.Text = Config.TitleTopText;
            TitleMainBox.Text = Config.TitleMainText;
        }

        private void BrowseServer_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Seleciona o SPT.Server.exe",
                Filter = "Executável (*.exe)|*.exe"
            };
            if (dlg.ShowDialog() == true)
                ServerPathBox.Text = dlg.FileName;
        }

        private void BrowseGame_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Seleciona o EscapeFromTarkov.exe (opcional)",
                Filter = "Executável (*.exe)|*.exe"
            };
            if (dlg.ShowDialog() == true)
                GamePathBox.Text = dlg.FileName;
        }

        private void BrowseBackground_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Seleciona a imagem de fundo",
                Filter = "Imagens (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (dlg.ShowDialog() == true)
                BackgroundPathBox.Text = dlg.FileName;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Config.ServerExePath = ServerPathBox.Text.Trim();
            Config.GameExePath = GamePathBox.Text.Trim();
            Config.BackgroundImagePath = BackgroundPathBox.Text.Trim();
            Config.TitleTopText = TitleTopBox.Text.Trim();
            Config.TitleMainText = TitleMainBox.Text.Trim();

            if (int.TryParse(PortBox.Text.Trim(), out var port) && port > 0)
                Config.ServerPort = port;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
