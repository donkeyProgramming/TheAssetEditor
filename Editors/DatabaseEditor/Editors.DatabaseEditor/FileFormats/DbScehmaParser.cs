using System.Text.Json;
using System.Text.Json.Nodes;
using Shared.Core.Settings;

namespace Editors.DatabaseEditor.FileFormats
{
    public static class DbScehmaParser
    {
        public static DbSchema CreateFromRpfmJson(string systemFilePath, GameTypeEnum game)
        {
            var dbSchema = new DbSchema() { Game = game };

            var jsonContent = File.ReadAllText(systemFilePath);
            var jsonNode = JsonSerializer.Deserialize<JsonNode>(jsonContent);

            var definitions = jsonNode["definitions"] as JsonObject;
            foreach (var definition in definitions)
            {
                var tableName = definition.Key;

                var tableVersions = definition.Value as JsonArray;
                foreach (var tableVersion in tableVersions)
                {
                    var schemaVersion = tableVersion["version"].GetValue<int>();
                    var schemaFields = tableVersion["fields"] as JsonArray;

                    var dbTableSchema = new DBTableSchema()
                    {
                        Name = tableName,
                        Version = schemaVersion
                    };

                    foreach (var schemaField in schemaFields)
                    {
                        var name = schemaField["name"].GetValue<string>();
                        var field_type = schemaField["field_type"].GetValue<string>();
                        var isKey = schemaField["is_key"].GetValue<bool>();

                        var dbColoumnSchema = new DbColoumnSchema()
                        {
                            DataType = field_type,
                            IsKey = isKey,
                            Name = name,
                            Description = ""
                        };

                        dbTableSchema.Coloumns.Add(dbColoumnSchema);
                    }

                    dbSchema.TableSchemas.Add(dbTableSchema);
                }
            }

            return dbSchema;
        }
    }
}
