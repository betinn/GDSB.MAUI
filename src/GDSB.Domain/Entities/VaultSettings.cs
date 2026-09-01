namespace GDSB.Domain.Entities
{
    // Proteções configuráveis por cofre, gravadas dentro do próprio arquivo (Profile.Settings) - não
    // em Preferences do aparelho. Os defaults reproduzem exatamente o comportamento fixo da rodada
    // anterior, então um arquivo v2 antigo (ou um v1 migrado) sem a chave "Settings" no JSON abre com
    // este mesmo comportamento, via inicializador de propriedade do System.Text.Json.
    public class VaultSettings
    {
        public bool ClipboardClearEnabled { get; set; } = true;

        public int ClipboardClearSeconds { get; set; } = 20;

        public bool AutoLockEnabled { get; set; } = true;

        public int AutoLockMinutes { get; set; } = 2;
    }
}
