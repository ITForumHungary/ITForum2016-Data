using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingPolice.Windows.Models
{
    public class HumidityDataPoint
    {
        public HumidityDataPoint()
        {
            RowKey = Guid.NewGuid().ToString();
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public double Humidity { get; set; }
    }
}
