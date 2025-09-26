using Blazored.LocalStorage;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using Radzen;
using Supabase;
using Supabase.Gotrue;
using Supabase.Postgrest;
using Supabase.Postgrest.Models;
using SeraBarber.BusinessObjects;
using SeraBarber.Pages;
using Supabase.Postgrest.Responses;
using Client = Supabase.Client;
using Constants = Supabase.Gotrue.Constants;

namespace SeraBarber.Services
{
    public class SupabaseService
    {
        private readonly Supabase.Client client;
        private readonly Supabase.Client adminClient;
        private readonly string ClientKey 
            = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImlubGNrcXdhd21waHlvd3dsbGlyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTg1NTk4OTksImV4cCI6MjA3NDEzNTg5OX0.OxFodXckQwJkdeupxugRw3sWttRIJLjqSZ_rBS4xS7g";
        private readonly string adminClientKey 
            = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImlubGNrcXdhd21waHlvd3dsbGlyIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1ODU1OTg5OSwiZXhwIjoyMDc0MTM1ODk5fQ.dkDRUek1vPUdoBgu5DunjtFgGiOYkhc_YpsHGU88TWY";

        private readonly DialogService _dialogService;
        private readonly IJSRuntime _jsRuntime;

        public SupabaseService(DialogService dialogService, IJSRuntime jsRuntime,ILocalStorageService localStorage)
        {
            _dialogService = dialogService;
            _jsRuntime = jsRuntime;
            LocalStorage = localStorage;
            client = new Supabase.Client("https://inlckqwawmphyowwllir.supabase.co", supabaseKey: ClientKey);
            adminClient = new Supabase.Client("https://inlckqwawmphyowwllir.supabase.co", supabaseKey: adminClientKey);
        }

        private ILocalStorageService LocalStorage { get; set; }

        public User? GetCurrentUser() => client.Auth.CurrentUser;
        public bool? IsAdminUser()
        {
            var currentUser = client.Auth.CurrentUser;
            return currentUser?.UserMetadata["role"].Equals("admin");
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
            await RestoreSessionAsync();
        }

        private async Task RestoreSessionAsync()
        {
            var sessionJson = await LocalStorage.GetItemAsStringAsync("supabase_session");

            if (!string.IsNullOrEmpty(sessionJson))
            {
                try
                {
                    await SaveSessionAsync(sessionJson);
                }
                catch
                {
                    Console.WriteLine("Failed to restore session.");
                }
            }
        }
        private async Task SaveSessionAsync(string? sessionJson)
        {
            var session = System.Text.Json.JsonSerializer.Deserialize<Supabase.Gotrue.Session>(sessionJson);

            if (session != null)
            {
                await this.Client.Auth.SetSession(session.AccessToken, session.RefreshToken);
            }
        }
        /// <summary>
        /// Register a new user with email, password, and optional username.
        /// Returns the User object if successful.
        /// </summary>

        public async Task<(User? user, string? errorMessage)> RegisterAsync(
            string email, string password, string name, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (null, "Email is required");
            if (string.IsNullOrWhiteSpace(password))
                return (null, "Password is required");
            if (string.IsNullOrWhiteSpace(name))
                return (null, "Username is required");
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return (null, "Phone number is required");
            var result = await this.UserExistsAsync(name, email, phoneNumber);
            if (result.exists)
            {
                return (null,"Ο χρήστης υπάρχει ήδη");
            }
            try
            {
                var options = new SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        { "name", name },
                        { "phone_number", phoneNumber },
                        { "email_address", email },
                        { "role", "user" }
                    }
                };

                var response = await client.Auth.SignUp(
                    Constants.SignUpType.Email,
                    email,
                    password,
                    options
                );

                if (response.User != null)
                {
                    Console.WriteLine($"User registered: {response.User.Email}");
                    return (response.User, null);
                }

                return (null, "Registration failed");
            }
            catch (Supabase.Gotrue.Exceptions.GotrueException ex)
            {
                // This exception usually tells you if the email already exists
                return (null, ex.Message);
            }
        }


        public async Task<(bool exists, string? field)> UserExistsAsync(string name, string email, string phoneNumber)
        {
            try
            {
                var users = await adminClient.AdminAuth(adminClientKey).ListUsers();

                var existing = users.Users.FirstOrDefault(u =>
                    u.Email.Equals(email) ||
                    (u.UserMetadata.ContainsKey("name") && u.UserMetadata["name"].Equals(name)) ||
                    (u.UserMetadata.ContainsKey("phone_number") && u.UserMetadata["phone_number"].Equals(phoneNumber))
                );

                if (existing != null)
                {
                    if (existing.UserMetadata.ContainsKey("name") && existing.UserMetadata["name"].Equals(name))
                        return (true, "name");
                    if (existing.Email.Equals(email))
                        return (true, "email");
                    if (existing.UserMetadata.ContainsKey("phone_number") && existing.UserMetadata["phone_number"].Equals(phoneNumber))
                        return (true, "phone number");
                }
                
                return (false, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking user existence: {ex.Message}");
                return (false, null);
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
        public async Task<(User? user, string? errorMessage)> LoginAsync(string email, string password)
        {
            try
            {
                var response = await client.Auth.SignInWithPassword(email, password);

                if (response?.User != null)
                {
                    //await this.CheckForPhoneAsync(); // may be needed
                    
                    return (response.User, null); // success
                }
                return (null, "Λανθασμένος κωδικός ή διεύθυνση email");
            }
            catch (Supabase.Gotrue.Exceptions.GotrueException ex)
            {
                var message = ex.Message.ToLower();

                if (message.Contains("email_not_confirmed"))
                {
                    return (null, "Η διεύθυνση email δεν έχει επαληθευτεί");
                }
                else if (message.Contains("invalid login credentials"))
                {
                    return (null, "Λανθασμένος κωδικός ή διεύθυνση email");
                }
                else
                {
                    return (null, "Προέκυψε απρόοπτο σφάλμα");
                }
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
                var response = await adminClient.From<Appointment>().Select("*").Get();
                return response.Models;
            }
        }
        /// <summary>
        /// Add a new appointment for the current user.
        /// RLS ensures users can only insert their own appointments.
        /// </summary>
        public async Task<Appointment?> AddAppointmentAsync(Appointment model)
        {
            try
            {
                var response = await client
                    .From<Appointment>()
                    .Insert(model);

                return response.Models.FirstOrDefault();
            }
            catch (Supabase.Gotrue.Exceptions.GotrueException ex)
            {
                Console.WriteLine($"Error adding appointment: {ex.Message}");
                return null;
            }
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
        public async Task<(bool,string?)> DeleteAppointmentAsync(Guid appointmentId)
        {
            // var currentUser = client.Auth.CurrentUser;
            // if (currentUser == null)
            //     return (false,"user not found");
            try
            {
                // if (currentUser.UserMetadata["role"].Equals("admin"))
                // {
                await adminClient
                    .From<Appointment>()
                    .Where(a => a.Id == appointmentId)
                    .Delete();
                return (true,null);
                // }
                // else
                // {
                    // await client
                    //     .From<Appointment>()
                    //     .Where(a => a.Id == appointmentId)
                    //     .Delete();
                    // return (true,null);
                //}
            }
            catch (Supabase.Gotrue.Exceptions.GotrueException ex)
            {
                return (false,ex.Message);
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

        public async Task CheckForPhoneAsync()
        {
            var user = Client.Auth.CurrentUser;
            if (!user.UserMetadata.ContainsKey("phone_number")||user.UserMetadata["phone_number"].ToString().IsNullOrEmpty())
            {
                await _dialogService.OpenAsync<EnterPhoneDialog>(
                    "Προσθέστε τον αριθμό σας",
                    new Dictionary<string, object> { { "UserId", user.Id } },
                    new DialogOptions
                    {
                        ShowClose = false,     // no close button
                        CloseDialogOnEsc = false, // can't escape
                        CloseDialogOnOverlayClick = false,          
                    }
                );
                        
            }
        }

        public bool IsMyAppointment(Guid appointmentUserId)
        {
            var currentUser = client.Auth.CurrentUser;
            if (currentUser == null || string.IsNullOrEmpty(currentUser.Id))
                return false;

            if (!Guid.TryParse(currentUser.Id, out var currentUserId))
                return false;
            return currentUserId == appointmentUserId;
        }
    }
}
