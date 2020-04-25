using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEchoBot.IModel
{
    interface IPublicHoliday
    {
        DateTime? Start_Date { get; set; }
        DateTime? End_Date { get; set; }
        DateTime? Single_Date { get; set; }
        bool IsInputRange { get; set; }
        bool IsInputSingle { get; set; }
    }
}
