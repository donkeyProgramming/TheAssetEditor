using Microsoft.EntityFrameworkCore;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Utility.DatabaseSchemaGenerator.Examples
{



    public class ExampleDb : DbContext
    {

        public static string GetDbPath()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            return System.IO.Path.Join(path, "blogging.db");    //C:\Users\ole_k\AppData\Local
        }

        IPackFileService _packFileService;

        public DbSet<battle_skeleton_parts> battle_skeleton_parts_table { get; set; }

        public ExampleDb(DbContextOptions<ExampleDb> options) : base(options) 
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var path = GetDbPath();
            optionsBuilder.UseSqlite($"Data Source={path}");
        }

        public void Deserialize(IPackFileService packFileService)
        {
            _packFileService = packFileService;

            Load(battle_skeleton_parts_table, battle_skeleton_parts.TableName, battle_skeleton_parts.Deserialize);
        }

        void Load<T>(DbSet<T> values, string tableName, Func<PackFile, List<T>> loader) where T : class
        {
            var file = _packFileService.FindFile($"db\\{tableName}\\data__");   // Get all in folder
            if (file != null)
            {
                var fileContent = loader(file);
                values.AddRange(fileContent);
            }
        }
    }
}
