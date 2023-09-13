using Xunit;

namespace DreamTravel.Identity.Cryptography.UnitTests
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

        [Fact]
        public void Encrypt_PasswordIsShort_NoExceptionIsThrown()
        {
            //Arrange
            string password = "x";

            //Act
            var encryptedPassword = Encryption.Encrypt(password);

            //Assert
            //no exception :)
        }
    }
}
