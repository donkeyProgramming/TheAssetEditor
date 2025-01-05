using System.Data.SQLite;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Utility.DatabaseSchemaGenerator.Examples;

namespace Utility.DatabaseSchemaGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CreateDatabase();
        }

        static void CreateDatabase()
        {
            var dbHandler = new DbSchemaBuilder(null);
            var pfs = CreatePackFileService();
            var databasePath = "app.db";

            if (File.Exists(databasePath))
            { 
                File.Delete(databasePath);
            }

            // Create the SQLite database file
            if (!File.Exists(databasePath))
            {
                Console.WriteLine($"Creating database file: {databasePath}");
                SQLiteConnection.CreateFile(databasePath);
            }


            // Create and open the SQLite connection
            using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                Console.WriteLine("Database connection opened.");

                using var command = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection);
                command.ExecuteNonQuery();

                try
                {
                    dbHandler.CreateSqTableScehma(connection, false, false);
                    Console.WriteLine("Schema successfully applied.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error applying schema: {ex.Message}");
                }

                dbHandler.PopulateTable(pfs, connection);
            }

            Console.WriteLine("Done.");
        }

        static string FormatCommandText(SQLiteCommand command)
        {
            string formattedCommand = command.CommandText;

            foreach (SQLiteParameter parameter in command.Parameters)
            {
                string placeholder = parameter.ParameterName;
                string value = parameter.Value is string ? $"'{parameter.Value}'" : parameter.Value.ToString();

                // Replace the parameter placeholder with the actual value
                formattedCommand = formattedCommand.Replace(placeholder, value);
            }

            return formattedCommand;
        }

        static IPackFileService CreatePackFileService()
        {
            var settings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            settings.Load();

            var containerLoader = new PackFileContainerLoader(settings);
            var gameFiles = containerLoader.LoadAllCaFiles(GameTypeEnum.Warhammer3);

            var pfs = new PackFileService(null);
            pfs.AddContainer(gameFiles);

            return pfs;
        }
    }
}
