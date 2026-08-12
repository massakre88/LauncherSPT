namespace LauncherSPT
{
    public class AppConfig
    {
        // Caminho para SPT.Server.exe - a pasta onde este ficheiro está também é
        // considerada a pasta raiz da instalação SPT (onde ficam user\profiles, EscapeFromTarkov.exe, etc.)
        public string ServerExePath { get; set; } = "";

        // Caminho opcional para o EscapeFromTarkov.exe, caso não esteja na mesma pasta do servidor.
        // Se vazio, é detetado automaticamente na pasta do SPT.Server.exe.
        public string GameExePath { get; set; } = "";

        // Endereço do servidor. É lido automaticamente de SPT_Data\database\server.json quando possível;
        // estes valores servem de reserva caso esse ficheiro não seja encontrado.
        public string ServerIp { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 6969;

        // Caminho opcional para uma imagem de fundo (jpg/png)
        public string BackgroundImagePath { get; set; } = "";

        // Textos do título mostrados no launcher
        public string TitleTopText { get; set; } = "ESCAPE FROM TARKOV";
        public string TitleMainText { get; set; } = "SINGLE PLAYER";

        // Último perfil selecionado (para pré-selecionar da próxima vez)
        public string LastProfileId { get; set; } = "";
    }
}
