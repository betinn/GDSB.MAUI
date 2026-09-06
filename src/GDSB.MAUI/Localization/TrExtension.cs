namespace GDSB.MAUI.Localization
{
    // Ponte entre "{loc:Tr Chave}" no XAML e o catálogo. Devolve um BindingBase (não uma string
    // resolvida na hora) - é isso que dá a troca de idioma ao vivo: o binding fica vivo, ligado ao
    // indexador de ILocalizationService, e reavalia sozinho quando o serviço notifica (ver
    // LocalizationService.SetLanguage e o "ponto de risco" documentado no plano da fase 1).
    // AcceptEmptyServiceProvider: ProvideValue não lê nada do IServiceProvider - a fonte do binding
    // sai de Source ou do LocalizationServiceLocator. Sem o atributo, o compilador de XAML emite um
    // XC0103 por ponto de uso de "{loc:Tr ...}" (172 no projeto) e cai para a resolução em runtime.
    [AcceptEmptyServiceProvider]
    [ContentProperty(nameof(Key))]
    public sealed class TrExtension : IMarkupExtension<BindingBase>
    {
        public string Key { get; set; } = string.Empty;

        // Só precisa ser setado explicitamente dentro de um DataTemplate (ex.:
        // Resources/HelpVisuals.xaml, fase 2): lá o BindingContext do binding avaliado é o item do
        // template, não o singleton de localização. Fora de um DataTemplate, o padrão (null) já
        // resolve o serviço certo.
        public object? Source { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            var localizationService = Source ?? (object)LocalizationServiceLocator.Resolve();

            return new Binding($"[{Key}]", BindingMode.OneWay, source: localizationService);
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
    }
}
