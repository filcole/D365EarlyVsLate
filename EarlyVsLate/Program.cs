using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Configuration;
using System.Diagnostics;


// Based off the blog post by Scott Durow, updated for D365 v9.02
// https://develop1.net/public/post/2014/08/09/Early-Binding-vs-Late-Binding-Performance-Revisited
namespace EarlyVsLate
{
    class Program
    {
        static void Main(string[] args)
        {
            int warmupCount = 400;
            int runCount = 500;

            CrmServiceClient crmConn = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRM"].ConnectionString);
            IOrganizationService service = crmConn.OrganizationServiceProxy;


            CreateAccounts("Early Bound Test", warmupCount, runCount, () =>
            {
                EvL.Account a = new EvL.Account();
                a.Name = "Test Early Vs Late";
                service.Create(a);
            });


            CreateAccounts("Late Bound Test", warmupCount, runCount, () =>
            {
                Entity e = new Entity("account");
                e["name"] = "Test Early Vs Late";
                service.Create(e);
            });

            //TidyUp();

            Console.WriteLine("Finished");
            Console.ReadKey();
        }

        static void CreateAccounts(String name, int warmup, int runs, Action action)
        {
            Console.WriteLine("\n" + name);
            // Warm Up
            for (int i = 1; i <= warmup; i++)
            {
                if (i % 10 == 0)
                    Console.Write("\r{0:P0}     Warmup   ", (((decimal)i / warmup)));
                action();
            }

            // Run Test
            double runningTotal = 0;
            Stopwatch stopwatch = new Stopwatch();
            for (int i = 1; i <= runs; i++)
            {
                stopwatch.Reset();
                stopwatch.Start();
                action();
                stopwatch.Stop();
                runningTotal += stopwatch.ElapsedMilliseconds;

                double runningAverage = runningTotal / i;
                if (i % 10 == 0)
                    Console.Write("\r{0:P0}     {1:N1}ms   ", (((decimal)i / runs)), runningAverage);
            }
        }
    }
}