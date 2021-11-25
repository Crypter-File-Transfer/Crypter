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
    --migrate-schema-v1 {connection-string}   Migrate the 'crypter' database to schema v1
    --delete-schema {connection-string}       Delete tables for the 'crypter' database
");
      }
   }
}
