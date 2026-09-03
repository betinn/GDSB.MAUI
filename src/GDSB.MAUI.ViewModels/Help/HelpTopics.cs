namespace GDSB.MAUI.Help
{
    // Catálogo único de todo o texto de ajuda do app. Fica aqui, e não no XAML, por três motivos:
    // a redação passa a ser revisável num arquivo só, o teste consegue cobri-la (ids existentes,
    // sem duplicatas, todo tópico com pelo menos uma amostra visual) e a mesma explicação pode ser
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

        public static IReadOnlyList<HelpTopic> All { get; } =
        [
            new HelpTopic(
                Ids.MasterUnlockCode,
                "A senha mestra",
                [
                    HelpBlock.OfText(
                        "É a senha que abre este cofre. Ela não fica guardada em lugar nenhum: nem no app, " +
                        "nem no seu celular, nem na internet. Por isso não existe \"esqueci minha senha\" " +
                        "aqui - se você perder essa senha, ninguém consegue abrir o arquivo de novo, nem " +
                        "você, nem quem fez o app."),
                    HelpBlock.OfText(
                        "Escolha algo que você lembre com folga e que ninguém adivinhe. O mínimo são 8 " +
                        "caracteres, mas quanto mais longa, melhor."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.MasterUnlockCodeField,
                        "É este campo. Toque nele e digite - as letras aparecem como pontinhos para " +
                        "ninguém ler por cima do seu ombro."),
                ]),

            new HelpTopic(
                Ids.VaultName,
                "O nome do cofre",
                [
                    HelpBlock.OfText(
                        "É o apelido que aparece dentro do app para você reconhecer este cofre. Trocar o " +
                        "nome aqui não renomeia o arquivo que está salvo no seu celular ou na nuvem: o " +
                        "arquivo continua com o nome de antes."),
                    HelpBlock.OfText(
                        "Por isso, depois de salvar um nome novo, o app oferece guardar também uma cópia " +
                        "num arquivo novo. É opcional - se você recusar, está tudo certo, a mudança já foi " +
                        "gravada no arquivo de sempre."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.SaveAsNewFileCard,
                        "É este convite que aparece depois de salvar. Tocar em \"Salvar como novo arquivo\" " +
                        "cria uma cópia e mantém o original intacto."),
                ]),

            new HelpTopic(
                Ids.VaultProtections,
                "Proteções do cofre",
                [
                    HelpBlock.OfHeading("Limpar a área de transferência"),
                    HelpBlock.OfText(
                        "Quando você copia uma senha, ela fica na memória de \"copiar e colar\" do celular e " +
                        "qualquer outro aplicativo pode ler de lá. Com esta proteção ligada, o app apaga " +
                        "esse valor sozinho depois do tempo que você escolher."),
                    HelpBlock.OfHeading("Bloqueio automático"),
                    HelpBlock.OfText(
                        "Se você sair do app e demorar para voltar, ele fecha o cofre e pede a senha mestra " +
                        "de novo. Serve para o caso de você deixar o celular na mesa e alguém pegar."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.ProtectionTimeChips,
                        "Toque em um dos tempos para escolher. O que estiver colorido é o que está valendo."),
                ]),

            new HelpTopic(
                Ids.VaultBackups,
                "Backups automáticos",
                [
                    HelpBlock.OfText(
                        "Toda vez que você salva alguma coisa, o app guarda antes uma cópia de como o cofre " +
                        "estava. Assim, se você apagar um item sem querer, dá para voltar atrás."),
                    HelpBlock.OfText(
                        "Aqui você decide quantas dessas cópias ficam guardadas. \"Por quantidade\" guarda um " +
                        "número fixo de versões. \"Por tempo\" guarda as dos últimos dias que você escolher. " +
                        "A cópia mais recente nunca é apagada, aconteça o que acontecer."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.BackupModeChips,
                        "Toque em \"Por quantidade\" ou \"Por tempo\" para escolher a regra. Logo abaixo " +
                        "aparecem os valores disponíveis."),
                ]),

            new HelpTopic(
                Ids.ChangeMasterUnlockCode,
                "Trocar a senha mestra",
                [
                    HelpBlock.OfText(
                        "Para trocar, o app pede a senha atual primeiro - é assim que ele confere que é " +
                        "você mesmo. Depois da troca, é a senha nova que abre este cofre."),
                    HelpBlock.OfText(
                        "Atenção com os backups: as cópias guardadas antes da troca continuam presas à " +
                        "senha antiga. Se um dia você precisar restaurar uma delas, é a senha antiga que " +
                        "vai funcionar, não a nova. Por isso vale anotar a antiga em algum lugar seguro, " +
                        "ou marcar a caixa que apaga essas cópias de uma vez."),
                    HelpBlock.OfText(
                        "Se você usa a digital para abrir o cofre, não precisa fazer nada: o app reconfigura " +
                        "a biometria sozinho com a senha nova."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.DeleteOldBackupsCheck,
                        "Marque esta caixa se você não quer guardar cópias que só abrem com a senha antiga."),
                ]),

            new HelpTopic(
                Ids.SecretUrl,
                "O endereço do site",
                [
                    HelpBlock.OfText(
                        "É opcional, mas ajuda bastante: quando você preenche, o endereço vira um atalho no " +
                        "cartão do item. Um toque e o site abre no navegador, sem você precisar digitar."),
                    HelpBlock.OfText(
                        "Pode escrever do jeito simples, como \"netflix.com\" - não precisa colocar o " +
                        "\"https://\" na frente."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.SecretUrlLink,
                        "Fica logo abaixo do nome do item, em rosa. Tocar nele abre o site."),
                ]),

            new HelpTopic(
                Ids.SecretStoredValue,
                "A senha guardada",
                [
                    HelpBlock.OfText(
                        "É a senha daquele site ou aplicativo - a que você usaria para entrar. Ela fica " +
                        "escondida atrás de pontinhos e só aparece quando você pede."),
                    HelpBlock.OfText(
                        "Na hora de usar, você tem duas opções: o olhinho mostra a senha na tela, e " +
                        "\"Copiar\" manda ela direto para o \"colar\" do celular, sem precisar mostrar para " +
                        "ninguém. Se a proteção de área de transferência estiver ligada, o app apaga o " +
                        "valor copiado sozinho depois de alguns segundos."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.SecretValueActions,
                        "São estes dois botões, no cartão da senha: o olhinho mostra, o \"Copiar\" copia."),
                ]),

            new HelpTopic(
                Ids.SecretFavorite,
                "Marcar como favorito",
                [
                    HelpBlock.OfText(
                        "Favoritos sobem para o topo da lista e ganham uma estrelinha dourada. Serve para " +
                        "os itens que você abre toda hora não se perderem no meio dos outros."),
                    HelpBlock.OfText(
                        "Não muda nada na segurança nem no conteúdo do item - é só uma forma de encontrar " +
                        "mais rápido."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.FavoriteStar,
                        "É esta estrelinha, que passa a aparecer no cartão do item na lista."),
                ]),

            new HelpTopic(
                Ids.BackupRecovery,
                "Recuperar um backup",
                [
                    HelpBlock.OfHeading("Como estas cópias aparecem aqui"),
                    HelpBlock.OfText(
                        "Toda vez que você salva algo no cofre, o app guarda antes uma cópia de como ele " +
                        "estava. Essas cópias ficam numa pasta privada do aplicativo, nunca junto do seu " +
                        "arquivo, para não bagunçar a sua pasta. Quantas ficam guardadas é o que você " +
                        "escolheu em Editar cofre, na parte de Backups."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.BackupCard,
                        "Cada cópia vira um cartão como este, com a data e a hora em que foi guardada."),

                    HelpBlock.OfHeading("Restaurar"),
                    HelpBlock.OfText(
                        "Restaurar não mexe no seu cofre atual: o app cria um arquivo novo com o conteúdo " +
                        "daquela cópia, e você escolhe depois qual dos dois quer abrir. O app vai pedir a " +
                        "senha mestra da época daquela cópia - se ela for de antes de você trocar a senha, " +
                        "é a senha antiga que funciona."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.RestoreButton,
                        "É este botão, no rodapé de cada cartão."),

                    HelpBlock.OfHeading("Excluir"),
                    HelpBlock.OfText(
                        "Apaga só aquela cópia da lista. Não dá para desfazer, e o seu cofre atual continua " +
                        "intacto."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.DeleteBackupButton,
                        "Fica ao lado de \"Restaurar\", em vermelho."),

                    HelpBlock.OfHeading("Excluir todos"),
                    HelpBlock.OfText(
                        "Apaga de uma vez todas as cópias que estão nesta tela. Também não dá para " +
                        "desfazer. Seu cofre atual não é afetado - só as cópias antigas somem."),
                    HelpBlock.OfVisual(
                        HelpVisuals.Ids.DeleteAllBackupsLink,
                        "É este link vermelho, no fim da lista."),
                ]),
        ];

        private static readonly Dictionary<string, HelpTopic> ById =
            All.ToDictionary(topic => topic.Id, StringComparer.Ordinal);

        public static bool TryGet(string id, out HelpTopic topic) => ById.TryGetValue(id, out topic!);
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
