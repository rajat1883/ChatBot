using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using SimpleEchoBot.Model;
using SimpleEchoBot.CardManager;
using Microsoft.Bot.Builder.FormFlow;
using SimpleEchoBot.Forms;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    [LuisModel("69341f37-3e60-44a8-8aa6-e8d1666507ba", "a22a76ea0d6049f5922e268c385d13c1")]
    public partial class RootLuisDialog : LuisDialog<object>
    {
        #region Properties
        Dictionary<DateTime?, string> nagarroPublicHolidays = new Dictionary<DateTime?, string>();
        Dictionary<DateTime?, string> nagarroFlexibleHolidays = new Dictionary<DateTime?, string>();
        Dictionary<DateTime?, string> appliedOptionalHolidays = new Dictionary<DateTime?, string>();
        Dictionary<DateTime?, string> appliedLeaves = new Dictionary<DateTime?, string>();
        public bool leaveAlreadySelectedFromCurrentCard = false;
        PublicHoliday holidayData;
        int numberOfLeavesApplied = 0;
        private bool periodEnteredByUser;
        HeroCardManager heroCardManager = new HeroCardManager();
        LeaveRequestBasicForm leaveRequestBasicForm = new LeaveRequestBasicForm();
        #endregion

        public RootLuisDialog()
        {
            //Initialize list of public holidays
            nagarroPublicHolidays.Add(DateTime.Parse("01/01/2019"), "New Year's Day");
            nagarroPublicHolidays.Add(DateTime.Parse("01/26/2019"), "Republic Day");
            nagarroPublicHolidays.Add(DateTime.Parse("02/10/2019"), "Vasant Panchami");
            nagarroPublicHolidays.Add(DateTime.Parse("03/21/2019"), "Holi");
            nagarroPublicHolidays.Add(DateTime.Parse("04/13/2019"), "Ram Navami");
            nagarroPublicHolidays.Add(DateTime.Parse("04/14/2019"), "Vaisakhi");
            nagarroPublicHolidays.Add(DateTime.Parse("08/15/2019"), "Independence Day");
            nagarroPublicHolidays.Add(DateTime.Parse("08/24/2019"), "Janmashtami");
            nagarroPublicHolidays.Add(DateTime.Parse("10/02/2019"), "Gandhi Jayanti");
            nagarroPublicHolidays.Add(DateTime.Parse("10/08/2019"), "Dussehra");
            nagarroPublicHolidays.Add(DateTime.Parse("10/27/2019"), "Diwali");
            nagarroPublicHolidays.Add(DateTime.Parse("10/28/2019"), "Diwali");
            nagarroPublicHolidays.Add(DateTime.Parse("12/25/2019"), "Christmas");

            //Initialize list of flexible holidays
            nagarroFlexibleHolidays.Add(DateTime.Parse("01/14/2019"), "Makar Sankranti");
            nagarroFlexibleHolidays.Add(DateTime.Parse("01/15/2019"), "Pongal");
            nagarroFlexibleHolidays.Add(DateTime.Parse("03/04/2019"), "Maha Shivaratri");
            nagarroFlexibleHolidays.Add(DateTime.Parse("04/19/2019"), "Good Friday");
            nagarroFlexibleHolidays.Add(DateTime.Parse("05/24/2019"), "Nagarro's Day of Reason");
            nagarroFlexibleHolidays.Add(DateTime.Parse("06/05/2019"), "Idul Fitr*");
            nagarroFlexibleHolidays.Add(DateTime.Parse("08/12/2019"), "Idul Juha*");
            nagarroFlexibleHolidays.Add(DateTime.Parse("09/02/2019"), "Ganesh Chaturthi");
            nagarroFlexibleHolidays.Add(DateTime.Parse("09/11/2019"), "Onam");
            nagarroFlexibleHolidays.Add(DateTime.Parse("10/29/2019"), "Bhai Dooj");
            nagarroFlexibleHolidays.Add(DateTime.Parse("11/12/2019"), "Guru Nanak Jayanti");
        }

        #region Luis Intents
        //This intent is for handling the user request for upcoming public holidays
        [LuisIntent("ListUpcomingPublicHolidays")]
        public async Task ListUpcomingPublicHolidays(IDialogContext context, LuisResult result)
        {
            var holidayData = ExtractFilteredHolidayList(context, result, nagarroPublicHolidays);

            //Forward the result
            await this.BuildAdaptiveCard(context, result, holidayData, "holidays");
        }
        
        //This intent is for handling the user request for upcoming flexible holidays
        [LuisIntent("ListUpcomingFlexibleHolidays")]
        public async Task ListUpcomingFlexibleHolidays(IDialogContext context, LuisResult result)
        {
            var holidayData = ExtractFilteredHolidayList(context, result, nagarroFlexibleHolidays);

            //Forward the result
            await this.BuildHeroCard(context, result, holidayData);
        }

        //This intent is for handling the user selection from flexible holiday list
        [LuisIntent("UserSelectedFlexibleHoliday")]
        public async Task UserSelectedFlexibleHoliday(IDialogContext context, LuisResult result)
        {
            context.UserData.TryGetValue<HeroCardManager>("heroCardManager", out heroCardManager);
            context.UserData.TryGetValue<Dictionary<DateTime?,string>>("appliedOptionalHolidays", out appliedOptionalHolidays);
            context.UserData.TryGetValue<bool>("leaveAlreadySelectedFromCurrentCard", out leaveAlreadySelectedFromCurrentCard);

            if (heroCardManager == null)
            {
                heroCardManager = new HeroCardManager();
            }
            if (appliedOptionalHolidays == null)
            {
                appliedOptionalHolidays = new Dictionary<DateTime?, string>();
            }
            EntityRecommendation Selected_Optional_Holiday, Hero_Card_Id;
            DateTime selectedOptionalHoliday = new DateTime();
            int heroCardId = 0;

            if (result.TryFindEntity("HeroCardId", out Hero_Card_Id))
            {
                Hero_Card_Id.Type = "Hero_Card_Id";
            }

            if (result.TryFindEntity("builtin.datetimeV2.datetime", out Selected_Optional_Holiday))
            {
                Selected_Optional_Holiday.Type = "Selected_Optional_Holiday";
            }

            if (Selected_Optional_Holiday != null && Selected_Optional_Holiday.Type == "Selected_Optional_Holiday")
            {
                var resolutionValues = (IList<object>)Selected_Optional_Holiday.Resolution["values"];
                selectedOptionalHoliday = Convert.ToDateTime(((IDictionary<string, object>)resolutionValues.Last())["value"]);
            }

            if (Hero_Card_Id != null && Hero_Card_Id.Type == "Hero_Card_Id")
            {
                heroCardId = Convert.ToInt32(Hero_Card_Id.Entity);
            }

            if (heroCardId == heroCardManager.currentHeroCardId && !leaveAlreadySelectedFromCurrentCard)
            {
                if (!appliedOptionalHolidays.ContainsKey(selectedOptionalHoliday))
                {
                    if (appliedOptionalHolidays.Count < 2)
                    {
                        if(selectedOptionalHoliday >= DateTime.Today)
                        {
                            appliedOptionalHolidays.Add(selectedOptionalHoliday, nagarroFlexibleHolidays[selectedOptionalHoliday]);
                            await context.PostAsync("You have successfully applied flexible holiday for : " + nagarroFlexibleHolidays[selectedOptionalHoliday] + "(" + selectedOptionalHoliday.ToString("d") + ")");
                            leaveAlreadySelectedFromCurrentCard = true;
                        }
                        else
                        {
                            await context.PostAsync("You cannot apply flexible holiday for past date. Please opt for flexible holiday from future date.");
                        }
                    }
                    else
                    {
                        await context.PostAsync("You have availed maximum of 2 flexible holidays. You cannot avail anymore holidays.");

                    }
                }
                else
                {
                    await context.PostAsync("You have already selected this flexible holiday.Please select a different holiday.");

                }
            }
            else
            {
                await context.PostAsync("You have already selected an flexible holiday from this card. Please request new options to avail another holiday.");
            }
            context.UserData.SetValue<HeroCardManager>("heroCardManager", heroCardManager);
            context.UserData.SetValue<Dictionary<DateTime?, string>>("appliedOptionalHolidays", appliedOptionalHolidays);
            context.UserData.SetValue<bool>("leaveAlreadySelectedFromCurrentCard", leaveAlreadySelectedFromCurrentCard);
        }

        //This intent is for handling leave application requests
        [LuisIntent("LeaveRequest")]
        public async Task LeaveRequest(IDialogContext context, LuisResult result)
        {
            PublicHoliday leaveDuration = ExtractDataFromDateEntities(context, result);
            if (leaveDuration.IsInputSingle)
            {
                var leaveRequestFormDialog = new FormDialog<LeaveRequestBasicForm>
                                         (new LeaveRequestBasicForm(leaveDuration.Single_Date,leaveDuration.Single_Date),
                                         leaveRequestBasicForm.BuildForm,
                                         FormOptions.PromptInStart);
                context.Call(leaveRequestFormDialog, AddRequestedLeaves);
            }
            else if(leaveDuration.IsInputRange)
            {
                var leaveRequestFormDialog = new FormDialog<LeaveRequestBasicForm>
                                         (new LeaveRequestBasicForm(leaveDuration.Start_Date,leaveDuration.End_Date),
                                         leaveRequestBasicForm.BuildForm,
                                         FormOptions.PromptInStart);
                context.Call(leaveRequestFormDialog, AddRequestedLeaves);
            }
            else
            {
                var leaveRequestFormDialog = new FormDialog<LeaveRequestBasicForm>
                                         (new LeaveRequestBasicForm(),
                                         leaveRequestBasicForm.BuildForm,
                                         FormOptions.PromptInStart);
                context.Call(leaveRequestFormDialog, AddRequestedLeaves);
            }
        }

        //This intent is for hadling the user requests to view their submitted requests for flexible holidays and leaves
        [LuisIntent("ListOfAppliedFlexibleHolidaysOrLeaves")]
        public async Task ListOfAppliedFlexibleHolidaysOrLeaves(IDialogContext context, LuisResult result)
        {
            context.UserData.TryGetValue<Dictionary<DateTime?, string>>("appliedLeaves", out appliedLeaves);
            context.UserData.TryGetValue<Dictionary<DateTime?, string>>("appliedOptionalHolidays", out appliedOptionalHolidays);
            if (appliedLeaves == null)
            {
                appliedLeaves = new Dictionary<DateTime?, string>();
            }
            if (appliedOptionalHolidays == null)
            {
                appliedOptionalHolidays = new Dictionary<DateTime?, string>();
            }
            //PublicHoliday duration = ExtractDataFromDateEntities(context, result);
            EntityRecommendation FlexibleHolidayOrLeave;
            string IsFlexibleHolidayOrLeave = "";
            if (result.TryFindEntity("FlexibleHolidayOrLeave", out FlexibleHolidayOrLeave))
            {
                FlexibleHolidayOrLeave.Type = "FlexibleHolidayOrLeave";
            }

            if (FlexibleHolidayOrLeave != null && FlexibleHolidayOrLeave.Type == "FlexibleHolidayOrLeave")
            {
                IsFlexibleHolidayOrLeave = FlexibleHolidayOrLeave.Entity;
            }

            if(IsFlexibleHolidayOrLeave.ToLower() == "flexible" || IsFlexibleHolidayOrLeave.ToLower() == "optional" || IsFlexibleHolidayOrLeave.ToLower() == "flexi")
            {
                var holidayData = ExtractFilteredHolidayList(context, result, appliedOptionalHolidays);
                await this.BuildAdaptiveCard(context, result, holidayData, "flexible holidays");
            }
            else if(IsFlexibleHolidayOrLeave.ToLower() == "leave" || IsFlexibleHolidayOrLeave.ToLower() == "leaves")
            {
                var leaveData = ExtractFilteredHolidayList(context, result, appliedLeaves);
                await this.BuildAdaptiveCard(context, result, leaveData, "leaves");
            }
            else
            {
                var holidayData = ExtractFilteredHolidayList(context, result, appliedOptionalHolidays);
                await this.BuildAdaptiveCard(context, result, holidayData, "flexible holidays");

                var leaveData = ExtractFilteredHolidayList(context, result, appliedLeaves);
                await this.BuildAdaptiveCard(context, result, leaveData, "leaves");
            }
        }

        //This is for handling greeting messages by the user
        [LuisIntent("Greeting")]
        public async Task Greeting(IDialogContext context, LuisResult result)
        {
            string userName = String.Empty;
            context.UserData.TryGetValue<string>("Name", out userName);

            if (string.IsNullOrEmpty(userName))
            {
                await context.PostAsync("Hi!");
                await context.PostAsync("Welcome to Nagarro holiday and leave management bot.");
                await context.PostAsync("May I know your name please ?");
                context.Wait(GetUserName);
            }
            else
            {
                await context.PostAsync($"Hi {userName}, How may I help you ?");
                context.Done<string>("Greeting dialog completed");
            }
        }
        
        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string userName = String.Empty;
            context.UserData.TryGetValue<string>("Name", out userName);

            if (string.IsNullOrEmpty(userName))
            {
                await context.PostAsync("Hi! How may I help you?");
            }
            else
            {
                await context.PostAsync($"Hi {userName}, How may I help you ?");
            }
        }
        #endregion

    }
}