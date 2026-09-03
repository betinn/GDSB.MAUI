using System.Reflection;
using GDSB.MAUI.Help;
using Xunit;

namespace GDSB.MAUI.Tests
{
    // O catálogo de ajuda é texto, e texto não quebra o build quando sai do lugar - estes testes
    // são o que segura as regras da rodada: cada id referenciado pelo XAML existe, cada amostra
    // aponta para uma chave declarada e, principalmente, NENHUM tópico é só texto. A regra "cada
    // painel mostra o controle de que fala" fica garantida por teste em vez de por disciplina.
    public class HelpTopicsTests
    {
        private static readonly IReadOnlyList<string> DeclaredTopicIds = StringConstantsOf(typeof(HelpTopics.Ids));
        private static readonly IReadOnlyList<string> DeclaredVisualIds = StringConstantsOf(typeof(HelpVisuals.Ids));

        [Fact]
        public void Ids_EveryDeclaredTopicId_ExistsInCatalog()
        {
            var catalogIds = HelpTopics.All.Select(topic => topic.Id).ToHashSet(StringComparer.Ordinal);

            Assert.NotEmpty(DeclaredTopicIds);
            Assert.All(DeclaredTopicIds, id => Assert.Contains(id, catalogIds));
        }

        [Fact]
        public void All_EveryTopicId_IsDeclaredInIds()
        {
            var declared = DeclaredTopicIds.ToHashSet(StringComparer.Ordinal);

            Assert.All(HelpTopics.All, topic => Assert.Contains(topic.Id, declared));
        }

        [Fact]
        public void All_HasNoDuplicateIds()
        {
            var duplicates = HelpTopics.All
                .GroupBy(topic => topic.Id, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            Assert.Empty(duplicates);
        }

        [Fact]
        public void All_EveryTopic_HasTitleAndBlocks()
        {
            Assert.All(HelpTopics.All, topic =>
            {
                Assert.False(string.IsNullOrWhiteSpace(topic.Title), $"Tópico '{topic.Id}' sem título.");
                Assert.NotEmpty(topic.Blocks);
            });
        }

        // A regra central da rodada: painel só de texto é considerado incompleto.
        [Fact]
        public void All_EveryTopic_HasAtLeastOneVisualBlock()
        {
            Assert.All(HelpTopics.All, topic =>
                Assert.True(
                    topic.Blocks.Any(block => block.Kind == HelpBlockKind.Visual),
                    $"Tópico '{topic.Id}' não mostra nenhum controle - só descreve."));
        }

        [Fact]
        public void All_EveryVisualBlock_PointsToDeclaredVisualId()
        {
            var declared = DeclaredVisualIds.ToHashSet(StringComparer.Ordinal);

            Assert.All(VisualBlocks(), pair =>
                Assert.True(
                    declared.Contains(pair.Block.Value),
                    $"Tópico '{pair.TopicId}' usa a amostra '{pair.Block.Value}', que não está em HelpVisuals.Ids."));
        }

        [Fact]
        public void All_EveryVisualBlock_HasCaption()
        {
            Assert.All(VisualBlocks(), pair =>
                Assert.False(
                    string.IsNullOrWhiteSpace(pair.Block.Caption),
                    $"Amostra '{pair.Block.Value}' do tópico '{pair.TopicId}' está sem legenda."));
        }

        // Chave declarada que nenhum tópico usa é DataTemplate morto em HelpVisuals.xaml - some
        // sem ninguém notar que sumiu.
        [Fact]
        public void HelpVisuals_EveryDeclaredId_IsUsedBySomeTopic()
        {
            var used = VisualBlocks().Select(pair => pair.Block.Value).ToHashSet(StringComparer.Ordinal);

            Assert.All(DeclaredVisualIds, id => Assert.Contains(id, used));
        }

        [Fact]
        public void HelpVisuals_All_MatchesDeclaredIds()
        {
            Assert.Equal(
                DeclaredVisualIds.OrderBy(id => id, StringComparer.Ordinal),
                HelpVisuals.All.OrderBy(id => id, StringComparer.Ordinal));
        }

        [Fact]
        public void All_NoBlock_HasEmptyValue()
        {
            Assert.All(HelpTopics.All, topic =>
                Assert.All(topic.Blocks, block =>
                    Assert.False(
                        string.IsNullOrWhiteSpace(block.Value),
                        $"Tópico '{topic.Id}' tem um bloco {block.Kind} vazio.")));
        }

        [Fact]
        public void TryGet_KnownId_ReturnsTopic()
        {
            Assert.True(HelpTopics.TryGet(HelpTopics.Ids.BackupRecovery, out var topic));
            Assert.Equal(HelpTopics.Ids.BackupRecovery, topic.Id);
        }

        [Fact]
        public void TryGet_UnknownId_ReturnsFalse()
        {
            Assert.False(HelpTopics.TryGet("nao.existe", out _));
        }

        // O "?" único da tela de backup cobre quatro assuntos (como os backups aparecem, restaurar,
        // excluir e excluir todos) - sem Heading, o painel viraria um paredão de texto.
        [Fact]
        public void BackupRecovery_IsSplitByHeadings()
        {
            Assert.True(HelpTopics.TryGet(HelpTopics.Ids.BackupRecovery, out var topic));

            var headings = topic.Blocks.Count(block => block.Kind == HelpBlockKind.Heading);

            Assert.Equal(4, headings);
        }

        private static IEnumerable<(string TopicId, HelpBlock Block)> VisualBlocks() =>
            HelpTopics.All.SelectMany(
                topic => topic.Blocks
                    .Where(block => block.Kind == HelpBlockKind.Visual)
                    .Select(block => (topic.Id, block)));

        private static IReadOnlyList<string> StringConstantsOf(Type type) =>
            type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToList();
    }
}
