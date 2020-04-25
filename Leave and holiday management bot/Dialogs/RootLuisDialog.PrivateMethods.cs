using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using SimpleEchoBot.CardManager;
using SimpleEchoBot.Forms;
using SimpleEchoBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SimpleEchoBot.Dialogs
{
    public partial class RootLuisDialog
    {

        #region Private Methods
        private async Task GetUserName(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            if (string.IsNullOrEmpty(activity.Text))
            {
                await context.PostAsync("Please enter a valid name.");
                context.Wait(GetUserName);
            }
            else
            {
                context.UserData.SetValue<string>("Name", activity.Text);
                await context.PostAsync($"Happy to see you {activity.Text} :)");
                await context.PostAsync($"Here is what all i can help you with : \n 1. List Nagarro's public holidays \n 2. List Nagarro's flexible holidays and option to opt for one \n 3. Submit a request for leave \n 4. View submitted requests for flexible holiday or leave");
                await context.PostAsync("How may i help you?");
                context.Done<string>("Greeting dialog completed");
            }
        }
        private async Task AddRequestedLeaves(IDialogContext context, IAwaitable<LeaveRequestBasicForm> result)
        {
            try
            {
                context.UserData.TryGetValue<Dictionary<DateTime?, string>>("appliedLeaves", out appliedLeaves);
                context.UserData.TryGetValue<Dictionary<DateTime?, string>>("appliedOptionalHolidays", out appliedOptionalHolidays);
                if(appliedLeaves == null)
                {
                    appliedLeaves = new Dictionary<DateTime?, string>();
                }
                if (appliedOptionalHolidays == null)
                {
                    appliedOptionalHolidays = new Dictionary<DateTime?, string>();
                }
                var leaveRequestForm = await result;
                numberOfLeavesApplied = 0;
                if(((((DateTime)leaveRequestForm.End_Date - (DateTime)leaveRequestForm.Start_Date).TotalDays + 1) + appliedLeaves.Count) <= 27)
                {
                    for (DateTime date = (DateTime)leaveRequestForm.Start_Date; date.Date <= ((DateTime)leaveRequestForm.End_Date).Date; date = date.AddDays(1))
                    {
                        bool isWeekend = date.DayOfWeek.ToString() == "Saturday" || date.DayOfWeek.ToString() == "Sunday";
                        bool isPublicHoliday = nagarroPublicHolidays.ContainsKey(date);
                        bool isOptionalHolidayAndAlreadyApplied = appliedOptionalHolidays.ContainsKey(date);
                        bool isLeaveAlreadyApplied = appliedLeaves.ContainsKey(date);
                        if (isLeaveAlreadyApplied)
                        {
                            await context.PostAsync("We have skipped " + date.ToString("d") + " because you have already applied leave on this day. " + appliedLeaves[date]);
                        }
                        if (isPublicHoliday)
                        {
                            await context.PostAsync("We have skipped " + date.ToString("d") + " because it is a public holiday : " + nagarroPublicHolidays[date]);
                        }
                        if (isOptionalHolidayAndAlreadyApplied && !isPublicHoliday)
                        {
                            await context.PostAsync("We have skipped " + date.ToString("d") + " because it is an optional holiday already availed by you : " + appliedOptionalHolidays[date]);
                        }
                        if (isWeekend && !isOptionalHolidayAndAlreadyApplied && !isPublicHoliday)
                        {
                            await context.PostAsync("We have skipped " + date.ToString("d") + " because it is " + date.DayOfWeek.ToString());
                        }
                        if (!isLeaveAlreadyApplied && !isPublicHoliday && !isOptionalHolidayAndAlreadyApplied && !isWeekend)
                        {
                            numberOfLeavesApplied++;
                            appliedLeaves[date] = "Reason : " + leaveRequestForm.Reason + " | Comments : " + leaveRequestForm.Comments;
                        }
                    }
                    if (numberOfLeavesApplied > 0)
                    {
                        string leaves = numberOfLeavesApplied == 1 ? "leave" : "leaves";
                        await context.PostAsync("You have successfully applied for " + numberOfLeavesApplied + " " + leaves);
                        numberOfLeavesApplied = 0;
                    }
                }
                else
                {
                    await context.PostAsync("You have " + (27 - appliedLeaves.Count) + " leaves left.Please try again with less number of leaves");
                }
                
                context.UserData.SetValue<Dictionary<DateTime?, string>>("appliedLeaves", appliedLeaves);
            }
            catch (FormCanceledException<LeaveRequestBasicForm> e)
            {
                string reply;
                if (e.InnerException == null)
                {
                    reply = $"You've opted to quit the form. Please let me know how can I help you further?";

                }
                else
                {
                    reply = $"Sorry, I've had a short circuit.  Please try again.";
                }
                context.Done(true);
                await context.PostAsync(reply);
            }
        }
        private Dictionary<DateTime?, string> ExtractFilteredHolidayList(IDialogContext context, LuisResult result, Dictionary<DateTime?, string> holidayList)
        {
            Dictionary<DateTime?, string> filteredHolidayData = new Dictionary<DateTime?, string>();
            PublicHoliday holidayData = ExtractDataFromDateEntities(context, result);

            if (holidayData.IsInputRange == true)
            {
                if (holidayData.End_Date == null)
                {
                    filteredHolidayData = holidayList.Where(x => x.Key >= holidayData.Start_Date).ToDictionary(i => i.Key, i => i.Value);
                }
                else if (holidayData.Start_Date == null)
                {
                    filteredHolidayData = holidayList.Where(x => x.Key <= holidayData.End_Date).ToDictionary(i => i.Key, i => i.Value);
                }
                else
                {
                    filteredHolidayData = holidayList.Where(x => x.Key >= holidayData.Start_Date && x.Key <= holidayData.End_Date).ToDictionary(i => i.Key, i => i.Value);
                }
                periodEnteredByUser = true;
            }

            if (holidayData.IsInputSingle == true)
            {
                if (holidayList.ContainsKey(holidayData.Single_Date))
                {
                    filteredHolidayData[holidayData.Single_Date] = holidayList[holidayData.Single_Date];
                }
            }

            if (holidayData.IsInputRange == false && holidayData.IsInputSingle == false)
            {
                filteredHolidayData = holidayList;
            }

            return filteredHolidayData;
        }
        public PublicHoliday ExtractDataFromDateEntities(IDialogContext context, LuisResult result)
        {
            holidayData = new PublicHoliday();

            EntityRecommendation Holiday_Date, Holiday_Date_Range;

            //Extracts date range entity from luis result : This case is for user input for a range of dates
            if (result.TryFindEntity("builtin.datetimeV2.daterange", out Holiday_Date_Range))
            {
                holidayData.IsInputRange = true;
                var resolutionValues = (IList<object>)Holiday_Date_Range.Resolution["values"];
                var lastRange = (IDictionary<string, object>)resolutionValues.Last();
                holidayData.Start_Date = (lastRange).ContainsKey("start") ? Convert.ToDateTime((lastRange)["start"]) : (DateTime?)null;
                holidayData.End_Date = (lastRange).ContainsKey("end") ? Convert.ToDateTime((lastRange)["end"]) : (DateTime?)null;
            }

            //Extracts single date entity from luis result : This case is for user input for a specific date
            if (result.TryFindEntity("builtin.datetimeV2.date", out Holiday_Date))
            {
                holidayData.IsInputSingle = true;
                var resolutionValues = (IList<object>)Holiday_Date.Resolution["values"];
                holidayData.Single_Date = Convert.ToDateTime(((IDictionary<string, object>)resolutionValues.Last())["value"]);
            }

            return holidayData;
        }
        private async Task BuildAdaptiveCard(IDialogContext context, LuisResult result, Dictionary<DateTime?, string> holidayData, string holidayOrLeave)
        {
            var message = "";
            if (holidayData.Count() == 0 && holidayOrLeave == "flexible holidays")
            {
                message = "You have not applied for any flexible holiday";
                if (periodEnteredByUser)
                {
                    message += " for this period";
                    periodEnteredByUser = false;
                }
            }
            else if (holidayData.Count() == 0 && holidayOrLeave == "leaves")
            {
                message = "You have not applied for any leaves";
                if (periodEnteredByUser)
                {
                    message += " for this period";
                    periodEnteredByUser = false;
                }
            }
            else
            {
                message = $"I found in total {holidayData.Count()} " + holidayOrLeave + " as per your request : ";
            }

            await context.PostAsync(message);
            var resultMessage = context.MakeMessage();
            resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            resultMessage.Attachments = new List<Attachment>();
            resultMessage.Attachments = AdaptiveCardJsonManager.CreateHolidayCards(holidayData).ToList();
            await context.PostAsync(resultMessage);
        }
        private async Task BuildHeroCard(IDialogContext context, LuisResult result, Dictionary<DateTime?, string> holidayData)
        {
            await context.PostAsync($"I found in total {holidayData.Count()} holidays as per your request : ");
            var resultMessage = context.MakeMessage();
            resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            Attachment attachment = heroCardManager.CreateOptionalHolidayCards(holidayData,context);
            resultMessage.Attachments = new List<Attachment>() { attachment };
            await context.PostAsync(resultMessage);
        }

        #endregion
    }
}