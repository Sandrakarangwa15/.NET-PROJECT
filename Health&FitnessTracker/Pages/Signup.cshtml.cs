using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Health_FitnessTracker.Pages
{
    public class SignupModel : PageModel
    {
        private readonly IConfiguration _config;
        public string? connString;

        public SignupModel(IConfiguration config)
        {
            _config = config;
            connString = _config.GetConnectionString("MyConnection");
        }

        // Properties to bind form data
        [BindProperty]
        public string? Name { get; set; }

        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        [BindProperty]
        public string? ConfirmPassword { get; set; }

        [BindProperty]
        public DateTime? DateOfBirth { get; set; }

        public string Message = "";

        public void OnGet()
        {
            // Handle GET requests
        }

        public IActionResult OnPost()
        {
            // Validate form inputs
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(ConfirmPassword))
            {
                Message = "Please fill all the required fields.";
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                Message = "Passwords do not match.";
                return Page();
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    // Check if the email already exists in the database
                    string checkEmailQuery = "SELECT COUNT(1) FROM Users WHERE email = @Email";
                    using (SqlCommand checkCmd = new SqlCommand(checkEmailQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Email", Email);
                        int emailExists = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (emailExists > 0)
                        {
                            Message = "An account with this email already exists.";
                            return Page();
                        }
                    }

                    // Hash the password using SHA256
                    string hashedPassword = HashPassword(Password);

                    // Insert new user into the database
                    string insertQuery = "INSERT INTO Users (name, email, password, dob) " +
                                         "VALUES (@Name, @Email, @Password, @DateOfBirth)";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", Name);
                        cmd.Parameters.AddWithValue("@Email", Email);
                        cmd.Parameters.AddWithValue("@Password", hashedPassword);  // Store the hashed password
                        cmd.Parameters.AddWithValue("@DateOfBirth", DateOfBirth as object ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Redirect to Login page after successful sign-up
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
                return Page();
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Compute hash from the password bytes
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert the byte array to a hex string
                StringBuilder builder = new StringBuilder();
                foreach (byte byteValue in bytes)
                {
                    builder.Append(byteValue.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
