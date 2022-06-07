using JsonToCsvHomeWork.Dtos;
using System.Data;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonToCsvHomeWork.Services
{
    public class JsonConverter
    {
        private const string Key = "Key";
        private const string Value = "Value";
        private const string NewValue = "NewValue";
        private DataTable _csvRepresentation;
        private List<DataTableRow> _dataTableObjectRows;
        private DataRow? _currentDataRow;
        private JsonNode? _jsonNode;
        /// <summary>
        /// Resets singletons private fields
        /// </summary>
        public void ResetWhenNewFileUploaded()
        {
            _dataTableObjectRows = new List<DataTableRow>();
            _csvRepresentation = new DataTable();
            var keyColumn = new DataColumn(Key)
            {
                Unique = true,
                AutoIncrement = false,
                AllowDBNull = false
            };
            _csvRepresentation.Columns.Add(keyColumn);
            _csvRepresentation.PrimaryKey = new[] { keyColumn };
            _csvRepresentation.Columns.Add(Value);
            _csvRepresentation.Columns.Add(NewValue);
        }

        /// <summary>
        /// Parse DataTable to list of DataTableRow objects for data transfer
        /// </summary>
        private void DataTableToJsonObjects()
        {
            foreach (DataRow row in _csvRepresentation.Rows)
            {
                _dataTableObjectRows.Add(
                    new DataTableRow
                    {
                        Key = row[Key].ToString(),
                        Value = row[Value].ToString(),
                        NewValue = row[NewValue].ToString()
                    });
            }
        }
        /// <summary>
        /// Update cell in data table with new value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        public void UpdateTable(string key, string newValue)
        {
            _csvRepresentation.Rows.Find(key)![NewValue] = newValue;
        }
        /// <summary>
        /// Get data transfer objects
        /// </summary>
        /// <returns></returns>
        public List<DataTableRow> GetCsvJsonRepresentation()
        {
            return _dataTableObjectRows;
        }

        /// <summary>
        /// Convert IFormFile to DataTable and (DataTableRowObjects for data transfer)
        /// </summary>
        /// <param name="file"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Convert(IFormFile file)
        {
            using Stream utf8Json = file.OpenReadStream();
            _jsonNode = JsonSerializer.Deserialize<JsonNode>(utf8Json);
            if (_jsonNode != null)
            {
                TraverseJson(_jsonNode);
                DataTableToJsonObjects();
            }
            else
            {
                throw new ArgumentNullException($"{utf8Json} failed to deserialize to json object");
            }
        }
        /// <summary>
        /// Add new row and fill with key and value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void FillRow(string key, string value)
        {
            var row = _csvRepresentation.NewRow();
            row[Key] = key;
            row[Value] = value;
            row[NewValue] = string.Empty;
            _csvRepresentation.Rows.Add(row);
        }

        /// <summary>
        /// Traverse through JsonObject to fill DataTable
        /// </summary>
        /// <param name="jsonNode"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void TraverseJson(JsonNode jsonNode)
        {
            switch (jsonNode)
            {
                case JsonObject jsonObject:
                    {
                        foreach (var (key, node) in jsonObject)
                        {
                            if (node is null)
                            {
                                FillRow($"{jsonObject.GetPath()}.{key}", string.Empty);
                            }
                            else
                            {
                                TraverseJson(node!);
                            }
                        }

                        break;
                    }

                case JsonArray jsonArray:
                    {
                        foreach (JsonNode? node in jsonArray)
                        {
                            if (node is null)
                            {
                                continue;
                            }
                            else
                            {
                                TraverseJson(node!);
                            }
                        }

                        break;
                    }

                case JsonValue jsonValue:
                    {
                        FillRow(jsonValue.GetPath(), jsonValue.ToJsonString().Trim('"'));
                        break;
                    }

                default:
                    throw new NotImplementedException($"Support for {jsonNode.GetType()} not implemented.");
            }
        }

        private void TraverseJsonAndUpdate(JsonNode jsonNode)
        {
            switch (jsonNode)
            {
                case JsonObject jsonObject:
                    {
                        bool isInCurrentObject = false;
                        foreach (var (key, node) in jsonObject)
                        {
                            if (node is null)
                            {
                                continue;
                            }
                            else
                            {
                                if (node.GetPath() == (string)_currentDataRow[Key])
                                {
                                    isInCurrentObject = true;
                                }
                                TraverseJsonAndUpdate(node!);
                            }
                        }
                        if (isInCurrentObject)
                        {
                            var propertyName = ((string)_currentDataRow[Key]).Substring(((string)_currentDataRow[Key]).LastIndexOf('.') + 1);
                            jsonObject[propertyName] = (string)_currentDataRow[NewValue];
                        }

                        break;
                    }

                case JsonArray jsonArray:
                    {
                        for (int i = 0; i < jsonArray.Count; i++)
                        {
                            if (jsonArray[i] is null)
                            {
                                continue;
                            }
                            else
                            {
                                if (jsonArray[i]!.GetPath() == (string)_currentDataRow[Key])
                                {
                                    jsonArray[i] = (string)_currentDataRow[NewValue];
                                }
                                TraverseJsonAndUpdate(jsonArray[i]!);
                            }
                        }
                        break;
                    }
                case JsonValue jsonValue:
                    {
                        break;
                    }

                default:
                    throw new NotImplementedException($"Support for {jsonNode.GetType()} not implemented.");
            }
        }

        public string GetUpdatedJson()
        {
            return JsonSerializer.Serialize(_jsonNode);
        }

        public void UpdateJsonNode()
        {
            foreach (DataRow row in _csvRepresentation.Rows)
            {
                if (row[NewValue].ToString() != String.Empty)
                {
                    _currentDataRow = row;
                    TraverseJsonAndUpdate(_jsonNode!);
                    row[NewValue] = String.Empty;
                }
            }
        }

        public string CreateJsonFromCsv()
        {
            JsonNode node = new JsonObject();
            foreach (DataRow row in _csvRepresentation.Rows)
            {
                _currentDataRow = row;
                TraverseJsonPath(node, ((string)row[Key]).Substring(2));
            }
            var json = JsonSerializer.Serialize(node);
            return json;
        }

        public void TraverseJsonPath(JsonNode node, string path)
        {
            if (path.Split('.').Length == 1)
            {
                node[path] = (string)_currentDataRow[Value];
            }
            else
            {
                string currentObjectName = path.Substring(0, path.IndexOf('.'));
                string nextObject = path.Substring(path.IndexOf('.') + 1);
                //if array
                if (currentObjectName.Contains('['))
                {
                    string key = currentObjectName.Substring(0, currentObjectName.Length - 3);
                    if (node is JsonArray)
                    {
                    }
                    else
                    {
                        if (!node.AsObject().ContainsKey(key))
                        {
                            node.AsObject().Add(key, new JsonArray());
                            TraverseJsonPath(node[key], $"{nextObject}.{node[key].AsArray().Count}");
                        }
                        else
                        {
                            TraverseJsonPath(node[key], $"{nextObject}.{node[key].AsArray().Count}");
                        }
                    }
                }
                else
                {
                    string key = currentObjectName;
                    if (node is JsonArray)
                    {
                        if (node.AsArray().Count == int.Parse(nextObject))
                        {
                            node.AsArray().Add(new JsonObject());
                            TraverseJsonPath(node[node.AsArray().Count - 1], key);
                        }
                        else
                        {
                            TraverseJsonPath(node[node.AsArray().Count - 1], key);
                        }

                    }
                    else
                    {
                        if (!node.AsObject().ContainsKey(key))
                        {
                            node.AsObject().Add(key, new JsonObject());
                            TraverseJsonPath(node[key], nextObject);
                        }
                        else
                        {
                            TraverseJsonPath(node[key], nextObject);
                        }
                    }
                }
            }
        }
    }
}
