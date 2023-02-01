using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SorterLibrary.Model
{
    public readonly struct RowModel
    {
        public string Value { get; init; }
        public int StreamReader { get; init; }
        public int Number { get; init; }
        public string Text { get; init; }
    }
}
