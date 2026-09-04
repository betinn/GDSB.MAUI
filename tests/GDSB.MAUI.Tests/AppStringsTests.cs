using System.Collections;
using System.Globalization;
using GDSB.MAUI.ViewModels.Resources;
using Xunit;

namespace GDSB.MAUI.Tests
{
    // Rede que garante que uma chave adicionada/renomeada num dos dois .resx não fica esquecida no
    // outro - sem isso, uma chave faltando em inglês cairia silenciosamente de volta pro português
    // (comportamento correto do ResourceManager, mas que esconderia o esquecimento em vez de
    // sinalizar).
    public class AppStringsTests
    {
        [Fact]
        public void EnglishResourceSet_HasExactlyTheSameKeysAsTheNeutralResourceSet()
        {
            var neutralKeys = ResourceKeys(CultureInfo.InvariantCulture);
            // "en", não "en-US" (AppLanguage.En.Code): o satélite é nomeado pelo sufixo do arquivo
            // (AppStrings.en.resx), e GetResourceSet com tryParents:false só bate com a cultura
            // exata do satélite - a resolução en-US -> en só acontece com tryParents:true (o
            // caminho que LocalizationService.Get usa de verdade, via ResourceManager.GetString).
            var englishKeys = ResourceKeys(new CultureInfo("en"));

            Assert.NotEmpty(neutralKeys);
            Assert.Equal(neutralKeys, englishKeys);
        }

        // tryParents: false - queremos só as chaves definidas de fato em cada arquivo, sem herdar
        // da cultura pai (o que faria as duas listas sempre baterem, mesmo com uma chave faltando).
        private static HashSet<string> ResourceKeys(CultureInfo culture)
        {
            var resourceSet = AppStrings.ResourceManager.GetResourceSet(culture, true, false);
            Assert.NotNull(resourceSet);

            var keys = new HashSet<string>();
            foreach (DictionaryEntry entry in resourceSet)
                keys.Add((string)entry.Key);

            return keys;
        }
    }
}
