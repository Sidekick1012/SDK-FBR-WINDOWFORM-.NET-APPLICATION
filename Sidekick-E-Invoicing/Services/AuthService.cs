using Sidekick_E_Invoicing.Data;
using System.Data.SQLite;

namespace Sidekick_E_Invoicing.Services
{
    public class AuthService
    {
        public bool ValidateUser(string username, string password)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM Users WHERE Username=@Username AND Password=@Password";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }
    }
}