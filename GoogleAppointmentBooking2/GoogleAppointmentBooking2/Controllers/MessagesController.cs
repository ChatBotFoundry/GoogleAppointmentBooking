using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Microsoft.Bot.Builder.Dialogs;
using System.Globalization;
using System.Collections.Generic;

namespace GoogleAppointmentBooking2
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        protected int count = 1;
        protected string serviceSelected = String.Empty;
        protected string fromUser = String.Empty;

        Action<string, string, DateTime> saveEventCallback;

        private ResumptionCookie resumptionCookie;

        public EchoDialog()
        {
        }

        public EchoDialog(Action<string, string, DateTime> callback)
        {
            this.saveEventCallback = callback;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            if (this.resumptionCookie == null)
            {
                this.resumptionCookie = new ResumptionCookie(message);
            }

            await this.WelcomeMessageAsync(context);
        }

        private async Task WelcomeMessageAsync(IDialogContext context)
        {
            var reply = context.MakeMessage();

            var options = new[]
            {
                "Átvizsgálás",
                "Javítás",
                "Gumicsere",
                "Futómű beállítás"
            };

            reply.AddHeroCard(
                "Üdvözlöm a Sport Verda autószervíznél! Válasszon alábbi szolgáltatásaink  közül: ",
                //"subtitle",
                options
                //new[] { "https://placeholdit.imgix.net/~text?txtsize=56&txt=Contoso%20Flowers&w=640&h=330" }
                );

            await context.PostAsync(reply);

            context.Wait(this.OnOptionSelected);
        }

        private async Task OnOptionSelectedWeek(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var reply = context.MakeMessage();

            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            CultureInfo cul = CultureInfo.CurrentCulture;
            DateTime today = DateTime.Now;

            int daysToweekEnd = DayOfWeek.Saturday - today.DayOfWeek;

            DateTime actualDT = daysToweekEnd <= 0 ? today.AddDays(2 + daysToweekEnd) : today;
            int currWeekOfYear = cul.Calendar.GetWeekOfYear(today, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

            string[] options = new string[4];
            for (int i = 0; i < 4; i++)
            {
                int diff = DayOfWeek.Friday - actualDT.DayOfWeek;
                options[i] = String.Format("{0}. hét ({1} - {2})", currWeekOfYear, actualDT.ToShortDateString(), actualDT.AddDays(diff).ToShortDateString());

                actualDT = actualDT.AddDays(7);
                if (actualDT.DayOfWeek != DayOfWeek.Monday)
                {
                    int delta = DayOfWeek.Monday - actualDT.DayOfWeek;
                    actualDT = actualDT.AddDays(delta);
                }

                currWeekOfYear++;
            }

            reply.AddHeroCard(
                "Válassza ki az Önnek alkalmas hetet:",
                //"subtitle",
                options
                //new[] { "https://placeholdit.imgix.net/~text?txtsize=56&txt=Contoso%20Flowers&w=640&h=330" }
                );

            //await context.PostAsync(reply);
            //context.Wait(this.OnOptionSelected);

            PromptDialog.Choice(context, this.AfterDateSelected, options, "Válassza ki az Önnek alkalmas hetet: ");

            //var message = await result;
            //await context.PostAsync(string.Format(CultureInfo.CurrentCulture, "Ön ezt választotta: {0}", "valami"));
            //PromptDialog.Choice(context, this.AfterDeliveryDateSelected, new[] { "Ma", "Holnap" }, "Válasszon időpontot a következő lehetőségek közül: ");
        }

        private async Task OnOptionSelected(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            fromUser = message.From.Id;
            serviceSelected = Convert.ToString(message.Text);

            List<DateTime> eventTimes;
            //CalendarListHelper.getEvents(service, calendarId, out eventTimes);

            //var reply = context.MakeMessage();

            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            CultureInfo cul = CultureInfo.CurrentCulture;
            DateTime today = DateTime.Now;

            int daysToweekEnd = DayOfWeek.Saturday - today.DayOfWeek;

            DateTime actualDT = daysToweekEnd <= 0 ? today.AddDays(2 + daysToweekEnd) : today;
            int currWeekOfYear = cul.Calendar.GetWeekOfYear(today, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

            int diff = DayOfWeek.Friday - actualDT.DayOfWeek;
            DateTime endDay = actualDT.AddDays(diff);
            int numDays = endDay.DayOfWeek - actualDT.DayOfWeek;

            Random rnd = new Random();

            string[] options = new string[4];
            for (int i = 0; i < 4; i++)
            {
                DateTime rndDay = actualDT.AddDays(rnd.Next(numDays));
                int hour = rnd.Next(8, 16);
                DateTime propTime = new DateTime(rndDay.Year, rndDay.Month, rndDay.Day, hour, 0, 0);

                options[i] = String.Format("{0}. {1}:00 óra", rndDay.ToShortDateString(), hour);
            }

            PromptDialog.Choice(context, this.AfterDateSelected, options, "Válasszon a következő szabad időpontok közül: ");
        }

        private async Task AfterDateSelected(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;

            string summary = string.Format("{0} - Ügyfél: {1}", serviceSelected, fromUser);
            string description = string.Format("Szolgáltatás igénybevétele: {0}", serviceSelected);
            //DateTime dt = Convert.ToDateTime(Convert.ToString(message).Split('.')[0]);
            DateTime dt = Convert.ToDateTime(Convert.ToString(message).Replace("óra", String.Empty));

            this.saveEventCallback(summary, description, dt);

            //if (!String.IsNullOrEmpty(calendarId))
            //{
            //    CalendarListHelper.insertEvent(service, calendarId, summary, description, dt);
            //}
            await context.PostAsync(string.Format(CultureInfo.CurrentCulture, "Köszönjük, hogy minket választott! Várjuk Önt ezen a napon: {0}!", dt.ToShortDateString()));
        }

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                this.count = 1;
                await context.PostAsync("Reset count.");
            }
            else
            {
                await context.PostAsync("Did not reset count.");
            }
            context.Wait(MessageReceivedAsync);
        }
    }

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        static CalendarService service;
        String saEmail = "sa-appointment@chatcalendar-161113.iam.gserviceaccount.com";
        String keyFilePath = AppDomain.CurrentDomain.BaseDirectory + "ChatCalendar-ab49c2262f5f.p12";
        static String calendarId = String.Empty;

        public MessagesController()
        {
            if (service == null)
            {
                service = Authentication.AuthenticateServiceAccount(saEmail, keyFilePath);
            }
            if (String.IsNullOrEmpty(calendarId))
            {
                CalendarList cl = CalendarListHelper.list(service, null);
                calendarId = cl?.Items?[0].Id;
            }
        }

        public static void insertEvent(string summary, string description, DateTime eventStartTime)
        {
            if (service != null && !String.IsNullOrEmpty(calendarId))
            {
                CalendarListHelper.insertEvent(service, calendarId, summary, description, eventStartTime);
            }
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            // check if activity is of type message
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new EchoDialog(insertEvent));
                //await Conversation.SendAsync(activity, MakeRootDialog);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
            //var response = Request.CreateResponse(HttpStatusCode.OK);
            //return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}