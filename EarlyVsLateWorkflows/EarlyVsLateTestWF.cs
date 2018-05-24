using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Diagnostics;

namespace eBECS.PhilColeTest.EarlyVsLateTestWF.Workflow
{
    public sealed class EarlyVsLateTestWF : CodeActivity
    {
        //[Input("Use Early Bound")]
        //[Default("true")]
        //[RequiredArgument]
        //public InArgument<bool> EarlyBound { get; set; }

        [Input("Warmup Count")]
        [Default("50")]
        [RequiredArgument]
        public InArgument<int> WarmupCount { get; set; }

        [Input("Run Count")]
        [Default("300")]
        [RequiredArgument]
        public InArgument<int> RunCount { get; set; }

        private void CreateAccounts(String name, int warmup, int runs, ITracingService tracing, Action action)
        {
            tracing.Trace("\n" + name);
            // Warm Up
            for (int i = 1; i <= warmup; i++)
            {
                if (i % 10 == 0)
                    tracing.Trace("{0:P0}     Warmup   ", (((decimal)i / warmup)));
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
                    tracing.Trace("{0:P0}     {1:N1}ms   ", (((decimal)i / runs)), runningAverage);
            }
        }


        protected override void Execute(CodeActivityContext executionContext)
        {
            var tracing = executionContext.GetExtension<ITracingService>();
            var context = executionContext.GetExtension<IWorkflowContext>();
            var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            int warmupCount = 0;
            int runCount = 0;

            tracing.Trace("Entering EarlyVsLateTestWF.");
            tracing.Trace("Running on Entity: {0} EntityId: {1}", context.PrimaryEntityName, context.PrimaryEntityId);

            if (null != this.WarmupCount && this.WarmupCount.Get<int>(executionContext) > 0)
            {
                warmupCount = this.WarmupCount.Get<int>(executionContext);
            }
            if (null != this.RunCount && this.RunCount.Get<int>(executionContext) > 0)
            {
                runCount = this.RunCount.Get<int>(executionContext);
            }

            tracing.Trace("Warm up count: {0}. Run count: {1}", warmupCount, runCount);

            CreateAccounts("Late Bound Test", warmupCount, runCount, tracing, () =>
            {
                Entity e = new Entity("account");
                e["name"] = "Test Early Vs Late";
                service.Create(e);
            });

            // TODO: An Account entity has a lot of plugins firing on it as standard, try this out with a custom entity to eliminate.
            CreateAccounts("Early Bound Test", warmupCount, runCount, tracing, () =>
            {
                EvL.Account a = new EvL.Account();
                a.Name = "Test Early Vs Late";
                service.Create(a);
            });

            tracing.Trace("Finished. Exiting EarlyVsLateTestWF.");
        }
    }
}