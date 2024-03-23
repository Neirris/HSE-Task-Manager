using System.Configuration;
using System.Net.Http;
using System.Windows;
using MySql.Data.MySqlClient;
using Task_ManagerCP3.Services;

namespace Task_ManagerCP3
{
    public partial class App : Application
    {
        public static string AuthToken = "";
        public static NotificationManager NotificationManagerInstance { get; set; }
        public static MySqlConnection GetConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["InitConnection"].ConnectionString;
            return new MySqlConnection(connectionString);
        }

        public static int GetUserId(string token, MySqlConnection conn)
        {
            using (var cmd = new MySqlCommand(@"SELECT id FROM users WHERE token = @token", conn))
            {
                cmd.Parameters.AddWithValue("@token", token);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static bool? IsImageUrl(string url)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var contentType = response?.Content?.Headers?.ContentType?.MediaType?.ToLower();
                        return contentType?.StartsWith("image/");
                    }
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
