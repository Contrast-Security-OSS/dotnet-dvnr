using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ContrastDvnr
{
    public class DvnrTraceListener : TextWriterTraceListener
    {
        public DvnrTraceListener(string fileName, string name) : base(fileName, name)
        {
        }

        public override void WriteLine(string message)
        {
            string adornedMessage = string.Format("{0}, {1}", DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture), message);
            base.WriteLine(adornedMessage);
        }

    }
}
