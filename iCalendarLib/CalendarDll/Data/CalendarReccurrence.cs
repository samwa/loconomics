//------------------------------------------------------------------------------
// <auto-generated>
//    Este código se generó a partir de una plantilla.
//
//    Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//    Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CalendarDll.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class CalendarReccurrence
    {
        public CalendarReccurrence()
        {
            this.CalendarReccurrenceFrequency = new HashSet<CalendarReccurrenceFrequency>();
        }
    
        public int ID { get; set; }
        public Nullable<int> EventID { get; set; }
        public Nullable<int> Count { get; set; }
        public string EvaluationMode { get; set; }
        public Nullable<int> Frequency { get; set; }
        public Nullable<int> Interval { get; set; }
        public Nullable<int> RestristionType { get; set; }
        public Nullable<System.DateTimeOffset> Until { get; set; }
        public Nullable<int> FirstDayOfWeek { get; set; }
    
        public virtual ICollection<CalendarReccurrenceFrequency> CalendarReccurrenceFrequency { get; set; }
        public virtual CalendarEvents CalendarEvents { get; set; }
    }
}
