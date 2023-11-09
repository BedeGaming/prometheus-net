using Prometheus.Advanced.DataContracts;
using System.Collections.Generic;

namespace Prometheus.SummaryImpl
{
    class QuantileComparer : IComparer<QuantileValue>
    {
        public int Compare(QuantileValue x, QuantileValue y)
        {
            return x.Quantile.CompareTo(y.Quantile);
        }
    }
}
