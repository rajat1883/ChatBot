using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using SimpleEchoBot.IModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleEchoBot.Forms
{
    [Serializable]
    public class LeaveRequestBasicForm : ILeaveRequestBasicForms
    {
        public LeaveRequestBasicForm(DateTime? Start_Date = null, DateTime? End_Date = null)
        {
            this.Start_Date = Start_Date;
            this.End_Date = End_Date;
        }

        [Describe("Start Date")]
        [Prompt("Please enter {&} for leave")]
        public DateTime? Start_Date { get; set; }

        [Describe("End Date")]
        [Prompt("Please enter {&} for leave")]
        public DateTime? End_Date { get; set; }

        [Describe("Reason")]
        [Prompt("Please enter {&} for leave")]
        public string Reason { get; set; }

        [Describe("Comments")]
        [Prompt("Please enter {&} for leave")]
        public string Comments { get; set; }

        public IForm<LeaveRequestBasicForm> BuildForm()
        {
            return new FormBuilder<LeaveRequestBasicForm>().Message("Welcome to Leave applying module. Please enter the requested details to apply for leave. (Note: You can type quit to come out of leave module)").Field(nameof(Start_Date),
                validate: async(state, value) =>
                {
                    var result = new ValidateResult() { IsValid = true, Value = value };
                    if ((DateTime)value < DateTime.Today)
                    {
                        result.IsValid = false;
                        result.Feedback = "Your start date is for past date which is an invalid input. Please enter a valid date and try again.";
                    }
                    else if(((DateTime)value).Year > DateTime.Now.Year)
                    {
                        result.IsValid = false;
                        result.Feedback = "Your start date is for a past date or future year which is an invalid input. Please enter a valid date and try again.";
                    }
                    return result;
                }
                )
                .Field(nameof(End_Date),
                 validate: async (state, value) =>
                 {
                     var result = new ValidateResult() { IsValid = true, Value = value };
                     if ((DateTime)value < DateTime.Today)
                     {
                         result.IsValid = false;
                         result.Feedback = "Your end date is for past date which is an invalid input. Please enter a valid date and try again.";
                     }
                     if ((DateTime)value < state.Start_Date)
                     {
                         result.IsValid = false;
                         result.Feedback = "Your end date falls before start date which is an invalid input. Please enter a valid date and try again.";
                     }
                     if (((DateTime)value).Year > DateTime.Now.Year)
                     {
                         result.IsValid = false;
                         result.Feedback = "Your end date is for a past date or future year which is an invalid input. Please enter a valid date and try again.";
                     }
                     //if ((((DateTime)value - (DateTime)state.Start_Date).TotalDays + 1) > numberOfLeavesLeft)
                     //{
                     //    result.IsValid = false;
                     //    result.Feedback = "You have " + numberOfLeavesLeft + " leaves left. Please try again with less number of leaves.";
                     //}
                     return result;
                 }
                )
                .Field(nameof(Reason))
                .Field(nameof(Comments))
                .Confirm("Is the information entered is correct ? \n {*filled}")
                .OnCompletion(async (context, profileForm) =>
                {
                    // Tell the user that the form is complete  
                    await context.PostAsync("Thanks for providing the information.");
                })
                .Build();
        }
    }
}