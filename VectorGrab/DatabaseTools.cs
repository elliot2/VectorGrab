using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlServerCe;
using System.IO;

namespace VectorGrab
{
    class DatabaseTools
    {
        public DatabaseTools()
        {

        }

        internal void createTable()
        {

            string connectionString = "Data Source=database.sdf";

            // Create the database file
            if (!File.Exists("database.sdf"))
            {
                SqlCeEngine engine = new SqlCeEngine(connectionString);
                engine.CreateDatabase();
            }

            // Create the table
            using (SqlCeConnection connection = new SqlCeConnection(connectionString))
            {
                connection.Open();

                using (SqlCeCommand command = new SqlCeCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "CREATE TABLE table_name (documentFileName NVARCHAR(255), page BIGINT, paragraph BIGINT, vector BINARY(6144))";
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
