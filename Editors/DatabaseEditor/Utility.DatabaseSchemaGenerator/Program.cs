using System.Data.SqlClient;
using System.Data.SQLite;
using Editors.DatabaseEditor.FileFormats;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Shared.Core.ByteParsing;
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
            //GenerateSchema();
            //CreateExampleDb();
        }

        static void GenerateSchema()
        {
            var dbSchema = DbScehmaParser.CreateFromRpfmJson(@"C:\Users\ole_k\Downloads\schema_wh3.json", GameTypeEnum.Warhammer3);

            // Delete old files
            // Get table
            // Generate
        }




        static void CreateDatabase()
        {
            var dbHandler = new DbSchemaBuilder();
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
                    dbHandler.CreateSqTableScehma(connection, true, true);
                    Console.WriteLine("Schema successfully applied.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error applying schema: {ex.Message}");
                }

               // using var transaction = connection.BeginTransaction();
               // try
               // {
               //     dbHandler.PopulateTable(pfs, connection);
               //     Console.WriteLine("PopulateTable called successfully.");
               // }
               // catch (Exception ex)
               // {
               //     Console.WriteLine($"Error PopulateTable: {ex.Message}");
               // }
               //
               // transaction.Commit();
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



        public static ExampleDb CreateDbContext()
        {
            var configurationBuilder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = configurationBuilder.Build();
            string connectionString = ExampleDb.GetDbPath();

            var optionsBuilder = new DbContextOptionsBuilder<ExampleDb>()
                .UseSqlite(connectionString);

            return new ExampleDb(optionsBuilder.Options);
        }

        static IPackFileService CreatePackFileService()
        {
            var settings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            settings.Load();
            var gameInfo = new GameInformationFactory();

            var containerLoader = new PackFileContainerLoader(settings, gameInfo);
            var gameFiles = containerLoader.LoadAllCaFiles(GameTypeEnum.Warhammer3);

            var pfs = new PackFileService(null);
            pfs.AddContainer(gameFiles);

            return pfs;
        }
    }
}
