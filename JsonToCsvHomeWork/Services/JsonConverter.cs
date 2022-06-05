using JsonToCsvHomeWork.Dtos;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonToCsvHomeWork.Services
{
    public class JsonConverter
    {

        private const string Key = "Key";
        private const string Value = "Value";
        private const string NewValue = "NewValue";
        private readonly DataTable _csvRepresentation;
        private readonly List<DataTableRow> _dataTableObjectRows;
        private JsonNode? _jsonNode;
        public JsonConverter()
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
            //foreach (DataRow row in _csvRepresentation.Rows)
            //{
            //    if ((string)row[Key] == key)
            //    {
            //        row[Value] = newValue;
            //    }
            //}
            _csvRepresentation.Rows.Find(key)![Value] = newValue;
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
                        FillRow(jsonValue.GetPath(), jsonValue.ToJsonString());
                        break;
                    }

                default:
                    throw new NotImplementedException($"Support for {jsonNode.GetType()} not implemented.");
            }
        }

        public JsonNode ParseDataTableToJson()
        {
            foreach (DataRow row in _csvRepresentation.Rows)
            {
                int splitIndex = ((string)row[Key]).LastIndexOf(".");
                var currentObject = ((string)row[Key]).Substring(0, splitIndex);
                var currentValueName = ((string)row[Key]).Substring(splitIndex + 1);
                JsonNode node = new JsonObject()
                {
                    [currentValueName] = row[Value].ToString()
                };

            }
            return null;
        }
    }
}
