using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using Editors.DatabaseEditor.FileFormats;
using Shared.Core.ByteParsing;
using Shared.Core.PackFiles;
using Shared.Core.Settings;

namespace Utility.DatabaseSchemaGenerator.Examples
{
    internal class DbSchemaBuilder
    {
        // battle_skeletons_tables
        // battle_skeleton_parts_tables



        /*
        CREATE TABLE IF NOT EXISTS Products (
            Id INTEGER,
            Name TEXT NOT NULL,
            Price REAL NOT NULL
        ) WITHOUT ROWID;
        */


        private readonly static Dictionary<string, string> s_mappingTable = new()
        {
            {"StringU8", "TEXT NOT NULL"},
            {"StringU16", "TEXT NOT NULL"},
            {"OptionalStringU8", "TEXT"},
            {"F32","FLOAT"},
            {"F64","REAL"},

            {"Boolean","bit"},
           
            {"I32","INT"},
            {"I64","BIGINT"},
            {"I16","smallint"},
            {"OptionalI32","NullableColumn INTEGER"},
            {"ColourRGB","tinyint"},// probably need to find a better type
           
        };


        private readonly static List<(string SchemaName, string sqlType, DbType SqlSerializeType, DbTypesEnum GameType)> s_typeMappingTable = new()
        {
            {("StringU8",           "TEXT NOT NULL",            DbType.String,      DbTypesEnum.String)},
            {("OptionalStringU8",   "TEXT",                     DbType.String,      DbTypesEnum.Optstring)},

            {("StringU16",          "TEXT NOT NULL",            DbType.AnsiString,  DbTypesEnum.String_ascii)},
            {("OptionalStringU8",   "TEXT",                     DbType.AnsiString,  DbTypesEnum.Optstring_ascii)},

            {("F32",                "FLOAT",                    DbType.Double,      DbTypesEnum.Single)},
            {("F64",                "REAL",                     DbType.Double,      DbTypesEnum.Int64)},     // wrong type

            {("I16",                "smallint",                 DbType.Int16,       DbTypesEnum.Short)},
            {("I32",                "INT",                      DbType.Int32,       DbTypesEnum.Integer)},
            {("OptionalI32",        "NullableColumn INTEGER",   DbType.Int32,       DbTypesEnum.Integer)},   // wrong type
            {("I64",                "BIGINT",                   DbType.Int64,       DbTypesEnum.Int64)},
           
            {("Boolean",            "TEXT NOT NULL",            DbType.Boolean,     DbTypesEnum.Boolean)},

            {("ColourRGB",          "tinyint",                  DbType.SByte,       DbTypesEnum.Short)},     // wrong type
        };

        private readonly DbSchema _jsonSchema;
        private readonly IPackFileService _packFileService;

        public DbSchemaBuilder(IPackFileService packFileService)
        {
            _jsonSchema = DbScehmaParser.CreateFromRpfmJson(@"C:\Users\ole_k\Downloads\schema_wh3.json", GameTypeEnum.Warhammer3);

            // Filter scheamas to make debugging easier
            _jsonSchema.TableSchemas = _jsonSchema.TableSchemas
                // Make sure we grab latest version (For now)
                .OrderByDescending(x=>x.Version)
                .GroupBy(x => x.Name)
                .Select(x=>x.First())

                // For debugging, only have a few tables
                //.Where(x => x.Name == "battle_skeleton_parts_tables" || x.Name == "battle_skeletons_tables")
                .ToList();



            DbScemaValidation.Validate(_jsonSchema);
            _packFileService = packFileService;
        }

        public void CreateSqTableScehma(SQLiteConnection connection, bool addKeys, bool addForeignKeys)
        {
            var numEdges = 0;
            var numNodes = 0;
            var maxEdges = 10;
            var tablesWithoutFk = 0;
            var tableSchemas = _jsonSchema.TableSchemas;




            var tables = new Dictionary<string, string>(); 
            foreach (var tableSchema in tableSchemas)
            {
                var tableName = tableSchema.Name;
                numNodes++;

                var sqlCommand = $"CREATE TABLE {tableName} (\n";
                for(var i = 0; i < tableSchema.Coloumns.Count; i++) 
                {
                    var coloumnSchema = tableSchema.Coloumns[i];
                    var coloumName = coloumnSchema.Name;
                    var coloumDataType = s_mappingTable[coloumnSchema.DataType];
                    var coloumnModifier = "";
                    var comma = i != (tableSchema.Coloumns.Count-1) ? "," : "";

                    sqlCommand += $"\t[{coloumName}] {coloumDataType} {coloumnModifier} {comma} \n";
                }

                var foreignKeyColoums = tableSchema.Coloumns.Where(x=>x.ForeignKey != null).ToList();
                if (foreignKeyColoums.Count == 0)
                    tablesWithoutFk++;

                if (addForeignKeys)
                {
                    foreach (var coloumn in foreignKeyColoums)
                    {
                        var coloumName = coloumn.Name;
                        var foreignKeyTableName = coloumn.ForeignKey.Table + "_tables";
                        var foreignTableRef = tableSchemas.FirstOrDefault(x => x.Name == foreignKeyTableName);
                        if (foreignTableRef != null)
                        {
                            sqlCommand += $"\t ,FOREIGN KEY ([{coloumName}]) REFERENCES {foreignKeyTableName}([{coloumn.ForeignKey.ColoumnName}]) \n";
                            numEdges++;
                        }
                    }
                }

                var keyColoumns = tableSchema.Coloumns
                    .Where(x=>x.IsKey)
                    .Select(x=>$"[{x.Name}]")
                    .ToList();
               
                // Add this to the top to make it look prettier
                if (keyColoumns.Any())
                    sqlCommand += $",PRIMARY KEY ({string.Join(", ", keyColoumns)})";

                sqlCommand += ");";

                tables.Add(tableName, sqlCommand);
            }

            var fullSqlCommand = "";
            fullSqlCommand +=string.Join("\n\n", tables.Values);

            // Execute the schema SQL commands
            using var command = new SQLiteCommand(fullSqlCommand, connection);

            command.ExecuteNonQuery();
        }



        DataTable? LoadTable(string tableName)
        {

            var tableSchema = _jsonSchema.TableSchemas.First(x=>x.Name == tableName);

            var packFile = _packFileService.FindFile($"db\\{tableName}\\data__");
            if (packFile == null)
            {
                Console.WriteLine(" - Not found");
                return null;
            }
            var byteChunk = packFile.DataSource.ReadDataAsChunk();

            var peakGuid = byteChunk.PeakUint32();
            if ((uint)4294770429 == peakGuid)
            {
                byteChunk.ReadUInt32();
                var guid = byteChunk.ReadStringAscii();
            }

            var peakVersion = byteChunk.PeakUint32();
            if ((uint)4294901244 == peakVersion)
            {
                byteChunk.ReadUInt32();
                var version = byteChunk.ReadUInt32();
            }

            var unkownBool = byteChunk.ReadBool();
            var numTableEntries = byteChunk.ReadUInt32();

            // Build the parameters
            var dt = new DataTable();
            dt.Clear();
            foreach (var tableColoumn in tableSchema.Coloumns)
                dt.Columns.Add(tableColoumn.Name);

            // Fill the command with game data
            for (var i = 0; i < numTableEntries; i++)
            {
                var row = dt.NewRow();
                for (var j = 0; j < tableSchema.Coloumns.Count; j++)
                {
                    var coloumn = tableSchema.Coloumns[j];

                    var gameType = s_typeMappingTable.First(x => x.SchemaName == coloumn.DataType).GameType;
                    var valueFromGameDb = byteChunk.ReadObject(gameType);
                    row[coloumn.Name] = valueFromGameDb;
                }

            }

            if (byteChunk.BytesLeft != 0)
                throw new Exception("Data left - error parsing");

            return dt;
        }

        public void PopulateTable(IPackFileService packFileService, SQLiteConnection sqlConnection)
        {
            var tableSchemas = _jsonSchema.TableSchemas;
            var parsedTables = 0;
            var skippedTables = 0;
            var failedTables = new List<string>();

            var tables = new List<DataTable>();
            foreach (var tableSchema in tableSchemas)
            {
                try
                {
                    var parsedTable = LoadTable(tableSchema.Name);
                    if(parsedTable != null)
                        tables.Add(parsedTable);
                }
                catch (Exception e)
                {
                }

            }




            //diplomacy_negotiation_string_options_tables
            //https://timdeschryver.dev/blog/faster-sql-bulk-inserts-with-csharp#sql-bulk-copy?

   
            foreach (var tableSchema in tableSchemas)
            {
                try
                {
                    using (var copy = new SqlBulkCopy(sqlConnection.ConnectionString))
                    {


                        //var _ravi = dt.NewRow();
                        //_ravi["Name"] = "ravi";
                        //_ravi["Marks"] = "500";
                        //dt.Rows.Add(_ravi);
                        //
                        ////copy.BatchSize
                        //copy.DestinationTableName = "dbo.Customers";
                        //copy.ColumnMappings.Add(nameof(Customer.Id), "Id");
                        //copy.ColumnMappings.Add(nameof(Customer.FirstName), "FirstName");


                       // copy.WriteToServer(dt);
                    }
                 
                    Console.WriteLine($"{tableSchema.Name} - {parsedTables}/{tableSchemas.Count}");

                    // Skip tables with datatypes that are currently not supported:
                    string[] unsupportedTypes = ["F62", "OptionalI32", "ColourRGB"];

                    var allTypes = tableSchema.Coloumns
                        .Select(x => x.DataType)
                        .Distinct()
                        .ToList();

                    var hasMatch = allTypes.Any(x => unsupportedTypes.Any(y => y == x));
                    if (hasMatch)
                    {
                        Console.WriteLine(" - Skipped");
                        skippedTables++;
                        continue;
                    }

                    // PackFile Parsing
                    var packFile = packFileService.FindFile($"db\\{tableSchema.Name}\\data__");
                    if (packFile == null)
                    {
                        Console.WriteLine(" - Not found");
                        continue;
                    }
                    var byteChunk = packFile.DataSource.ReadDataAsChunk();

                    var peakGuid = byteChunk.PeakUint32();
                    if ((uint)4294770429 == peakGuid)
                    {
                        byteChunk.ReadUInt32();
                        var guid = byteChunk.ReadStringAscii();
                    }

                    var peakVersion = byteChunk.PeakUint32();
                    if ((uint)4294901244 == peakVersion)
                    {
                        byteChunk.ReadUInt32();
                        var version = byteChunk.ReadUInt32();
                    }

                    var unkownBool = byteChunk.ReadBool();
                    var numTableEntries = byteChunk.ReadUInt32();

                    // Create sql command
                    var tableColoumnNames = tableSchema.Coloumns.Select(x => $"[{x.Name}]").ToList();
                    var values = tableSchema.Coloumns.Select(x => $"@{x.Name}").ToList();

                    var strCommand = $"INSERT INTO {tableSchema.Name} ({string.Join(",", tableColoumnNames)}) VALUES ({string.Join(",", values)})";
                    using var sqlCommand = new SQLiteCommand(strCommand, sqlConnection);

                    // Build the parameters
                    var valueParameters = new List<SQLiteParameter>();
                    foreach (var tableColoumn in tableSchema.Coloumns)
                    {
                        var dbType = s_typeMappingTable.First(x => x.SchemaName == tableColoumn.DataType).SqlSerializeType;
                        var dbName = $"@{tableColoumn.Name}";
                        var newParam = sqlCommand.Parameters.Add(dbName, dbType);
                        valueParameters.Add(newParam);
                    }

                    // Fill the command with game data
                    for (var i = 0; i < numTableEntries; i++)
                    {
                        //foreach (var valueParameter in valueParameters)
                        for (var j = 0; j < tableSchema.Coloumns.Count; j++)
                        {
                            var valueParameter = valueParameters[j];
                            var coloumn = tableSchema.Coloumns[j];

                            var gameType = s_typeMappingTable.First(x => x.SchemaName == coloumn.DataType).GameType;
                            var valueFromGameDb = byteChunk.ReadObject(gameType);
                            valueParameter.Value = valueFromGameDb;
                        }

                        sqlCommand.ExecuteNonQuery();
                    }

                    if (byteChunk.BytesLeft != 0)
                        throw new Exception("Data left - error parsing");

                    parsedTables++;
                }
                catch (Exception e)
                {
                    Console.WriteLine(" - Failed");
                    failedTables.Add(tableSchema.Name + " - " + e.Message);
                }
            }
        }
    }
}
