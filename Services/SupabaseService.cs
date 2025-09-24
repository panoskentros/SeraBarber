using Supabase;
using Supabase.Gotrue;
using Supabase.Postgrest;
using Supabase.Postgrest.Models;
using SeraBarber.BusinessObjects;
using Supabase.Postgrest.Responses;
using Client = Supabase.Client;
using Constants = Supabase.Gotrue.Constants;

namespace SeraBarber.Services
{
    public class SupabaseService
    {
        private readonly Supabase.Client client;
        private readonly Supabase.Client adminClient;
        public SupabaseService()
        {
            client = new Supabase.Client(
                "https://inlckqwawmphyowwllir.supabase.co", // replace with your Supabase URL
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImlubGNrcXdhd21waHlvd3dsbGlyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTg1NTk4OTksImV4cCI6MjA3NDEzNTg5OX0.OxFodXckQwJkdeupxugRw3sWttRIJLjqSZ_rBS4xS7g" // replace with your Supabase anon key
            );
            adminClient = new Supabase.Client("https://inlckqwawmphyowwllir.supabase.co",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImlubGNrcXdhd21waHlvd3dsbGlyIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1ODU1OTg5OSwiZXhwIjoyMDc0MTM1ODk5fQ.dkDRUek1vPUdoBgu5DunjtFgGiOYkhc_YpsHGU88TWY");
        }
        public User? GetCurrentUser() => client.Auth.CurrentUser;
        public bool? IsAdminUser()
        {
            var currentUser = client.Auth.CurrentUser;
            if(currentUser == null) return null;
            if (currentUser.UserMetadata["role"].Equals("admin")) return true;
            return false;

        }

        public Supabase.Client Client => client;

        /// <summary>
        /// Initialize the Supabase client.
        /// Call this once at app startup or after login.
        /// </summary>
        public async Task InitializeAsync()
        {
            await client.InitializeAsync();
            await adminClient.InitializeAsync();
        }
        /// <summary>
        /// Register a new user with email, password, and optional username.
        /// Returns the User object if successful.
        /// </summary>

        public async Task<User?> RegisterAsync(string email, string password, string username, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.", nameof(password));
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required.", nameof(username));
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

            try
            {
                // Prepare metadata
                var options = new SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        { "username", username },
                        { "phone_number", phoneNumber },
                        {"display_name", username },
                        {"email_address", email },
                        {"role","user"},
                    }
                };

                // email sign up
                var response = await client.Auth.SignUp(
                    Constants.SignUpType.Email,
                    email,
                    password,
                    options
                );

                if (response.User != null)
                {
                    Console.WriteLine($"User registered: {response.User.Email}");
                    return response.User;
                }

                Console.WriteLine("Registration failed.");
                return null;
            }
            catch (Supabase.Gotrue.Exceptions.GotrueException ex)
            {
                Console.WriteLine($"Registration failed: {ex.Message}");
                return null;
            }
        }



        // public async Task<bool> CheckAndVerifyPhoneAsync(User user)
        // {
        //     await client.Auth.SignOut();
        //     var phone = user.UserMetadata["phone_number"]?.ToString();
        //     if (string.IsNullOrEmpty(phone))
        //     {
        //         Console.WriteLine("No phone number found for user.");
        //         return false;
        //     }
        //
        //     if (user.UserMetadata.TryGetValue("phone_verified", out var verifiedObj) &&
        //         verifiedObj?.ToString() != "true")
        //     {
        //         // Step 1: Send OTP
        //         var otpResponse = await client.Auth.SignInWithOtp(
        //             new SignInWithPasswordlessPhoneOptions(phone: phone)
        //         );
        //
        //         Console.WriteLine($"OTP sent to {phone}. Enter the code:");
        //         var otp = Console.ReadLine().Trim();
        //         if (otp == null || user.Phone == null) return false;
        //         // Step 2: Verify OTP
        //         var verifyResponse = await client.Auth.VerifyOTP(
        //             phone:user.Phone,
        //             type:Constants.MobileOtpType.SMS,
        //             token: otp);
        //
        //         if (verifyResponse?.User != null)
        //         {
        //             Console.WriteLine("Phone verified successfully!");
        //             return true;
        //         }
        //
        //         Console.WriteLine("Invalid OTP. Phone not verified.");
        //         return false;
        //     }
        //
        //     Console.WriteLine("Phone already verified.");
        //     return true;
        // }


        
        /// <summary>
        /// Log in a user with email and password.
        /// Returns the User object if successful.
        /// </summary>
        public async Task<User?> LoginAsync(string email, string password)
        {
            try
            {
                var response = await client.Auth.SignInWithPassword(email, password); 

                if (response?.User != null)
                {
                    var user = response.User;
                    Console.WriteLine($"Logged in as: {response.User.Email}");
                    
                    //await CheckAndVerifyPhoneAsync(user);
                    
                    
                    return response.User;
                }

                return null;
            }catch (Supabase.Gotrue.Exceptions.GotrueException ex)
            {
                // Check if the exception is due to email not confirmed
                if (ex.Message.Contains("email_not_confirmed"))
                {
                    Console.WriteLine("Login failed: Email not confirmed. Please check your inbox.");
                }
                else
                {
                    Console.WriteLine($"Login failed: {ex.Message}");
                }

                return null;
            }

        }
        // gets all apointments with all properties if admin
        // if user it gets all apointment dates only. not name,phone number etc
        public async Task<List<Appointment>> GetCurrentUserAppointmentsAsync()
        {
            var currentUser = client.Auth.CurrentUser;
            if (currentUser == null)
                return new List<Appointment>();

            var response = await client
                            .From<Appointment>()
                            .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, currentUser.Id) // <- Operator.Equals instead of "eq"
                            .Select("*")
                            .Get();
            
            if (response == null) throw new Exception("Response is null");
            return response.Models;
        }
        
        public async Task<List<Appointment>> GetAllAppointmentsAsync()
        {
            var currentUser = client.Auth.CurrentUser;
            if (currentUser == null)
                return new List<Appointment>();
            if (currentUser.UserMetadata["role"].Equals("admin"))
            {
                var response = await adminClient.From<Appointment>().Select("*").Get();
                return response.Models;
            }
            else
            {
                var response = await adminClient.From<Appointment>().Select("start,end,username").Get();
                return response.Models;
            }
        }
        /// <summary>
        /// Add a new appointment for the current user.
        /// RLS ensures users can only insert their own appointments.
        /// </summary>
        public async Task<Appointment?> AddAppointmentAsync(Appointment model)
        {
            var user = client.Auth.CurrentUser;
            if (user == null) return null;
            

            var appointment = new Appointment
            {
                UserId = Guid.Parse(user.Id),
                Username = user.UserMetadata["username"]?.ToString() ?? "",
                Email = user.Email,
                PhoneNumber = user.UserMetadata["phone_number"]?.ToString() ?? "",
                Day =  model.Day,
                Time = model.Time,
                Description = model.Description,
                CreatedAt = DateTime.UtcNow  // UTC timestamp
            };

            var response = await client
                .From<Appointment>()
                .Insert(appointment);

            return response.Models.FirstOrDefault();
        }


        /// <summary>
        /// Update an existing appointment.
        /// Only the owner or admin can update.
        /// </summary>
        public async Task<Appointment?> UpdateAppointmentAsync(Appointment appointment)
        {
            var currentUser = client.Auth.CurrentUser;
            if (currentUser == null)
                return null;
            if (currentUser.UserMetadata["role"].Equals("admin"))
            {
                var response = await adminClient
                    .From<Appointment>()
                    .Where(a => a.Id == appointment.Id)
                    .Update(appointment);
                return response.Models.FirstOrDefault();
            }
            else
            {
                var response = await client
                    .From<Appointment>()
                    .Where(a => a.Id == appointment.Id)
                    .Update(appointment);
                return response.Models.FirstOrDefault();
            }

        }
        public async Task<Appointment?> FindAppointmentAsync(Guid appointmentId)
        {
            var currentUser = client.Auth.CurrentUser;
            if (currentUser == null)
                return null;
            if (currentUser.UserMetadata["role"].Equals("admin")){
            // Query the appointment by ID only
            var response = await adminClient
                .From<Appointment>()
                .Where(a => a.Id == appointmentId)
                .Select("*")
                .Get();
                return response.Models.FirstOrDefault();
            }
            else
            {
                var response = await client
                    .From<Appointment>()
                    .Where(a => a.Id == appointmentId)
                    .Select("*")
                    .Get();
                return response.Models.FirstOrDefault();
            }

        }


        /// <summary>
        /// Delete an appointment by ID.
        /// Only the owner or admin can delete it.
        /// </summary>
        public async Task<bool> DeleteAppointmentAsync(Guid appointmentId)
        {
            var currentUser = client.Auth.CurrentUser;
            if (currentUser == null)
                return false;
            try
            {
                if (currentUser.UserMetadata["role"].Equals("admin"))
                {
                    await adminClient
                        .From<Appointment>()
                        .Where(a => a.Id == appointmentId)
                        .Delete();
                    return true;
                }
                else
                {
                    await client
                        .From<Appointment>()
                        .Where(a => a.Id == appointmentId)
                        .Delete();
                    return true;
                }
            }
            catch (Supabase.Gotrue.Exceptions.GotrueException ex)
            {
                return false;
            }
        }

        public bool DateTimeHasPassed(DateTime dateTime)
        {
            // Ensure comparison in UTC
            DateTime utcNow = DateTime.UtcNow;

            // If the input DateTime is not UTC, convert it
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            else
            {
                dateTime = dateTime.ToUniversalTime();
            }

            // Return true if the appointment is in the past
            return dateTime < utcNow;
        }
        public bool IsWithinWorkHours(DateTime slotStart, int workStart, int workEnd)
        {
            // Returns true if the slot is within working hours (with 1-hour buffer)
            return slotStart.Hour >= workStart && slotStart.Hour <= workEnd - 1;
        }

    }
}
