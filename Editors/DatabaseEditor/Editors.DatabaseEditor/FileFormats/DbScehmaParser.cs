using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Schema;
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
                        var is_ref = schemaField["is_reference"] as JsonArray;

                        var dbColoumnSchema = new DbColoumnSchema()
                        {
                            DataType = field_type,
                            IsKey = isKey,
                            Name = name,
                            Description = ""
                        };

                        if (is_ref != null)
                        {
                            var foreignKeyTable = is_ref[0].GetValue<string>();
                            var foreignKeyColumn= is_ref[1].GetValue<string>();
                            dbColoumnSchema.ForeignKey = new DbColoumnForeignKey(foreignKeyTable, foreignKeyColumn);
                        }

                        dbTableSchema.Coloumns.Add(dbColoumnSchema);
                    }

                    dbSchema.TableSchemas.Add(dbTableSchema);
                }
            }

            return dbSchema;
        }
    }
}
