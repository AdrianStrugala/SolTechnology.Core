using DreamTravel.Cryptography;
using Xunit;

namespace DreamTravel.CryptographyTests
{
    public class EncryptionTests
    {
        [Fact]
        public void Encrypt_Decrypt_PasswordsAreTheSame()
        {
            //Arrange
            string password = "Password";

            //Act
            var encryptedPassword = Encryption.Encrypt(password);
            var decryptedPassword = Encryption.Decrypt(encryptedPassword);

            //Assert
            Assert.Equal(password, decryptedPassword);
        }
    }
}
