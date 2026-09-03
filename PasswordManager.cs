using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Matric_scope
{
    public static class PasswordManager
    {
        // Hidden path: C:\Users\Username\AppData\Local\VariationApp\security.dat
        private static readonly string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VariationApp");
        private static readonly string filePath = Path.Combine(directoryPath, "security.dat");
        private const string DefaultPassword = "admin123";

        public static void Initialize()
        {
            if (!Directory.Exists(directoryPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(directoryPath);
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden; // Hides the folder
            }

            if (!File.Exists(filePath))
            {
                SetPassword(DefaultPassword);
            }
        }

        public static bool VerifyPassword(string input)
        {
            Initialize();
            string storedHash = File.ReadAllText(filePath);
            return storedHash == HashPassword(input);
        }

        public static void SetPassword(string newPassword)
        {
            if (!Directory.Exists(directoryPath)) Initialize();
            File.WriteAllText(filePath, HashPassword(newPassword));
        }

        private static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte t in bytes)
                {
                    builder.Append(t.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
