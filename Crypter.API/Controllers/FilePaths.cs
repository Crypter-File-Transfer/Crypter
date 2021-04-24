using System;
using System.IO;
namespace Crypter.API.Controllers

{
    public class FilePaths
    {
        private readonly string folderName;
        public string ActualFileName { get; set; }
        public string SignatureName { get; set; }
        public string ActualPathString { get; set; }
        public string SigPathString { get; set; }

        public FilePaths(string baseFilePath)
        {
            folderName = baseFilePath;
        }

        /// <summary>
        /// Accepts a file name "untrusted name" and guid to generate file paths
        /// </summary>
        /// <param name="untrustedName"></param>
        /// <param name="guid"></param>
        /// <param name="isFile"></param>
        /// <returns>true or false to indicate whether the operation was successful</returns>
        public bool SaveFile(string untrustedName, string guid, bool isFile)
        {
            string pathString;
            //create folder path for file upload
            if (isFile)
            {
                pathString = Path.Combine(folderName, $"files/{guid}");
            }
            //create folder path for message upload
            else
            {
                pathString = Path.Combine(folderName, $"messages/{guid}");
            }
            //Create folder for uploaded file 
            System.IO.Directory.CreateDirectory(pathString);
            //create paths for encrypted content and signature
            ActualFileName = $"{untrustedName}";
            SignatureName = $"{untrustedName}.sig";
            // Combine paths and use standard directory separator
            ActualPathString = Path.GetFullPath(Path.Combine(pathString, ActualFileName));
            SigPathString = Path.GetFullPath(Path.Combine(pathString, SignatureName));
            ////Confirm paths, write to console
            Console.WriteLine("Newly created file path: {0}", ActualPathString);
            Console.WriteLine("New created signature path: {0}", SigPathString);

            return true;
        }

        /// <summary>
        /// Accepts a file path and string to write the string to the provided file as text
        /// </summary>
        /// <param name="destPath"></param>
        /// <param name="cipherText"></param>
        /// https://docs.microsoft.com/en-us/dotnet/api/system.io.file.writealltext?view=net-5.0
        public bool WriteTextToFile(string destPath, string cipherText)
        {
            try
            {
                // create file and write all text to file, then close file
                File.WriteAllText(destPath, cipherText);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception!: {exception}");
                return false;
            }
        }


        /// <summary>
        /// Accepts a file path and base64 encoded string. Decodes string to bytes and writes to destPath as binary
        /// </summary>
        /// <param name="destPath"></param>
        /// <param name="cipherText"></param>
        /// https://docs.microsoft.com/en-us/dotnet/api/system.io.file.writeallbytes?view=net-5.0
        public bool WriteBinaryToFile(string destPath, string cipherText)
        {
            //decode base64 to bytes and save as binary
            try
            {
                // create file and write all text to file, then close file
                File.WriteAllBytes(destPath, Convert.FromBase64String(cipherText));
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception!: {exception}");
                return false;
            }
        }

        /// <summary>
        /// Accepts a file path and returns the size 
        /// </summary>
        /// <param name="destPath"></param>
        /// <param name="cipherText"></param>
        public int FileSizeBytes(string targetPath)
        {
            //decode base64 to bytes and save as binary
            try
            {
                FileInfo file = new FileInfo(targetPath);
                int size = (int)file.Length;
                return size; 
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception caught!: {exception}");
                return -1; 
            }
        }


    }
}
