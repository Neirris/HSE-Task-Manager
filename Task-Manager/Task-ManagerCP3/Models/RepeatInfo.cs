using System;
using System.Collections.Generic;
using System.Linq;
namespace Task_ManagerCP3.Services
{
    public class RepeatInfo
    {
        public int ID { get; set; }
        public int TaskID { get; set; }
        public string RepeatType { get; set; }
        public DateTime? RepeatDateTime { get; set; }
    }
}
