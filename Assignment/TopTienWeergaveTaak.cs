using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Assignment
{
    class TopTienWeergaveTaak
    {
        private readonly BlockingCollection<WoonObject[]> queue;
        private readonly TopTen topTen;
        private readonly Action<Makelaar[]> outputAction;
        private readonly bool useDelay;
        private readonly IFetchProgress progress;
        private readonly CancellationToken cancellationToken;

        public TopTienWeergaveTaak(BlockingCollection<WoonObject[]> inputQueue, IFetchProgress progress, Action<Makelaar[]> outputAction, bool useDelay, CancellationToken cancellationToken)
        {
            this.queue = inputQueue;
            this.topTen = new TopTen();
            this.outputAction = outputAction;
            this.useDelay = useDelay;
            this.progress = progress;
            this.cancellationToken = cancellationToken;
        }

        public void Start()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    var meerObjecten = queue.Take();
                    topTen.AddWoonObjecten(meerObjecten);
                    outputAction(topTen.GetTopTen());
                    progress.Print();
                    if (useDelay)
                    {
                        try
                        {
                            Task.Delay(300, cancellationToken).Wait();
                        }
                        catch (AggregateException)
                        {
                            // stopping is enough
                        }
                    }
                }
            });
        }
    }
}
