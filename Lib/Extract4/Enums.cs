using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extract4
{
    public enum Source
    {
        YahooJP,
        YahooUS,
    }

    public enum Field
    {
        OPEN,
        HIGH,
        LOW,
        CLOSE,

        ASK,
        BID,

        ASK_SIZE,
        BID_SIZE,

        LAST,
        LAST_SEIZE,

        VOLUME
    }
}
