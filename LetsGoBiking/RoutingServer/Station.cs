using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RoutingServer
{

    public class Station
    {
        public int number { get; set; }
        public string contract_name { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public Position position { get; set; }
        public string status { get; set; }
        public int available_bike_stands { get; set; }
        public int available_bikes {  get; set; }

    }

    public class Position
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }


}
