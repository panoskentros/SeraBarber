using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SeraBarber.BusinessObjects
{
    [Supabase.Postgrest.Attributes.Table("appointments")]
    public class Appointment : BaseModel
    {
        public Appointment()
        {
            
        }
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Supabase.Postgrest.Attributes.Column("user_id")]
        public Guid UserId { get; set; }

        [Supabase.Postgrest.Attributes.Column("day")]
        public DateOnly Day { get; set; }
        [Supabase.Postgrest.Attributes.Column("time")]
        public TimeSpan Time { get; set; }
        
        
        [NotMapped]
        [JsonIgnore]
        public DateTime Start => Day.ToDateTime(TimeOnly.FromTimeSpan(Time));

        [NotMapped]
        [JsonIgnore]
        public DateTime End => Start.AddMinutes(30);



        [Supabase.Postgrest.Attributes.Column("description")]
        public string Description { get; set; }

        [Supabase.Postgrest.Attributes.Column("username")]
        public string Username { get; set; }

        [Supabase.Postgrest.Attributes.Column("email")]
        public string Email { get; set; }

        [Supabase.Postgrest.Attributes.Column("phone_number")]
        public string PhoneNumber { get; set; }

        [Supabase.Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}