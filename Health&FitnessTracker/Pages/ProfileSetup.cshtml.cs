using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;

namespace Health_FitnessTracker.Pages
{
    public class ProfileSetupModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly string connString;

        public ProfileSetupModel(IConfiguration config)
        {
            _config = config;
            connString = _config.GetConnectionString("MyConnection");
        }

        [BindProperty]
        public double Height { get; set; }
        [BindProperty]
        public double Weight { get; set; }
        [BindProperty]
        public string FitnessGoal { get; set; }
        public double BMI { get; set; }
        public string Recommendations { get; set; }

        public void OnGet()
        {
            BMI = 0;
            Recommendations = string.Empty;
        }

        public IActionResult OnPost()
        {
            if (Height <= 0 || Weight <= 0 || string.IsNullOrWhiteSpace(FitnessGoal))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return Page();
            }

            if (HttpContext.Session.GetString("UserEmail") == null)
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToPage("/Login");
            }

            BMI = CalculateBMI(Height, Weight);
            Recommendations = GenerateRecommendations(BMI, FitnessGoal);

            try
            {
                string userEmail = HttpContext.Session.GetString("UserEmail");
                int userId = GetUserIdByEmail(userEmail);

                if (userId == 0)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return Page();
                }

                SaveProfileData(userId);

                
                TempData["SuccessMessage"] = "Profile saved successfully.";
                return Page();  
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return Page();
            }
        }

        private double CalculateBMI(double height, double weight) =>
            weight / Math.Pow(height / 100, 2);

        private int GetUserIdByEmail(string email)
        {
            using SqlConnection conn = new(connString);
            conn.Open();
            string query = "SELECT UserId FROM Users WHERE email = @Email";
            using SqlCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@Email", email);
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        private string GenerateRecommendations(double bmi, string goal)
        {
            string recommendation = $"Your BMI is {bmi:F2}. ";
            recommendation += bmi switch
            {
                < 18.5 => "You are underweight. Focus on gaining weight.",
                < 24.9 => "Your weight is normal.",
                < 29.9 => "You are overweight. Consider weight loss.",
                _ => "You are obese. Consult a healthcare provider."
            };
            recommendation += goal switch
            {
                "Lose Weight" => " Focus on a caloric deficit.",
                "Maintain Weight" => " Maintain a balanced lifestyle.",
                "Gain Muscle" => " Prioritize strength training.",
                _ => ""
            };
            return recommendation;
        }

        private void SaveProfileData(int userId)
        {
            using SqlConnection conn = new(connString);
            conn.Open();
            string query = "INSERT INTO HealthProfile (UsersID, height, weight, BMI, fitnessGoal) VALUES (@UserID, @Height, @Weight, @BMI, @FitnessGoal)";
            using SqlCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@Height", Height);
            cmd.Parameters.AddWithValue("@Weight", Weight);
            cmd.Parameters.AddWithValue("@BMI", BMI);
            cmd.Parameters.AddWithValue("@FitnessGoal", FitnessGoal);
            cmd.ExecuteNonQuery();
        }
    }
}
