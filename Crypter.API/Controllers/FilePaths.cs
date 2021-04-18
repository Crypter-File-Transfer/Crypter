using System;
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
                pathString = System.IO.Path.Combine(folderName, $"files/{guid}");
            }
            //create folder path for message upload
            else
            {
                pathString = System.IO.Path.Combine(folderName, $"messages/{guid}");
            }
            //Create folder for uploaded file 
            System.IO.Directory.CreateDirectory(pathString);
            //create paths for encrypted content and signature
            ActualFileName = $"{untrustedName}";
            SignatureName = $"{untrustedName}.sig";
            ActualPathString = System.IO.Path.Combine(pathString, ActualFileName);
            SigPathString = System.IO.Path.Combine(pathString, SignatureName);
            ////Confirm paths, write to console
            Console.WriteLine("Newly created file path: {0}", ActualPathString);
            Console.WriteLine("New created signature path: {0}", SigPathString);

            return true;
        }

    }
}
