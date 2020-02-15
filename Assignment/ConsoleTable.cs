using System;
using System.Collections.Generic;
using System.Text;

namespace Assignment
{
    class ConsoleTable
    {
        private readonly Makelaar[] content;

        public ConsoleTable(Makelaar[] content) => this.content = content;

        public void Print()
        {
            Console.SetCursorPosition(0, 0);
            Action<string> print = Console.WriteLine;

            print(new string('—', 74));
            print($"|{"Nummer".PadRight(10)}|{"Naam".PadRight(40)}|{"Aantal objecten".PadRight(20)}|");
            print($"|{new string('—', 10)}|{new string('—', 40)}|{new string('—', 20)}|");
            for (int nummer = 1; nummer <= content.Length; nummer++)
            {
                var makelaar = content[nummer - 1];
                print(
                    $@"|{nummer.ToString().PadRight(10)
                    }|{makelaar.MakelaarNaam.PadRight(40)
                    }|{makelaar.AantalListings.ToString().PadRight(20)
                }|");
            }
            print(new string('—', 74));
        }
    }
}
