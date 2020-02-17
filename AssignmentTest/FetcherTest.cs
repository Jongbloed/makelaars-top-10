using Assignment;
using Assignment.Data;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AssignmentTest
{
    class FakeProgress : IFetchProgress
    {
        public bool[] PagesComplete { get; set; }

        public void Print()
        {
            
        }
    }

    public class FetcherTest : IDisposable
    {
        private Mock<IWoonObjectBron> fakeWoonObjectBron;
        private FakeProgress fakeProgress;
        private BlockingCollection<WoonObject[]> blockingQueue;
        private Fetcher testFetcher;

        [SetUp]
        public void SetUp()
        {
            fakeWoonObjectBron = new Mock<IWoonObjectBron>();
            fakeWoonObjectBron.Setup(x => x.HaalPagina(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((int paginaNummer, CancellationToken _) => Task.FromResult(
                    new FundaResultaat
                    {
                        Objects = new WoonObject[0],
                        Paging = new PaginaInfo
                        {
                            AantalPaginas = 1,
                            HuidigePagina = paginaNummer,
                        }
                    }
                ));
            fakeProgress = new FakeProgress();
            blockingQueue = new BlockingCollection<WoonObject[]>();
            testFetcher = new Fetcher(fakeWoonObjectBron.Object, fakeProgress, blockingQueue);
        }

        [Test]
        public async Task FetchAllAsync_DoesNotCrash()
        {
            await testFetcher.FetchAllAsync(default);
        }

        public void Dispose()
        {
            testFetcher.Dispose();
        }
    }
}
