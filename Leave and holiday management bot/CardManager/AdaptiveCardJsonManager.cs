using AdaptiveCards;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace SimpleEchoBot.CardManager
{
    public class AdaptiveCardJsonManager
    {
        public static IEnumerable<Attachment> CreateHolidayCards(Dictionary<DateTime?, string> holidayData)
        {
            List<Attachment> attachments = new List<Attachment>();
            string pathToFiles = HttpContext.Current.Server.MapPath("~/AdaptiveCardJson/HolidayList.json");
            foreach (var holiday in holidayData)
            {
                string adaptiveCardJson = FillAdaptiveCard(holiday, pathToFiles);
                AdaptiveCard adaptiveCard = JsonConvert.DeserializeObject<AdaptiveCard>(adaptiveCardJson);

                Attachment attachment = new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = adaptiveCard
                };
                attachments.Add(attachment);
            }
            return attachments;
        }

        private static string FillAdaptiveCard(KeyValuePair<DateTime?, string> holiday, string pathToFiles)
        {
            StringBuilder adaptiveCardJsonToModify = new StringBuilder(File.ReadAllText(pathToFiles));
            adaptiveCardJsonToModify.Replace("{holiday_date}",holiday.Key.Value.Date.ToString("d"));
            adaptiveCardJsonToModify.Replace("{holiday_description}",holiday.Value);
            return adaptiveCardJsonToModify.ToString();
        }
    }
}