using System.Linq;
using System.Text;
using System.Windows.Documents;

namespace Editors.DatabaseEditor.FileFormats
{
    public static class DbScemaValidation
    {
        public static string Validate(DbSchema dbSchema)
        {
            GenerateGexf(dbSchema);

            var res0 = BuildRelationshipMap(dbSchema);
            var res1 = FindTablesWithoutReferences(dbSchema);
            var res2 = FindIncommingReferences(dbSchema);

         
            return res0;
        }

        private static void GenerateGexf(DbSchema dbSchema)
        {
            var fkList = dbSchema.TableSchemas
               .SelectMany(x => x.Coloumns.Select(y => y.ForeignKey))
               .Where(x => x != null)
               .ToList();

            var idMap = new Dictionary<string, int>();
            for (var i = 0; i < dbSchema.TableSchemas.Count; i++)
            {
                idMap.Add(dbSchema.TableSchemas[i].Name, i + 1);
            }

            var edges = new List<string>();
            


            var output = new StringBuilder();
            output.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            output.AppendLine("<gexf xmlns=\"http://www.gexf.net/1.2draft\" version=\"1.2\">");
            output.AppendLine("  <graph mode=\"static\" defaultedgetype=\"directed\">");
            
            output.AppendLine("    <nodes>");

            foreach (var dbEntry in idMap)
            {
                var incommingConnections = fkList.Where(x => x.Table + "_tables" == dbEntry.Key).ToList();

                var size = incommingConnections.Count;
                size = int.Max(size, 1);
                output.AppendLine($"       <node id=\"{dbEntry.Value}\" label=\"{dbEntry.Key}\">");
                output.AppendLine($"            <viz:size value=\"{size}\"/>");
                output.AppendLine("        </node>");
            }


            output.AppendLine("    </nodes>");

            output.AppendLine("    <edges>");

            var edgeIdCounter = 1;
            foreach (var table in dbSchema.TableSchemas)
            {
                var fks = table.Coloumns
                    .Select(x => x.ForeignKey)
                    .Where(x => x != null)
                    .ToList();

                foreach (var fk in fks)
                {
                    var fkTarget = fk.Table + "_tables";
                    if (idMap.TryGetValue(fkTarget, out var fkTargetId) == false)
                        continue;


                    var id0 = idMap[table.Name];
                    var id1 = fkTargetId;
                    var lable = "";
                    output.AppendLine($"      <edge id=\"{edgeIdCounter++}\" source=\"{id0}\" target=\"{id1}\" label=\"{lable}\" />");
                }

            }


            output.AppendLine("    </edges>");

            output.AppendLine("  </graph>");
            output.AppendLine("</gexf>");




            File.WriteAllText(@"C:\Users\ole_k\AssetEditor\warhammer3_db.gexf", output.ToString());
        }

        private static string FindIncommingReferences(DbSchema dbSchema)
        {
            var fkList = dbSchema.TableSchemas
               .SelectMany(x => x.Coloumns.Select(y => y.ForeignKey))
               .Where(x => x != null)
               .ToList();

            var tableConnections = new Dictionary<string, List<string>>();    
            foreach (var table in dbSchema.TableSchemas)
            {
                var incommingConnections = fkList.Where(x => x.Table + "_tables" == table.Name).ToList();
                if (incommingConnections.Any())
                {
                    var prettyIncomming = incommingConnections.Select(x => $"").ToList();
                    tableConnections.Add(table.Name, prettyIncomming);
                }
            }

            var res = tableConnections.Select(x=> (x.Key, x.Value)).ToList();
            var orderedRes = res.OrderByDescending(x => x.Value.Count).ToList();

            return "";
        }

        private static object FindTablesWithoutReferences(DbSchema dbSchema)
        {
            var sb = new StringBuilder();
            var fkList = dbSchema.TableSchemas
                .SelectMany(x => x.Coloumns.Select(y => y.ForeignKey))
                .Where(x => x != null)
                .Select(x=>x!.Table + "_tables")
                .ToList();

            foreach (var table in dbSchema.TableSchemas)
            {
                var r = fkList.Contains(table.Name);
                if (r == false)
                    sb.AppendLine(table.Name);
            }

            var res = sb.ToString();
            return res;
        }

       



        // Sort Tables by referces
        // Find tabels not refernces


        private static string BuildRelationshipMap(DbSchema dbSchema)
        {
            var tables = dbSchema.TableSchemas
                .OrderByDescending(x => x.Version)
                .GroupBy(x => x.Name)
                .Select(x => x.First())
                .ToList();


            var output = new Dictionary<string, string>();
            var sb = new StringBuilder();

            foreach (var table in tables)
            {

                if (table.Name == "factions_tables")
                {
                }

                var isTablePrinted = false;
                foreach (var column in table.Coloumns)
                {
                    if (column.Name == "WAAAGH_FACTION".ToLower())
                    { 
                    
                    }

                    var coloumnOutput = new List<string>();
                    RefChain(tables, table.Name, column.Name, coloumnOutput);

                    if (coloumnOutput.Count != 0)
                    {
                        if (isTablePrinted == false)
                        {
                            isTablePrinted = true;
                            sb.AppendLine(table.Name + "_" + table.Version);
                        }

                        var str = string.Join(" => ", coloumnOutput);
                        var header = $"\t\t[{coloumnOutput.Count}] {column.Name}";
                        sb.AppendLine($"{header.ToUpper()}  :  {str}");
                    }
                }

                if (isTablePrinted)
                    sb.AppendLine();
            }

            var res = sb.ToString();
            return res;
        }

        static void RefChain(IEnumerable<DBTableSchema> db, string currentTableName, string currentColoumnName, List<string> output)
        {
            if (output.Count >= 100)
                return;

            var table = db.FirstOrDefault(x => x.Name == currentTableName);
            if (table == null)
            {
                output.Add($"Missing Table - {currentTableName}");
                return;
            }
               
            var coloumn = table.Coloumns.First(x=>x.Name == currentColoumnName);

            if (coloumn.ForeignKey != null)
            {
                var tableName = coloumn.ForeignKey.Table + "_tables";
                output.Add($"{tableName}.{coloumn.ForeignKey.ColoumnName}");

                RefChain(db, tableName, coloumn.ForeignKey.ColoumnName, output);
            }
        }
    }
}

// SkeletonTable
//  [3] SkeletonTable.BoneID => [BoneTable.Id] => [AnimationTable.Id] => [MovementTable.Id]
