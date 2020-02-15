using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Assignment
{
    class TopTienWeergaveTaak
    {
        private readonly BlockingCollection<WoonObject[]> queue;
        private readonly TopTen topTen;
        private readonly Action<Makelaar[]> outputAction;
        public bool Slow;
        private Task task;
        private bool run;

        public TopTienWeergaveTaak(BlockingCollection<WoonObject[]> queue, Action<Makelaar[]> outputAction)
        {
            this.queue = queue;
            this.topTen = new TopTen();
            this.outputAction = outputAction;
        }

        public void Stop() => run = false; // TODO cancellationToken would be nicer

        public void Start()
        {
            run = true;
            this.task = Task.Run(() =>
            {
                while (run)
                {
                    var meerObjecten = queue.Take();
                    topTen.AddListings(meerObjecten);
                    outputAction(topTen.GetTopTen());
                    if (Slow)
                    {
                        Task.Delay(300).Wait();
                    }
                }
            });
        }
    }
}
