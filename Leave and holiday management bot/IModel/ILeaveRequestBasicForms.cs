using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEchoBot.IModel
{
    interface ILeaveRequestBasicForms
    {
        DateTime? Start_Date { get; set; }
        DateTime? End_Date { get; set; }
        string Reason { get; set; }
    }
}
