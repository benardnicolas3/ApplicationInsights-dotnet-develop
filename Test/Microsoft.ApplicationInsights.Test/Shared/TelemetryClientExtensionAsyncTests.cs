﻿namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    using Extensibility.Implementation;
    using TestFramework;
    

    /// <summary>
    /// Tests corresponding to TelemetryClientExtension methods.
    /// </summary>
    [TestClass]
    public class TelemetryClientExtensionAsyncTests
    {
        private TelemetryClient telemetryClient;
        private List<ITelemetry> sendItems;
        private object sendItemsLock;

        [TestInitialize]
        public void TestInitialize()
        {
            var configuration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>();
            this.sendItemsLock = new object();
            configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item =>
            {
                lock (this.sendItemsLock)
                {
                    this.sendItems.Add(item);
                    Monitor.Pulse(this.sendItemsLock);
                }
            }};
            configuration.InstrumentationKey = Guid.NewGuid().ToString();
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            this.telemetryClient = new TelemetryClient(configuration);
            CallContextHelpers.SaveOperationContext(null);
        }

        /// <summary>
        /// Ensure that context being propagated via async/await.
        /// </summary>
        [TestMethod]
        public void ContextPropagatesThroughAsyncAwait()
        {
            var task = this.TestAsync();
            task.Wait();
        }

        /// <summary>
        /// Actual async test method.
        /// </summary>
        /// <returns>Task to await.</returns>
        public async Task TestAsync()
        {
            using (var op = this.telemetryClient.StartOperation<RequestTelemetry>("request"))
            {
                var id1 = Thread.CurrentThread.ManagedThreadId;
                this.telemetryClient.TrackTrace("trace1");

                //HttpClient client = new HttpClient();
                await Task.Delay(TimeSpan.FromMilliseconds(100));//client.GetStringAsync("http://bing.com");

                var id2 = Thread.CurrentThread.ManagedThreadId;
                this.telemetryClient.TrackTrace("trace2");

                Assert.AreNotEqual(id1, id2);
            }

            Assert.AreEqual(3, this.sendItems.Count);
            var id = ((RequestTelemetry)this.sendItems[this.sendItems.Count - 1]).Id;
            Assert.IsFalse(string.IsNullOrEmpty(id));

            foreach (var item in this.sendItems)
            {
                if (item is TraceTelemetry)
                {
                    Assert.AreEqual(id, item.Context.Operation.ParentId);
                    Assert.AreEqual(GetRootOperationId(id), item.Context.Operation.Id);
                }
                else
                {
                    Assert.AreEqual(id, ((RequestTelemetry)item).Id);
                    Assert.AreEqual(GetRootOperationId(id), item.Context.Operation.Id);
                    Assert.IsNull(item.Context.Operation.ParentId);
                }
            }
        }

        /// <summary>
        /// Ensure that context being propagated via Begin/End.
        /// </summary>
        [TestMethod, Timeout(2000)]
        public void ContextPropagatesThroughBeginEnd()
        {
            var op = this.telemetryClient.StartOperation<RequestTelemetry>("request");
            var id1 = Thread.CurrentThread.ManagedThreadId;
            int id2 = 0;
            this.telemetryClient.TrackTrace("trace1");

            var result = Task.Delay(TimeSpan.FromMilliseconds(50)).ContinueWith((t) =>
            {
                id2 = Thread.CurrentThread.ManagedThreadId;
                this.telemetryClient.TrackTrace("trace2");

                this.telemetryClient.StopOperation(op);
            });

            do
            {
                lock (this.sendItemsLock)
                {
                    if (this.sendItems.Count < 3)
                    {
                        Monitor.Wait(this.sendItemsLock, 50); // We will rely on the overall test timeout to break the wait in case of failure
                    }
                }
            } while (this.sendItems.Count < 3);

            Assert.AreNotEqual(id1, id2);
            Assert.AreEqual(3, this.sendItems.Count);
            var id = ((RequestTelemetry)this.sendItems[this.sendItems.Count - 1]).Id;
            Assert.IsFalse(string.IsNullOrEmpty(id));

            foreach (var item in this.sendItems)
            {
                if (item is TraceTelemetry)
                {
                    Assert.AreEqual(id, item.Context.Operation.ParentId);
                    Assert.AreEqual(GetRootOperationId(id), item.Context.Operation.Id);
                }
                else
                {
                    Assert.AreEqual(id, ((RequestTelemetry)item).Id);
                    Assert.AreEqual(GetRootOperationId(id), item.Context.Operation.Id);
                    Assert.IsNull(item.Context.Operation.ParentId);

                }
            }
        }

        private string GetRootOperationId(string operationId)
        {
            Assert.IsTrue(operationId.StartsWith("|"));
            return operationId.Substring(1, operationId.IndexOf('.') - 1);
        }
    }
}
