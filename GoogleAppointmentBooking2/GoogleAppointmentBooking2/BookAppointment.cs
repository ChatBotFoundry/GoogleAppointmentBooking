using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GoogleAppointmentBooking2
{
    public enum ServiceType
    {
        [Describe("Átvizsgálás")]
        Inspection,
        [Describe("Javítás")]
        Repair,
        [Describe("Gumicsere")]
        TireReplacement,
        [Describe("Futómű beállítás")]
        WheelAlignment
    };

    [Serializable]
    public class BookAppointment
    {
        [Prompt("Melyik szolgáltatásunkat szeretné igénybe venni? {||}")]
        public ServiceType? serviceType;

        [Optional]
        [Template(TemplateUsage.StatusFormat, "{&}: {:t}", FieldCase = CaseNormalization.None)]
//        [Prompt("Válasszon időpontot a következő lehetőségek közül: {||}")]
        public DateTime? requestedTime;

        public static IForm<BookAppointment> BuildForm()
        {
            return new FormBuilder<BookAppointment>()
                .Message("Üdvözlöm az Old Car Service szolgáltatónál!")
                .AddRemainingFields()
                .Message("Köszönjük jelentkezését és várjuk üzletünkben.")
                .Build();
        }
    }
}