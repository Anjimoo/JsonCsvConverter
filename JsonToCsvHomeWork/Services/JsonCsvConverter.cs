using JsonToCsvHomeWork.Dtos;
using System.Data;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonToCsvHomeWork.Services
{
    public class JsonCsvConverter
    {
        private const string Key = "Key";
        private const string Value = "Value";
        private const string NewValue = "NewValue";
        private DataTable _csvRepresentation;
        private DataRow? _currentDataRow;
        private JsonNode? _jsonNode;

        public JsonCsvConverter()
        {
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
        /// Reads rows from DataTable(csv)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<(string Key, string Value, string NewValue)> ReadRows()
        {
            using DataTableReader reader = _csvRepresentation.CreateDataReader();
            while (reader.Read())
            {
                yield return (reader.GetString(0), reader.GetString(1), reader.GetString(2));
            }
        }

        /// <summary>
        /// Update cell in data table with new value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        public void UpdateTable(string key, string newValue)
        {
            _csvRepresentation.Rows.Find(key)![Value] = newValue;
        }

        /// <summary>
        /// Convert IFormFile to DataTable and (DataTableRowObjects for data transfer)
        /// </summary>
        /// <param name="file"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void ConvertJsonFileToCsv(Stream utf8Json)
        {
            _jsonNode = JsonSerializer.Deserialize<JsonNode>(utf8Json);
            if (_jsonNode != null)
            {
                TraverseJson(_jsonNode);
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
                                continue;
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
                        RemoveNullsFromArray(jsonArray);
                        foreach (JsonNode? node in jsonArray)
                        {
                            TraverseJson(node!);
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

        /// <summary>
        /// Removes null objects from JsonArray 
        /// </summary>
        /// <param name="jsonArray"></param>
        private void RemoveNullsFromArray(JsonArray jsonArray)
        {
            var nullIndexes = new List<int>();
            for (int i = 0; i < jsonArray.Count; i++)
            {
                if (jsonArray[i] == null)
                {
                    nullIndexes.Add(i);
                }
            }

            for (int i = 0; i<nullIndexes.Count; i++)
            {
                int nullIndex = nullIndexes[i];
                jsonArray.RemoveAt(nullIndex - i);
            }
        }

        /// <summary>
        /// Creates new json string from DataTable
        /// </summary>
        /// <returns></returns>
        public JsonNode CreateJsonFromCsv()
        {
            JsonNode node = new JsonObject();
            foreach (DataRow row in _csvRepresentation.Rows)
            {
                _currentDataRow = row;
                TraverseJsonPath(node, ((string)row[Key]).Substring(2));
            }
            return node;
        }

        private static bool IsLeafNode(string path)
        {
            return path.Split('.').Length == 1;
        }

        /// <summary>
        /// Traverse path of an object and create new JsonValue/JsonObject/JsonArray
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="path">Path to the object in for of objectName.ProperyName</param>
        /// <param name="currentObjectIndex"></param>
        public void TraverseJsonPath(JsonNode parentNode, string path, int? currentObjectIndex = null)
        {
            if (IsLeafNode(path))
            {
                if (parentNode is JsonArray jsonArray)
                {
                    if (IsArray(path))
                    {
                        AddJsonValueToArray(path, currentObjectIndex, jsonArray);
                    }
                    else
                    {
                        AddPropertyToObjectInArray(path, currentObjectIndex, jsonArray);
                    }
                }
                else // parent Node is JsonObject
                {
                    if (IsArray(path)) 
                    {
                        var currentArrayName = GetNameWithoutBrackets(path);
                        AddJsonValueToArrayInObject(currentArrayName, parentNode.AsObject());
                    }
                    else
                    {
                        parentNode[path] = (string)_currentDataRow![Value];
                    }
                }
            }
            else
            {
                string currentObjectName = path.Substring(0, path.IndexOf('.'));
                string nextObjectInPath = path.Substring(path.IndexOf('.') + 1);

                if (IsArray(currentObjectName))
                {
                    string key = GetNameWithoutBrackets(currentObjectName);
                    if (parentNode is JsonArray jsonArray)
                    {
                        TraverseJsonArray(currentObjectIndex, currentObjectName, nextObjectInPath, key, jsonArray);
                    }
                    else
                    {
                        TraverseJsonObjectToAddArray(parentNode, currentObjectName, nextObjectInPath, key);
                    }
                }
                else //current object is an object
                {
                    if (!parentNode.AsObject().ContainsKey(currentObjectName))
                    {
                        parentNode.AsObject().Add(currentObjectName, new JsonObject());
                        TraverseJsonPath(parentNode[currentObjectName], nextObjectInPath);
                    }
                    else
                    {
                        TraverseJsonPath(parentNode[currentObjectName], nextObjectInPath);
                    }
                }
            }
        }

        private static bool IsArray(string jsonPropertyName)
        {
            return jsonPropertyName.EndsWith(']');
        }

        /// <summary>
        /// Traverses through JsonObject and adds new JsonArray property if it does not exist.
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="currentObjectName"></param>
        /// <param name="nextObjectInPath"></param>
        /// <param name="propertyName"></param>
        private void TraverseJsonObjectToAddArray(JsonNode currentNode, string currentObjectName, string nextObjectInPath, string propertyName)
        {
            int indexOfArray = ExtractIndexFromName(currentObjectName);
            if (!currentNode.AsObject().ContainsKey(propertyName))
            {
                currentNode.AsObject().Add(propertyName, new JsonArray());
                TraverseJsonPath(currentNode[propertyName], nextObjectInPath, indexOfArray);
            }
            else
            {
                TraverseJsonPath(currentNode[propertyName], nextObjectInPath, indexOfArray);
            }
        }

        /// <summary>
        /// Extracts index from name that looks like: "name[0]"
        /// </summary>
        /// <param name="currentObjectName"></param>
        /// <returns></returns>
        private static int ExtractIndexFromName(string currentObjectName)
        {
            int indexOfNumberInName = currentObjectName.IndexOf('[') + 1;
            int indexOfArray = int.Parse(currentObjectName[indexOfNumberInName].ToString());
            return indexOfArray;
        }

        /// <summary>
        /// Traverses through JsonArray. 
        /// Adds JsonArray to current JsonObject if it does not exist 
        /// or adds JsonObject to array that does not have enough space for this object.
        /// </summary>
        /// <param name="currentObjectIndex"></param>
        /// <param name="currentArrayName"></param>
        /// <param name="nextObjectName"></param>
        /// <param name="propertyName"></param>
        /// <param name="jsonArray"></param>
        private void TraverseJsonArray(
            int? currentObjectIndex,
            string currentArrayName,
            string nextObjectName,
            string propertyName,
            JsonArray jsonArray)
        {
            int indexOfCurrentObject = ExtractIndexFromName(currentArrayName);
            if (jsonArray.Count - 1 == currentObjectIndex)
            {
                var currentObject = jsonArray[currentObjectIndex.Value].AsObject();
                if (!currentObject.ContainsKey(propertyName))
                {
                    currentObject.Add(propertyName, new JsonArray());
                    TraverseJsonPath(currentObject[propertyName], nextObjectName, indexOfCurrentObject);
                }
                else
                {
                    if (currentObject[propertyName].AsArray().Count <= indexOfCurrentObject)
                    {
                        //add new json object to array if array is not big enough for next object
                        currentObject[propertyName].AsArray().Add(new JsonObject());
                    }
                    TraverseJsonPath(currentObject[propertyName], nextObjectName, indexOfCurrentObject);
                }
            }
        }

        /// <summary>
        /// Adds new property to JsonObject in JsonArray 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="currentObjectIndex"></param>
        /// <param name="jsonArray"></param>
        private void AddPropertyToObjectInArray(string propertyName, int? currentObjectIndex, JsonArray jsonArray)
        {
            if (jsonArray.Count <= currentObjectIndex)
            {
                jsonArray.Add(new JsonObject()
                {
                    [propertyName] = (string)_currentDataRow[Value]
                });
            }
            else
            {
                jsonArray[currentObjectIndex.Value][propertyName] = (string)_currentDataRow[Value];
            }
        }

        /// <summary>
        /// Adds JsonValue to current JsonArray or creates new JsonArray in current object and then adds JsonValue
        /// </summary>
        /// <param name="currentArray"></param>
        /// <param name="currentObjectIndex"></param>
        /// <param name="jsonArray"></param>
        private void AddJsonValueToArray(string currentArray, int? currentObjectIndex, JsonArray jsonArray)
        {
            var arrayNameWithoutBrackets = GetNameWithoutBrackets(currentArray);
            var currentObject = jsonArray[currentObjectIndex.Value].AsObject();
            AddJsonValueToArrayInObject(arrayNameWithoutBrackets, currentObject);
        }

        private  static string GetNameWithoutBrackets(string nameWithBrackets)
        {
            return nameWithBrackets[..^3];//.Substring(0, nameWithBrackets.Length - 3);
        }

        /// <summary>
        /// Adds new JsonValue to JsonArray in current object
        /// </summary>
        /// <param name="arrayNameWithoutBrackets"></param>
        /// <param name="currentObject">Object with array of JsonValues</param>
        private void AddJsonValueToArrayInObject(string arrayNameWithoutBrackets, JsonObject currentObject)
        {
            if (!currentObject.ContainsKey(arrayNameWithoutBrackets))
            {
                currentObject.Add(arrayNameWithoutBrackets,
                    new JsonArray(JsonValue.Create(_currentDataRow[Value])));
            }
            else
            {
                currentObject[arrayNameWithoutBrackets].AsArray().Add(JsonValue.Create(_currentDataRow[Value]));
            }
        }
    }
}
