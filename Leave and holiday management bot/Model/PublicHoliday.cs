using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SimpleEchoBot.IModel;

namespace SimpleEchoBot.Model
{
    [Serializable]
    public class PublicHoliday : IPublicHoliday
    {
        public PublicHoliday()
        {
            IsInputRange = false;
            IsInputSingle = false;
        }
        public DateTime? Start_Date { get; set; }
        public DateTime? End_Date { get; set; }
        public DateTime? Single_Date { get; set; }
        public bool IsInputRange { get; set; }
        public bool IsInputSingle { get; set; }
    }
}