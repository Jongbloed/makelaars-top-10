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

        public TopTienWeergaveTaak(BlockingCollection<WoonObject[]> inputQueue, IFetchProgress progress, Action<Makelaar[]> outputAction, bool useDelay)
        {
            this.queue = inputQueue;
            this.topTen = new TopTen();
            this.outputAction = outputAction;
            this.useDelay = useDelay;
            this.progress = progress;
        }

        public void Start(CancellationToken cancellationToken)
        {
            Task.Run(async() =>
            {
                while (true)
                {
                    var meerObjecten = queue.Take(cancellationToken);
                    topTen.AddWoonObjecten(meerObjecten);
                    outputAction(topTen.GetTopTen());
                    progress.Print();
                    if (useDelay)
                    {
                        try
                        {
                            await Task.Delay(300, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // stopping is enough
                        }
                    }
                }
            });
        }
    }
}
