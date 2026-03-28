using Microsoft.AspNetCore.DataProtection;

namespace Graduation_Project.Services
{
    public interface IChatMessageCrypto
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    public class ChatMessageCrypto : IChatMessageCrypto
    {
        private readonly IDataProtector _protector;

        public ChatMessageCrypto(IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector("GraduationProject.ChatMessageContent.v1");
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return string.Empty;

            return _protector.Protect(plainText);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            try
            {
                return _protector.Unprotect(cipherText);
            }
            catch
            {
                // Backward compatibility for already persisted plaintext messages.
                return cipherText;
            }
        }
    }
}
