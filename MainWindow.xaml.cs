using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace LauncherSPT
{
    public partial class MainWindow : Window
    {
        private const string ConfigFileName = "config.json";
        private AppConfig _config = new();
        private Process? _serverProcess;
        private Process? _gameProcess;
        private bool _editionsLoaded;

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
        }

        // ---------- Configuração ----------

        private void LoadConfig()
        {
            if (File.Exists(ConfigFileName))
            {
                try
                {
                    var json = File.ReadAllText(ConfigFileName);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch
                {
                    _config = new AppConfig();
                }
            }

            ApplyBackgroundImage();
            ApplyTitleTexts();
            RefreshProfiles();

            // Lista de edições por omissão, até conseguirmos falar com o servidor
            EditionComboBox.ItemsSource = new[] { "standard", "left_behind", "prepare_for_escape", "edge_of_darkness", "unheard_edition" };
            EditionComboBox.SelectedIndex = 0;
        }

        private void SaveConfig()
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFileName, json);
        }

        private void ApplyTitleTexts()
        {
            TopTitleText.Text = string.IsNullOrWhiteSpace(_config.TitleTopText) ? "ESCAPE FROM TARKOV" : _config.TitleTopText;
            MainTitleText.Text = string.IsNullOrWhiteSpace(_config.TitleMainText) ? "SINGLE PLAYER" : _config.TitleMainText;
        }

        private void ApplyBackgroundImage()
        {
            if (string.IsNullOrWhiteSpace(_config.BackgroundImagePath) || !File.Exists(_config.BackgroundImagePath))
            {
                BackgroundImage.Visibility = Visibility.Collapsed;
                BackgroundOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(_config.BackgroundImagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                BackgroundImage.Source = bitmap;
                BackgroundImage.Visibility = Visibility.Visible;
                BackgroundOverlay.Visibility = Visibility.Visible;
            }
            catch
            {
                BackgroundImage.Visibility = Visibility.Collapsed;
                BackgroundOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private string? GetSptFolder()
        {
            if (string.IsNullOrWhiteSpace(_config.ServerExePath))
                return null;

            return Path.GetDirectoryName(_config.ServerExePath);
        }

        // ---------- Aba JOGAR: perfis ----------

        private void RefreshProfiles()
        {
            var sptFolder = GetSptFolder();

            if (sptFolder == null || !File.Exists(_config.ServerExePath))
            {
                StatusText.Text = "Configuração em falta. Clica em ⚙ para definir o caminho do SPT.Server.exe.";
                ProfileComboBox.ItemsSource = null;
                ManageProfilesListBox.ItemsSource = null;
                PlayButton.IsEnabled = false;
                return;
            }

            var address = ProfileLoader.ReadServerAddress(sptFolder);
            if (address != null)
            {
                _config.ServerIp = address.Value.Ip;
                _config.ServerPort = address.Value.Port;
            }

            var profiles = ProfileLoader.LoadProfiles(sptFolder);
            ProfileComboBox.ItemsSource = profiles;
            ManageProfilesListBox.ItemsSource = profiles;

            if (profiles.Count == 0)
            {
                StatusText.Text = "Não encontrei nenhum perfil. Cria um na aba PERFIS.";
                PlayButton.IsEnabled = false;
                return;
            }

            var toSelect = profiles.Find(p => p.Id == _config.LastProfileId) ?? profiles[0];
            ProfileComboBox.SelectedItem = toSelect;

            PlayButton.IsEnabled = true;
            StatusText.Text = "Pronto para iniciar.";
        }

        private void RefreshProfilesButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshProfiles();
        }

        // ---------- Aba PERFIS: criar / apagar ----------

        private async void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_editionsLoaded) return;
            if (MainTabControl.SelectedItem is not TabItem item || item.Header?.ToString() != "PERFIS") return;

            _editionsLoaded = true;
            await LoadEditionsFromServerAsync();
        }

        private async Task LoadEditionsFromServerAsync()
        {
            var sptFolder = GetSptFolder();
            if (sptFolder == null) return;

            ProfilesStatusText.Text = "A ligar ao servidor para obter as edições disponíveis...";

            var ok = await EnsureServerRunningAsync();
            if (!ok)
            {
                ProfilesStatusText.Text = "Não consegui ligar ao servidor. A usar lista de edições por omissão.";
                return;
            }

            var connectInfo = await SptApiClient.ConnectAsync(_config.ServerIp, _config.ServerPort);
            if (connectInfo != null && connectInfo.Editions.Count > 0)
            {
                EditionComboBox.ItemsSource = connectInfo.Editions;
                EditionComboBox.SelectedIndex = 0;
                ProfilesStatusText.Text = "Pronto para criar ou apagar perfis.";
            }
            else
            {
                ProfilesStatusText.Text = "Não consegui obter as edições do servidor. A usar lista por omissão.";
            }
        }

        private async void CreateProfile_Click(object sender, RoutedEventArgs e)
        {
            var sptFolder = GetSptFolder();
            if (sptFolder == null)
            {
                MessageBox.Show("Define primeiro o caminho do SPT.Server.exe nas Definições (ícone ⚙).", "Configuração em falta");
                return;
            }

            var username = NewUsernameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Escreve um nome de utilizador para o novo perfil.", "Nome em falta");
                return;
            }

            var edition = EditionComboBox.SelectedItem as string ?? "standard";

            CreateProfileButton.IsEnabled = false;
            ProfilesStatusText.Text = "A iniciar servidor (se necessário) e a criar o perfil...";

            try
            {
                var ok = await EnsureServerRunningAsync();
                if (!ok)
                {
                    ProfilesStatusText.Text = "Não consegui iniciar/ligar ao servidor.";
                    return;
                }

                var newId = await SptApiClient.RegisterProfileAsync(_config.ServerIp, _config.ServerPort, username, edition);
                if (string.IsNullOrEmpty(newId))
                {
                    ProfilesStatusText.Text = $"Não foi possível criar o perfil. É provável que o nome \"{username}\" já esteja em uso.";
                    return;
                }

                ProfilesStatusText.Text = $"Perfil \"{username}\" criado com sucesso.";
                NewUsernameBox.Text = "";
                RefreshProfiles();
            }
            catch (Exception ex)
            {
                ProfilesStatusText.Text = "Erro ao criar o perfil.";
                MessageBox.Show(ex.Message, "Erro");
            }
            finally
            {
                CreateProfileButton.IsEnabled = true;
            }
        }

        private async void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            var sptFolder = GetSptFolder();
            if (sptFolder == null) return;

            if (sender is not Button button || button.Tag is not SptProfile profile)
                return;

            var confirm = MessageBox.Show(
                $"Tens a certeza que queres apagar o perfil \"{profile.Nickname}\"?\nEsta ação não pode ser desfeita.",
                "Confirmar remoção",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            button.IsEnabled = false;
            ProfilesStatusText.Text = $"A apagar \"{profile.Nickname}\"...";

            try
            {
                var ok = await EnsureServerRunningAsync();
                if (!ok)
                {
                    ProfilesStatusText.Text = "Não consegui iniciar/ligar ao servidor.";
                    return;
                }

                var removed = await SptApiClient.RemoveProfileAsync(_config.ServerIp, _config.ServerPort, profile.Id);
                if (removed)
                {
                    ProfilesStatusText.Text = $"Perfil \"{profile.Nickname}\" apagado.";
                    if (_config.LastProfileId == profile.Id)
                        _config.LastProfileId = "";
                    SaveConfig();
                    RefreshProfiles();
                }
                else
                {
                    ProfilesStatusText.Text = "O servidor não confirmou a remoção do perfil.";
                }
            }
            catch (Exception ex)
            {
                ProfilesStatusText.Text = "Erro ao apagar o perfil.";
                MessageBox.Show(ex.Message, "Erro");
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        // ---------- Botões da barra de título ----------

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Settings.SettingsWindow(_config) { Owner = this };
            if (settingsWindow.ShowDialog() == true)
            {
                _config = settingsWindow.Config;
                SaveConfig();
                ApplyBackgroundImage();
                ApplyTitleTexts();
                RefreshProfiles();
            }
        }

        // ---------- Jogar ----------

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var sptFolder = GetSptFolder();
            if (sptFolder == null || !File.Exists(_config.ServerExePath))
            {
                MessageBox.Show("Define primeiro o caminho do SPT.Server.exe nas Definições (ícone ⚙).", "Configuração em falta");
                return;
            }

            var selectedProfile = ProfileComboBox.SelectedItem as SptProfile;
            if (selectedProfile == null)
            {
                MessageBox.Show("Seleciona um perfil na lista antes de jogar.", "Nenhum perfil selecionado");
                return;
            }

            PlayButton.IsEnabled = false;

            try
            {
                var ready = await EnsureServerRunningAsync();
                if (!ready)
                {
                    StatusText.Text = "O servidor não respondeu a tempo. Verifica o caminho e a porta nas Definições.";
                    return;
                }

                StatusText.Text = $"A entrar com o perfil \"{selectedProfile.Nickname}\"...";

                _config.LastProfileId = selectedProfile.Id;
                SaveConfig();

                StartGameDirect(sptFolder, selectedProfile);

                StatusText.Text = "Bom combate.";
                PlayButton.Content = "REINICIAR JOGO";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ocorreu um erro ao iniciar.";
                MessageBox.Show(ex.Message, "Erro ao iniciar");
            }
            finally
            {
                PlayButton.IsEnabled = true;
            }
        }

        // Garante que o servidor está a correr (arranca-o escondido se necessário)
        public async Task<bool> EnsureServerRunningAsync()
        {
            if (string.IsNullOrWhiteSpace(_config.ServerExePath) || !File.Exists(_config.ServerExePath))
                return false;

            if (_serverProcess != null && !_serverProcess.HasExited)
                return await WaitForServerAsync(_config.ServerIp, _config.ServerPort, TimeSpan.FromSeconds(5));

            StartServerHidden();
            var ready = await WaitForServerAsync(_config.ServerIp, _config.ServerPort, TimeSpan.FromSeconds(40));

            if (ready)
            {
                StopButton.IsEnabled = true;
            }

            return ready;
        }

        private void StartServerHidden()
        {
            var psi = new ProcessStartInfo
            {
                FileName = _config.ServerExePath,
                WorkingDirectory = Path.GetDirectoryName(_config.ServerExePath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            _serverProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _serverProcess.Exited += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "O servidor terminou.";
                    PlayButton.Content = "JOGAR";
                    StopButton.IsEnabled = false;
                });
            };
            _serverProcess.Start();
        }

        // Arranca o EscapeFromTarkov.exe diretamente com o token do perfil selecionado
        private void StartGameDirect(string sptFolder, SptProfile profile)
        {
            var gameExe = !string.IsNullOrWhiteSpace(_config.GameExePath) && File.Exists(_config.GameExePath)
                ? _config.GameExePath
                : Path.Combine(sptFolder, "EscapeFromTarkov.exe");

            if (!File.Exists(gameExe))
            {
                throw new FileNotFoundException(
                    $"Não encontrei o EscapeFromTarkov.exe em:\n{gameExe}\n\nDefine o caminho manualmente nas Definições.");
            }

            var backendUrl = $"https://{_config.ServerIp}:{_config.ServerPort}";
            var configJson = $"{{\"BackendUrl\":\"{backendUrl}\",\"Version\":\"live\",\"MatchingVersion\":\"live\"}}";

            var psi = new ProcessStartInfo
            {
                FileName = gameExe,
                WorkingDirectory = Path.GetDirectoryName(gameExe),
                UseShellExecute = false
            };
            psi.ArgumentList.Add("-force-gfx-jobs");
            psi.ArgumentList.Add("native");
            psi.ArgumentList.Add($"-token={profile.Id}");
            psi.ArgumentList.Add($"-config={configJson}");

            _gameProcess = Process.Start(psi);
        }

        private static async Task<bool> WaitForServerAsync(string ip, int port, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                try
                {
                    using var client = new TcpClient();
                    var connectTask = client.ConnectAsync(ip, port);
                    var completed = await Task.WhenAny(connectTask, Task.Delay(1000));
                    if (completed == connectTask && client.Connected)
                        return true;
                }
                catch
                {
                    // servidor ainda não está a aceitar ligações; tenta de novo
                }

                await Task.Delay(700);
            }

            return false;
        }

        // ---------- Parar servidor ----------

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    _serverProcess.Kill(entireProcessTree: true);
                }
                catch { /* já pode ter terminado */ }
            }

            _serverProcess = null;
            PlayButton.Content = "JOGAR";
            StopButton.IsEnabled = false;
            StatusText.Text = "Servidor parado.";
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try { _serverProcess.Kill(entireProcessTree: true); } catch { }
            }
            base.OnClosed(e);
        }
    }
}
