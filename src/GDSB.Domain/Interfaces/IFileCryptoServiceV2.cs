namespace GDSB.Domain.Interfaces
{
    public interface IFileCryptoServiceV2
    {
        byte[] Encrypt(string json, string password);
        string Decrypt(byte[] fileBytes, string password);
    }
}
