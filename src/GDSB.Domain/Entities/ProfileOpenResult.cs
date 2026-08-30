namespace GDSB.Domain.Entities
{
    // WasLegacyFormat sinaliza que o arquivo lido ainda está no formato v1 (fraco) — quem abriu
    // deve chamar IProfileFileService.Save logo em seguida para migrar para v2 de forma transparente.
    public sealed record ProfileOpenResult(Profile Profile, bool WasLegacyFormat);
}
