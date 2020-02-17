using System;
using System.Collections.Concurrent;
using System.Linq;
using MoreLinq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using Assignment.Data;

namespace Assignment
{
    public class Fetcher : IDisposable
    {
        private readonly IWoonObjectBron bron;
        private readonly IFetchProgress progress;
        private readonly BlockingCollection<WoonObject[]> outputQueue;
        private readonly ConcurrentQueue<int> paginaNummerWachtrij;

        enum State { WaitingAMinute, Active }
        private volatile State state = State.Active;

        public Fetcher(IWoonObjectBron bron, IFetchProgress progress, BlockingCollection<WoonObject[]> outputQueue)
        {
            this.bron = bron;
            this.progress = progress;
            this.outputQueue = outputQueue;
            this.paginaNummerWachtrij = new ConcurrentQueue<int>();
        }

        public void Dispose() => bron.Dispose();

        public async Task FetchAllAsync(CancellationToken cancellationToken)
        {
            var eerstePagina = await bron.HaalPagina(1, cancellationToken);
            var aantalPaginas = eerstePagina.Paging.AantalPaginas;
            if (aantalPaginas < 1)
            {
                throw new UnexpectedApiResponseException("Incorrect number of pages was returned: " + aantalPaginas);
            }
            progress.PagesComplete = new bool[aantalPaginas];
            progress.PagesComplete[0] = true;
            outputQueue.Add(eerstePagina.Objects);

            foreach (var paginaNummer in Enumerable.Range(2, eerstePagina.Paging.AantalPaginas - 1))
            {
                paginaNummerWachtrij.Enqueue(paginaNummer);
            }

            List<Task> lopendeTaken = new List<Task>();
            int totaalAantalBatchesStopconditie = 1000;
            while (state == State.Active && --totaalAantalBatchesStopconditie > 0)
            {
                for (var batchCounter = 0; batchCounter < 99; batchCounter++)
                {
                    if (state == State.WaitingAMinute)
                    {
                        break;
                    }
                    if (!paginaNummerWachtrij.TryDequeue(out var paginaNummer))
                    {
                        break;
                    }
                    lopendeTaken.Add(Task.Factory.StartNew(async (object paginaNummerBoxed) =>
                    {
                        int paginaNummerUnboxed = (int)paginaNummerBoxed;
                        if (state == State.WaitingAMinute)
                        {
                            paginaNummerWachtrij.Enqueue(paginaNummer);
                            return;
                        }
                        try
                        {
                            var fundaResultaat = await bron.HaalPagina(paginaNummer, cancellationToken);
                            var woonObjecten = fundaResultaat.Objects;
                            progress.PagesComplete[paginaNummer - 1] = true;
                            outputQueue.Add(woonObjecten);
                        }
                        catch (RequestLimitExceededException)
                        {
                            paginaNummerWachtrij.Enqueue(paginaNummer);
                            state = State.WaitingAMinute;
                        }
                    }, paginaNummer));
                }
                await Task.WhenAll(lopendeTaken);
                var exceptions = lopendeTaken.Where(x => x.IsFaulted).Select(x => x.Exception).ToArray();
                if (exceptions.Any())
                {
                    throw new AggregateException(exceptions);
                }
                // if this was not the last round, we need to wait until a minute before we can do more API calls
                if (paginaNummerWachtrij.Any())
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
                state = State.Active;
            }
        }
    }
}
