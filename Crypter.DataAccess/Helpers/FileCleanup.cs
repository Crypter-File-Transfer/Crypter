using System;
using System.IO;

namespace Crypter.DataAccess.Helpers
{
    public class FileCleanup
    {
        public string uploadId { get; set; }
        private readonly string folderName;

        public FileCleanup(string guid, string baseFilePath)
        {
            uploadId = guid;
            folderName = baseFilePath;
        }

        /// <summary>
        /// Accepts a boolean for upload type and deletes the directory and all subdirectories and files
        /// </summary>
        /// <param name="isFile"></param>
        /// <returns>true or false to indicate whether the operation was successful</returns>
        /// https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.delete?view=net-5.0
        public bool CleanExpiredDirectory(bool isFile)
        {
            string pathString;
            //create path files
            if (isFile)
            {
                pathString = Path.GetFullPath(Path.Combine(folderName, $"files/{uploadId}"));
            }
            //create folder path for message upload
            else
            {
                pathString = Path.GetFullPath(Path.Combine(folderName, $"messages/{uploadId}"));
            }
            try
            {
                //delete directory and subcontent at path
                Directory.Delete(pathString, true);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception!: {exception}");
                return false;
            }
            return false;
        }
    }

}
