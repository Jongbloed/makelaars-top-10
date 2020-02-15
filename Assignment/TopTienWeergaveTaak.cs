using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Assignment
{
    class TopTienWeergaveTaak
    {
        private readonly BlockingCollection<WoonObject[]> queue;
        private readonly TopTen topTen;
        private readonly Action<Makelaar[]> outputAction;
        private readonly bool useDelay;
        private bool run;

        public TopTienWeergaveTaak(BlockingCollection<WoonObject[]> inputQueue, Action<Makelaar[]> outputAction, bool useDelay)
        {
            this.queue = inputQueue;
            this.topTen = new TopTen();
            this.outputAction = outputAction;
            this.useDelay = useDelay;
        }

        public void Stop() => run = false; // TODO cancellationToken would be nicer

        public void Start()
        {
            run = true;
            Task.Run(() =>
            {
                while (run)
                {
                    var meerObjecten = queue.Take();
                    topTen.AddWoonObjecten(meerObjecten);
                    outputAction(topTen.GetTopTen());
                    if (useDelay)
                    {
                        Task.Delay(300).Wait();
                    }
                }
            });
        }
    }
}
