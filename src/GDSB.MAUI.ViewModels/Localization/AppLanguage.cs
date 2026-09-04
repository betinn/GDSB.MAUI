namespace GDSB.MAUI.Localization
{
    // Lista fechada de propósito - ver "Fora do escopo" no plano da rodada: um terceiro idioma é
    // trabalho futuro, não algo que este tipo precisa prever. DisplayName é o endônimo mostrado no
    // dropdown e nunca passa pelo catálogo de tradução: quem caiu num idioma que não lê precisa
    // achar o seu pelo nome nativo.
    public sealed record AppLanguage(string Code, string DisplayName)
    {
        public static readonly AppLanguage PtBr = new("pt-BR", "Português (Brasil)");

        // Code é o CultureInfo aplicado (formata data/número como en-US) - o satélite de recursos
        // continua resolvendo por ele via fallback de cultura pai (en-US -> en), sem precisar bater
        // com o nome exato da pasta do assembly satélite.
        public static readonly AppLanguage En = new("en-US", "English (US)");

        public static IReadOnlyList<AppLanguage> All { get; } = [PtBr, En];

        public static AppLanguage Default => PtBr;

        // Cobre tanto a primeira abertura (preferência ausente, code null) quanto uma preferência
        // gravada por uma versão futura com um idioma que esta versão não conhece.
        public static AppLanguage FromCode(string? code) =>
            All.FirstOrDefault(language => language.Code == code) ?? Default;
    }
}
