namespace Crypter.Console.Jobs
{
   internal class Help
   {
      public static void DisplayHelp()
      {
         System.Console.WriteLine(@"
Commands:

    -h --help /?            Show this menu
    -d --delete-expired     Delete expired uploads
    --create-schema {connection-string}       Create tables for the 'crypter' database
    --delete-schema {connection-string}       Delete tables for the 'crypter' database
");
      }
   }
}
