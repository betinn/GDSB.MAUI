namespace GDSB.Infrastructure.Backup
{
    // Nome de exibição do arquivo de backup. O prefixo resolve a truncagem em tela pequena (o que
    // se perde é o fim do nome, não o começo); o sufixo continua existindo pra nunca confundir um
    // backup com um cofre de verdade. IsBackupName aceita o prefixo OU qualquer um dos sufixos
    // porque também precisa reconhecer backups antigos, gravados do lado do cofre no formato velho
    // (só sufixo, sem o prefixo "BKP - ").
    public static class VaultBackupNaming
    {
        public const string Prefix = "BKP - ";
        public const string RollingSuffix = ".bak";
        public const string LegacySuffix = ".v1.bak";

        public static string BuildName(string vaultFileName, string suffix) => $"{Prefix}{vaultFileName}{suffix}";

        // Usado só para Rolling, que agora acumula versões em vez de sobrescrever - o timestamp
        // é o que distingue uma versão da outra. Sem ":" no horário porque é ilegal em nome de
        // arquivo no Windows.
        public static string BuildName(string vaultFileName, string suffix, DateTime createdAtUtc) =>
            $"{Prefix}{vaultFileName} - {createdAtUtc:yyyy-MM-dd HH-mm-ss}{suffix}";

        public static bool IsBackupName(string fileName) =>
            fileName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(LegacySuffix, StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(RollingSuffix, StringComparison.OrdinalIgnoreCase);
    }
}
