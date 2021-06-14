namespace Crypter.Console.Jobs
{
   public class Help
   {
      public static void DisplayHelp()
      {
         System.Console.WriteLine(@"
Commands:

    -h --help /?            Show this menu
    -d --delete-expired     Delete expired uploads
");
      }
   }
}
