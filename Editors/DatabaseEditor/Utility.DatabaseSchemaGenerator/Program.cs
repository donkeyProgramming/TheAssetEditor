using Editors.DatabaseEditor.FileFormats;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Utility.DatabaseSchemaGenerator.Examples;

namespace Utility.DatabaseSchemaGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //GenerateSchema();
            CreateExampleDb();
        }

        static void GenerateSchema()
        {
            var dbSchema = DbScehmaParser.CreateFromRpfmJson(@"C:\Users\ole_k\Downloads\schema_wh3.json", GameTypeEnum.Warhammer3);

            // Delete old files
            // Get table
            // Generate
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

        static void CreateExampleDb()
        {
            var settings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            var gameInfo = new GameInformationFactory();

            var containerLoader = new PackFileContainerLoader(settings, gameInfo);
            var gameFiles = containerLoader.LoadAllCaFiles(GameTypeEnum.Warhammer3);

            var pfs = new PackFileService(null);
            pfs.AddContainer(gameFiles);

            using (ExampleDb sc = CreateDbContext())
            {

                //https://marketplace.visualstudio.com/items?itemName=ErikEJ.SQLServerCompactSQLiteToolbox&ssr=false#overview
                // dotnet tool install --global dotnet-ef
                //dotnet add package Microsoft.EntityFrameworkCore.Design*/
                
                //var m = sc.Database.GetAppliedMigrations();
                sc.Database.EnsureCreated();

                sc.Deserialize(pfs);
                sc.SaveChanges();
            }
        }
    }
}
