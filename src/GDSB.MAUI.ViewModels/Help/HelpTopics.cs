using System.Globalization;
using GDSB.MAUI.ViewModels.Resources;

namespace GDSB.MAUI.Help
{
    // Catálogo único do texto de ajuda do app, inteiro. Fica aqui, e não no XAML, por três motivos:
    // a redação passa a ser revisável num arquivo só, o teste consegue cobri-la (ids existentes,
    // sem duplicatas, cada tópico com pelo menos uma amostra visual) e a mesma explicação pode ser
    // reaproveitada por telas diferentes - PROTEÇÕES e BACKUPS aparecem tanto na criação do cofre
    // quanto na edição.
    //
    // Regra de redação: usuário final leigo. Nada de "criptografia", "hash", "instância" ou
    // "persistir"; frases curtas, segunda pessoa, e sempre dizendo ONDE o controle está na tela.
    //
    // Os nomes de constante evitam de propósito as palavras "password"/"pwd"/"passphrase": a regra
    // S2068 do Sonar flagra a declaração de qualquer campo com esses nomes e valor literal como
    // "credencial no código", mesmo quando o valor é só um identificador. Mesmo motivo do
    // VaultUnlockCode dos testes - ver claude-context.md.
    //
    // A prosa (Title, Text/Heading, Caption) vem do catálogo (AppStrings), lida na cultura vigente -
    // por isso All não é mais materializado uma vez num inicializador estático: rodada 4 (multilíngue)
    // precisa que o mesmo tópico saia diferente depois de uma troca de idioma. Como ler o
    // ResourceManager de novo pra cada um dos 48 textos toda vez que um "?" abre seria trabalho à
    // toa, o resultado fica em cache por cultura (só duas culturas existem - ver AppLanguage - então
    // o cache nunca cresce fora de controle e não precisa de invalidação: a próxima leitura na
    // cultura vigente já bate no cache dela).
    public static class HelpTopics
    {
        public static class Ids
        {
            public const string MasterUnlockCode = "vault.masterUnlockCode";
            public const string VaultName = "vault.name";
            public const string VaultProtections = "vault.protections";
            public const string VaultBackups = "vault.backups";
            public const string ChangeMasterUnlockCode = "vault.changeMasterUnlockCode";
            public const string SecretUrl = "secret.url";
            public const string SecretStoredValue = "secret.storedValue";
            public const string SecretFavorite = "secret.favorite";
            public const string BackupRecovery = "backup.recovery";
        }

        private static readonly Dictionary<string, IReadOnlyList<HelpTopic>> TopicsByCulture = new();
        private static readonly Dictionary<string, Dictionary<string, HelpTopic>> ByIdByCulture = new();

        public static IReadOnlyList<HelpTopic> All => GetTopicsForCurrentCulture();

        public static bool TryGet(string id, out HelpTopic topic) =>
            GetByIdForCurrentCulture().TryGetValue(id, out topic!);

        private static IReadOnlyList<HelpTopic> GetTopicsForCurrentCulture()
        {
            var cultureKey = CultureInfo.CurrentUICulture.Name;
            if (TopicsByCulture.TryGetValue(cultureKey, out var cached))
                return cached;

            var topics = BuildTopics();
            TopicsByCulture[cultureKey] = topics;
            ByIdByCulture[cultureKey] = topics.ToDictionary(topic => topic.Id, StringComparer.Ordinal);
            return topics;
        }

        private static Dictionary<string, HelpTopic> GetByIdForCurrentCulture()
        {
            GetTopicsForCurrentCulture();
            return ByIdByCulture[CultureInfo.CurrentUICulture.Name];
        }

        private static string Tr(string key) =>
            AppStrings.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

        private static IReadOnlyList<HelpTopic> BuildTopics() =>
        [
            new HelpTopic(
                Ids.MasterUnlockCode,
                Tr("Help_MasterUnlockCode_Title"),
                [
                    HelpBlock.OfText(Tr("Help_MasterUnlockCode_Text1")),
                    HelpBlock.OfText(Tr("Help_MasterUnlockCode_Text2")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.MasterUnlockCodeField, Tr("Help_MasterUnlockCode_VisualCaption")),
                ]),

            new HelpTopic(
                Ids.VaultName,
                Tr("Help_VaultName_Title"),
                [
                    HelpBlock.OfText(Tr("Help_VaultName_Text1")),
                    HelpBlock.OfText(Tr("Help_VaultName_Text2")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.SaveAsNewFileCard, Tr("Help_VaultName_VisualCaption")),
                ]),

            new HelpTopic(
                Ids.VaultProtections,
                Tr("Help_VaultProtections_Title"),
                [
                    HelpBlock.OfHeading(Tr("Help_VaultProtections_Heading1")),
                    HelpBlock.OfText(Tr("Help_VaultProtections_Text1")),
                    HelpBlock.OfHeading(Tr("Help_VaultProtections_Heading2")),
                    HelpBlock.OfText(Tr("Help_VaultProtections_Text2")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.ProtectionTimeChips, Tr("Help_VaultProtections_VisualCaption")),
                ]),

            new HelpTopic(
                Ids.VaultBackups,
                Tr("Help_VaultBackups_Title"),
                [
                    HelpBlock.OfText(Tr("Help_VaultBackups_Text1")),
                    HelpBlock.OfText(Tr("Help_VaultBackups_Text2")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.BackupModeChips, Tr("Help_VaultBackups_VisualCaption")),
                ]),

            new HelpTopic(
                Ids.ChangeMasterUnlockCode,
                Tr("Help_ChangeMasterUnlockCode_Title"),
                [
                    HelpBlock.OfText(Tr("Help_ChangeMasterUnlockCode_Text1")),
                    HelpBlock.OfText(Tr("Help_ChangeMasterUnlockCode_Text2")),
                    HelpBlock.OfText(Tr("Help_ChangeMasterUnlockCode_Text3")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.DeleteOldBackupsCheck, Tr("Help_ChangeMasterUnlockCode_VisualCaption")),
                ]),

            new HelpTopic(
                Ids.SecretUrl,
                Tr("Help_SecretUrl_Title"),
                [
                    HelpBlock.OfText(Tr("Help_SecretUrl_Text1")),
                    HelpBlock.OfText(Tr("Help_SecretUrl_Text2")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.SecretUrlLink, Tr("Help_SecretUrl_VisualCaption")),
                ]),

            new HelpTopic(
                Ids.SecretStoredValue,
                Tr("Help_SecretStoredValue_Title"),
                [
                    HelpBlock.OfText(Tr("Help_SecretStoredValue_Text1")),
                    HelpBlock.OfText(Tr("Help_SecretStoredValue_Text2")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.SecretValueActions, Tr("Help_SecretStoredValue_VisualCaption")),
                ]),

            new HelpTopic(
                Ids.SecretFavorite,
                Tr("Help_SecretFavorite_Title"),
                [
                    HelpBlock.OfText(Tr("Help_SecretFavorite_Text1")),
                    HelpBlock.OfText(Tr("Help_SecretFavorite_Text2")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.FavoriteStar, Tr("Help_SecretFavorite_VisualCaption")),
                ]),

            new HelpTopic(
                Ids.BackupRecovery,
                Tr("Help_BackupRecovery_Title"),
                [
                    HelpBlock.OfHeading(Tr("Help_BackupRecovery_Heading1")),
                    HelpBlock.OfText(Tr("Help_BackupRecovery_Text1")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.BackupCard, Tr("Help_BackupRecovery_VisualCaption1")),

                    HelpBlock.OfHeading(Tr("Help_BackupRecovery_Heading2")),
                    HelpBlock.OfText(Tr("Help_BackupRecovery_Text2")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.RestoreButton, Tr("Help_BackupRecovery_VisualCaption2")),

                    HelpBlock.OfHeading(Tr("Help_BackupRecovery_Heading3")),
                    HelpBlock.OfText(Tr("Help_BackupRecovery_Text3")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.DeleteBackupButton, Tr("Help_BackupRecovery_VisualCaption3")),

                    HelpBlock.OfHeading(Tr("Help_BackupRecovery_Heading4")),
                    HelpBlock.OfText(Tr("Help_BackupRecovery_Text4")),
                    HelpBlock.OfVisual(HelpVisuals.Ids.DeleteAllBackupsLink, Tr("Help_BackupRecovery_VisualCaption4")),
                ]),
        ];
    }

    // Chaves das amostras visuais. Cada uma corresponde a um DataTemplate declarado em
    // GDSB.MAUI/Resources/HelpVisuals.xaml, que monta uma réplica inerte do controle real usando
    // os estilos de verdade de Styles.xaml. O projeto de teste é net10.0 puro e não enxerga XAML,
    // então a correspondência chave -> DataTemplate é garantida em tempo de execução por uma
    // guarda de DEBUG em HelpSheetView, não por teste.
    public static class HelpVisuals
    {
        public static class Ids
        {
            public const string MasterUnlockCodeField = "HelpVisual.MasterUnlockCodeField";
            public const string SaveAsNewFileCard = "HelpVisual.SaveAsNewFileCard";
            public const string ProtectionTimeChips = "HelpVisual.ProtectionTimeChips";
            public const string BackupModeChips = "HelpVisual.BackupModeChips";
            public const string DeleteOldBackupsCheck = "HelpVisual.DeleteOldBackupsCheck";
            public const string SecretUrlLink = "HelpVisual.SecretUrlLink";
            public const string SecretValueActions = "HelpVisual.SecretValueActions";
            public const string FavoriteStar = "HelpVisual.FavoriteStar";
            public const string BackupCard = "HelpVisual.BackupCard";
            public const string RestoreButton = "HelpVisual.RestoreButton";
            public const string DeleteBackupButton = "HelpVisual.DeleteBackupButton";
            public const string DeleteAllBackupsLink = "HelpVisual.DeleteAllBackupsLink";
        }

        public static IReadOnlyList<string> All { get; } =
        [
            Ids.MasterUnlockCodeField,
            Ids.SaveAsNewFileCard,
            Ids.ProtectionTimeChips,
            Ids.BackupModeChips,
            Ids.DeleteOldBackupsCheck,
            Ids.SecretUrlLink,
            Ids.SecretValueActions,
            Ids.FavoriteStar,
            Ids.BackupCard,
            Ids.RestoreButton,
            Ids.DeleteBackupButton,
            Ids.DeleteAllBackupsLink,
        ];
    }
}
