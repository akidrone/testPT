using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PlaytechJob
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Start");
            System.Net.WebRequest.DefaultWebProxy.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            //WebRequest.DefaultWebProxy = new WebProxy("http://localhost:8888/",true);

            Info("Started");
            var service = new PlaytechService(Error, Trace, "username", "password", "https://admin.megasportcasino.com");
            try
            {
                //var wr = service.DownloadPlayerGamesReport(DateTime.Today, DateTime.Today + TimeSpan.FromMinutes(10));
                //if (wr.Result != null)
                //File.WriteAllText("playergames.csv", wr.Result);

                //var wr = service.DownloadFinancialReport(DateTime.Today - TimeSpan.FromDays(1), DateTime.Today);
                //if (wr.Result != null)
                //File.WriteAllText("playertransactions.csv", wr.Result);

                //var wr = service.DownloadPlayerLoginsReport(DateTime.Today - TimeSpan.FromDays(10), DateTime.Today);
                //if (wr.Result != null)
                //File.WriteAllText("playerlogins.csv", wr.Result);

                // casino - empty or code (mannycasino - 431)
                // delivery platforms are numbers
                // client platform - empty or name
                var wr = service.DownloadDailyStatsReport(DateTime.Today - TimeSpan.FromDays(21), DateTime.Today, "download", "", "");
                if (wr.Result != null)
                    File.WriteAllText("dailystats.csv", wr.Result);
                Info("End with success");
            }
            catch(Exception exc)
            {
                Error(exc.ToString());
            }
            finally
            {
                service.Logout();
            }
            Console.WriteLine("End");
            Console.ReadLine();
        }

        private static void Trace(string msg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(msg);
        }

        private static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
        }

        private static void Info(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
        }
    }
}