using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Health_FitnessTracker.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly string? connString;

        public LoginModel(IConfiguration config)
        {
            _config = config;
            connString = _config.GetConnectionString("MyConnection");
        }

        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        public string Message = "";

        public void OnGet()
        {
        }

        public void OnPost()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                Message = "Email and password are required.";
                return;
            }

            if (!Email.Contains('@'))
            {
                Message = "Invalid email format.";
                return;
            }

            try
            {
                using (SqlConnection conn = new(connString))
                {
                    conn.Open();

                    string query = "SELECT password, name FROM Users WHERE email = @Email";
                    using (SqlCommand cmd = new(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", Email);
                        using SqlDataReader reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            string storedPassword = reader["password"].ToString();
                            string hashedPassword = HashPassword(Password);

                            if (storedPassword == hashedPassword)
                            {
                                HttpContext.Session.SetString("UserName", reader["name"].ToString());
                                HttpContext.Session.SetString("UserEmail", Email);
                                HttpContext.Response.Redirect("/ProfileSetup");
                            }
                            else
                            {
                                Message = "Invalid email or password.";
                            }
                        }
                        else
                        {
                            Message = "Account does not exist.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
            }
        }

        private string HashPassword(string password)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
