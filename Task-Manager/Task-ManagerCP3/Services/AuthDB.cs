using System.Text;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;

namespace Task_ManagerCP3
{
    class AuthDB
    {
        public static string ConvertHashSHA512(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            using (var hash = SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);
                var res = BitConverter.ToString(hashedInputBytes).Replace("-", "").ToLower();
                return res;
            }
        }

        public static bool Registration(string login, string password)
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand())
                {
                    bool isUserExists = false;
                    cmd.Connection = conn;
                    cmd.CommandText = @"SELECT EXISTS (SELECT 1 FROM users WHERE username = @login)";
                    cmd.Parameters.AddWithValue("@login", login);
                    isUserExists = Convert.ToInt64(cmd.ExecuteScalar()) == 1;


                    if (isUserExists)
                    {
                        return false;
                    }
                    else
                    {
                        cmd.CommandText = @"INSERT INTO users (username, passwordHash, token)
                                         VALUES (@login, @password, @token)";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", ConvertHashSHA512(password));
                        cmd.Parameters.AddWithValue("@token", ConvertHashSHA512(login + password));
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
        }

        public static bool TokenCheck(string token)
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;

                    cmd.CommandText = @"SELECT token FROM users WHERE token = @token LIMIT 1";
                    cmd.Parameters.AddWithValue("@token", token);
                    var storedToken = (string)cmd.ExecuteScalar();

                    bool isTokenExists = !string.IsNullOrEmpty(storedToken);
                    if (isTokenExists)
                    {
                         return true;
                    }

                    return false;
                }
            }
        }

        public static (bool IsUserExists, bool IsPasswordCorrect) Authentication(string login, string password)
        {
            using (var conn = App.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;

                    cmd.CommandText = @"SELECT passwordHash FROM users WHERE username = @login LIMIT 1";
                    cmd.Parameters.AddWithValue("@login", login);
                    var storedHash = (string)cmd.ExecuteScalar();

                    bool isUserExists = !string.IsNullOrEmpty(storedHash);
                    bool isPasswordCorrect = false;

                    if (isUserExists)
                    {
                        isPasswordCorrect = ConvertHashSHA512(password) == storedHash;

                        if (isPasswordCorrect)
                        {
                            App.AuthToken = ConvertHashSHA512(login + password);
                        }
                    }

                    return (isUserExists, isPasswordCorrect);
                }
            }
        }

    }
}
