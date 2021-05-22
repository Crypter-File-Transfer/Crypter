using System;
using System.IO;

namespace Crypter.DataAccess.FileSystem
{
    public class FileCleanup
    {
        public Guid UploadId { get; set; }
        private readonly string folderName;

        public FileCleanup(Guid guid, string baseFilePath)
        {
            UploadId = guid;
            folderName = baseFilePath;
        }

        /// <summary>
        /// Accepts a boolean for upload type and deletes the directory and all subdirectories and files
        /// </summary>
        /// <param name="isFile"></param>
        /// <returns>true or false to indicate whether the operation was successful</returns>
        /// https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.delete?view=net-5.0
        public bool CleanDirectory(bool isFile)
        {
            string pathString;
            //create path files
            if (isFile)
            {
                pathString = Path.GetFullPath(Path.Combine(folderName, $"files/{UploadId}"));
            }
            //create folder path for message upload
            else
            {
                pathString = Path.GetFullPath(Path.Combine(folderName, $"messages/{UploadId}"));
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
            return true;
        }
    }

}
