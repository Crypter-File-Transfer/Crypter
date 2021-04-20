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
            ActualPathString = Path.Combine(pathString, ActualFileName);
            SigPathString = Path.Combine(pathString, SignatureName);
            ////Confirm paths, write to console
            Console.WriteLine("Newly created file path: {0}", ActualPathString);
            Console.WriteLine("New created signature path: {0}", SigPathString);

            return true;
        }

        /// <summary>
        /// Accepts a file path and string to write the string to the provided file
        /// </summary>
        /// <param name="destPath"></param>
        /// <param name="cipherText"></param>
        /// https://docs.microsoft.com/en-us/dotnet/api/system.io.file.writealltext?view=net-5.0
        public bool WriteToFile(string destPath, string cipherText)
        {
            try
            {
                // create file and write all text to file, then close file
                File.WriteAllText(destPath, cipherText);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception!: {exception}");
            }
            return true;
        }
    }
}
