using System;
using System.Linq;

namespace Assignment
{
    public interface IFetchProgress
    {
        void Print();
        bool[] PagesComplete { get; set; }
    }

    class FetchProgress : IFetchProgress
    {
        public void Print()
        {
            Console.SetCursorPosition(2, 16);
            Console.Write($"|{new string(PagesComplete.Select(b => b ? '×' : ' ').ToArray())}| {GetProgressPercentageString()}");
        }
        public bool[] PagesComplete { get; set; } = new bool[0];
        string GetProgressPercentageString() =>
            $"{PagesComplete.Count(x => x) / (float)PagesComplete.Length:P0} geladen...";
    }
}
