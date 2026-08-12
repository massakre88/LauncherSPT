namespace LauncherSPT
{
    public class SptProfile
    {
        // Nome do ficheiro (sem .json) - é o ID/token usado para arrancar o jogo com este perfil
        public string Id { get; set; } = "";

        public string Username { get; set; } = "";

        public string Nickname { get; set; } = "?";
        public string Side { get; set; } = "";
        public int Level { get; set; }
        public string GameVersion { get; set; } = "";

        public override string ToString()
        {
            var side = string.IsNullOrEmpty(Side) ? "" : $" [{Side.ToUpper()}]";
            var version = string.IsNullOrEmpty(GameVersion) ? "" : $" [{GameVersion}]";
            return $"{Nickname}{side} [Lvl {Level}]{version}";
        }
    }
}
