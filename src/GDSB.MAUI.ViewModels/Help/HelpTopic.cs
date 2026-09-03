namespace GDSB.MAUI.Help
{
    // Um tópico de ajuda não é uma lista de parágrafos: é uma lista de blocos, porque a regra
    // desta rodada é que cada painel MOSTRE o controle de que fala, não só o descreva. Um painel
    // só de texto é considerado incompleto - HelpTopicsTests garante isso por teste, em vez de
    // depender de disciplina de quem escreve.
    public enum HelpBlockKind
    {
        // Subtítulo. Existe porque um tópico pode cobrir uma tela inteira (o "?" único da tela de
        // backup cobre quatro seções) e precisa se dividir.
        Heading,

        // Parágrafo em português leigo.
        Text,

        // Amostra visual: Value é uma chave declarada em HelpVisuals.Ids e Caption é a legenda
        // curta que aparece abaixo da amostra.
        Visual
    }

    public sealed record HelpBlock(HelpBlockKind Kind, string Value, string? Caption = null)
    {
        public static HelpBlock OfHeading(string text) => new(HelpBlockKind.Heading, text);

        public static HelpBlock OfText(string text) => new(HelpBlockKind.Text, text);

        public static HelpBlock OfVisual(string visualId, string caption) =>
            new(HelpBlockKind.Visual, visualId, caption);
    }

    public sealed record HelpTopic(string Id, string Title, IReadOnlyList<HelpBlock> Blocks);
}
