using System.Text;
using System.Security.Cryptography;

namespace ProxyInterface
{
    public static class CryptoManager
    {
        private static readonly byte[] AES_KEY = StringToByteArray("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
        private static readonly byte[] AES_IV = Encoding.ASCII.GetBytes("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");

        public static string DecryptAes(string encryptedData)
        {
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = AES_KEY;
                    aes.IV = AES_IV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(encryptedBytes, 0, encryptedBytes.Length);
                            cs.FlushFinalBlock();
                        }

                        byte[] decryptedBytes = ms.ToArray();
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decrypting data: {ex.Message}");
                return null;
            }
        }

        private static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("La chaîne hexadécimale ne peut pas avoir une longueur impaire");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
            }

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}