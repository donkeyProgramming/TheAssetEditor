namespace Shared.GameFormats.DB
{
    /*public class SchemaManager : IGameComponent
    {
        ILogger _logger = Logging.Create<SchemaManager>();

        Dictionary<GameTypeEnum, SchemaFile> _gameTableDefinitions = new Dictionary<GameTypeEnum, SchemaFile>();
        Dictionary<GameTypeEnum, SchemaFile> _gameAnimMetaDefinitions = new Dictionary<GameTypeEnum, SchemaFile>();

        public GameTypeEnum CurrentGame { get; set; } = GameTypeEnum.Warhammer2;

        public SchemaManager()
        {
            //foreach (var game in GameInformationFactory.Games)
            //    Load(game.Type);
        }

        public void UpdateCurrentTableDefinitions(SchemaFile schemaFile)
        {
            _gameTableDefinitions[CurrentGame] = schemaFile;
            SaveDbSchema();
        }

        public void UpdateCurrentTableDefinition(DbTableDefinition newTableDefinition)
        {
            if (_gameTableDefinitions[CurrentGame].TableDefinitions.ContainsKey(newTableDefinition.TableName))
            {
                var added = false;
                var defs = _gameTableDefinitions[CurrentGame].TableDefinitions[newTableDefinition.TableName];
                for (int i = 0; i < defs.Count; i++)
                {
                    if (defs[i].Version == newTableDefinition.Version)
                    {
                        defs[i].ColumnDefinitions = newTableDefinition.ColumnDefinitions;
                        added = true;
                        break;
                    }
                }

                if (added == false)
                    _gameTableDefinitions[CurrentGame].TableDefinitions[newTableDefinition.TableName].Add(newTableDefinition);
            }
            else
            {
                _gameTableDefinitions[CurrentGame].TableDefinitions.Add(newTableDefinition.TableName, new List<DbTableDefinition>());
                _gameTableDefinitions[CurrentGame].TableDefinitions[newTableDefinition.TableName].Add(newTableDefinition);
            }
            SaveDbSchema();
        }

        public bool IsSupported(string tableName)
        {
            var definition = GetTableDefinitionsForTable(tableName);
            if (definition.Count != 0)
                return true;
            return false;
        }

        public List<DbTableDefinition> GetTableDefinitionsForTable(string tableName)
        {
            if (!_gameTableDefinitions.ContainsKey(CurrentGame))
                return new List<DbTableDefinition>();

            if (_gameTableDefinitions[CurrentGame].TableDefinitions.ContainsKey(tableName))
                return _gameTableDefinitions[CurrentGame].TableDefinitions[tableName];
            return new List<DbTableDefinition>();
        }

        public DbTableDefinition GetTableDefinitionsForTable(string tableName, int version)
        {
            var def = GetTableDefinitionsForTable(tableName).FirstOrDefault(x => x.Version == version);
            if (def != null)
                return def;
            return new DbTableDefinition();
        }

        public DbTableDefinition GetMetaDataDefinition(string tableName, int version)
        {
            if (_gameAnimMetaDefinitions.ContainsKey(CurrentGame))
            {
                var allDefs = _gameAnimMetaDefinitions[CurrentGame].TableDefinitions.Where(x => x.Key == tableName).SelectMany(x => x.Value);
                var bestDef = allDefs.FirstOrDefault(x => x.Version == version);
                if (bestDef != null)
                    return bestDef;
            }

            return new DbTableDefinition()
            {
                TableName = tableName,
                Version = version
            };
        }

        public List<DbTableDefinition> GetAllMetaDataDefinitions()
        {
            if (_gameAnimMetaDefinitions.ContainsKey(CurrentGame))
            {
                var allDefs = _gameAnimMetaDefinitions[CurrentGame].TableDefinitions.SelectMany(x => x.Value).ToList();
                return allDefs;
            }

            return null;
        }

        public void UpdateMetaTableDefinition(DbTableDefinition newTableDefinition)
        {
            if (!_gameAnimMetaDefinitions.ContainsKey(CurrentGame))
                _gameAnimMetaDefinitions.Add(CurrentGame, new SchemaFile() { GameEnum = CurrentGame });

            if (_gameAnimMetaDefinitions[CurrentGame].TableDefinitions.ContainsKey(newTableDefinition.TableName))
            {
                var added = false;
                var defs = _gameAnimMetaDefinitions[CurrentGame].TableDefinitions[newTableDefinition.TableName];
                for (int i = 0; i < defs.Count; i++)
                {
                    if (defs[i].Version == newTableDefinition.Version)
                    {
                        defs[i].ColumnDefinitions = newTableDefinition.ColumnDefinitions;
                        added = true;
                        break;
                    }
                }

                if (added == false)
                    _gameAnimMetaDefinitions[CurrentGame].TableDefinitions[newTableDefinition.TableName].Add(newTableDefinition);
            }
            else
            {
                _gameAnimMetaDefinitions[CurrentGame].TableDefinitions.Add(newTableDefinition.TableName, new List<DbTableDefinition>());
                _gameAnimMetaDefinitions[CurrentGame].TableDefinitions[newTableDefinition.TableName].Add(newTableDefinition);
            }
            SaveMetaDataSchema();
        }


        public bool SaveMetaDataSchema()
        {
            _logger.Information("Trying to metadat schema");
            try
            {
                if (!_gameAnimMetaDefinitions.ContainsKey(CurrentGame))
                    return false;
                string path = DirectoryHelper.SchemaDirectory + "\\" + GameInformationFactory.GetGameById(CurrentGame).ShortID + "_AnimMetaDataSchema.json";
                var content = JsonConvert.SerializeObject(_gameAnimMetaDefinitions[CurrentGame], Formatting.Indented);
                File.WriteAllText(path, content);

                MessageBox.Show("Be kind and send the updated scehema to the developers so it can be shared");

                return true;
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                throw e;
            }
        }

        public bool SaveDbSchema()
        {
            _logger.Information("Trying to save file");
            try
            {
                if (!_gameTableDefinitions.ContainsKey(CurrentGame))
                    return false;
                string path = DirectoryHelper.SchemaDirectory + "\\" + GameInformationFactory.GetGameById(CurrentGame).ShortID + "_schema.json";
                var content = JsonConvert.SerializeObject(_gameTableDefinitions[CurrentGame], Formatting.Indented);
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                throw e;
            }
        }

        void Load(GameTypeEnum game)
        {
            if (_gameTableDefinitions.ContainsKey(game))
                return;

            string path = DirectoryHelper.SchemaDirectory + "\\" + GameInformationFactory.GetGameById(game).ShortID + "_schema.json";   // Try local copy first
            if (!File.Exists(path))
                path = "Resources\\Schemas\\" + GameInformationFactory.GetGameById(game).ShortID + "_schema.json";

            var content = LoadSchemaFile(path);
            if (content != null)
                _gameTableDefinitions.Add(game, content);

            path = DirectoryHelper.SchemaDirectory + "\\" + GameInformationFactory.GetGameById(game).ShortID + "_AnimMetaDataSchema.json";
            if (!File.Exists(path))
            {
                var allResource = GetResourceNames();

                var resourceFileName = GameInformationFactory.GetGameById(game).ShortID + "_AnimMetaDataSchema.json";
                var entry = allResource.FirstOrDefault(x => x.Key.ToString().Contains(resourceFileName, StringComparison.InvariantCultureIgnoreCase));
                if (entry.Key != null)
                {
                    using var resourceReader = new StreamReader(entry.Value as Stream);
                    var resourceContent = resourceReader.ReadToEnd();
                    content = LoadSchemaFileFromContent(resourceContent);
                }
            }
            else
                content = LoadSchemaFile(path);
            if (content != null)
                _gameAnimMetaDefinitions.Add(game, content);
        }

        public static List<DictionaryEntry> GetResourceNames()
        {
            var asm = Assembly.GetEntryAssembly();
            string resName = asm.GetName().Name + ".g.resources";
            using (var stream = asm.GetManifestResourceStream(resName))
            using (var reader = new System.Resources.ResourceReader(stream))
            {
                return reader.Cast<DictionaryEntry>().Select(entry => entry).ToList();
            }
        }


        SchemaFile LoadSchemaFile(string path)
        {
            if (!File.Exists(path))
                return null;

            _logger.Here().Information($"Loading schema file - {path}");
            var content = File.ReadAllText(path);
            var schema = JsonConvert.DeserializeObject<SchemaFile>(content);
            return schema;
        }

        SchemaFile LoadSchemaFileFromContent(string content)
        {
            _logger.Here().Information($"Loading schema from content");
            var schema = JsonConvert.DeserializeObject<SchemaFile>(content);
            return schema;
        }

        public void Initialize()
        {
        }
    }

    public class SchemaFile
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public GameTypeEnum GameEnum { get; set; }
        public int Version { get; set; } = 1;
        public Dictionary<string, List<DbTableDefinition>> TableDefinitions { get; set; } = new Dictionary<string, List<DbTableDefinition>>();
    }*/
}
