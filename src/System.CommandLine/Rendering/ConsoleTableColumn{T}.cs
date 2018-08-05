using System.Collections.Generic;
using System.Linq;

namespace System.CommandLine.Rendering
{
    internal class ConsoleTableColumn<T>
    {
        private Dictionary<int, Span> _spans;

        public ConsoleTableColumn(
            Span header,
            Func<T, Span> renderCell,
            ConsoleWriter consoleWriter)
        {
            RenderCell = renderCell ?? throw new ArgumentNullException(nameof(renderCell));
            ConsoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            Header = header;
        }

        public Func<T, Span> RenderCell { get; }

        public Span Header { get; }

        public void FlushRow(
            int rowIndex,
            ConsoleWriter consoleWriter)
        {
            if (_spans == null)
            {
                return;
            }

            var span = _spans[rowIndex];

            consoleWriter.RenderToRegion(span,
                                         new Region(
                                             width: Width,
                                             height: 1,
                                             top: rowIndex,
                                             left: Left));
        }

        public int Width { get; private set; }

        public void CalculateSpans(IReadOnlyList<T> items)
        {
            _spans = new Dictionary<int, Span>();

            _spans[0] = Header;

            for (var i = 0; i < items.Count; i++)
            {
                _spans[i + 1] = RenderCell(items[i]);
            }

            Width = _spans.Values.Max(s => s.ContentLength) + Gutter;
        }

        public int Gutter { get; set; } = 2;

        public ConsoleWriter ConsoleWriter { get; }

        public int Left { get; internal set; }
    }
}
