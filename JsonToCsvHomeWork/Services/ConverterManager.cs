using System.Collections.Concurrent;

namespace JsonToCsvHomeWork.Services
{
    /// <summary>
    /// Responsible for creating new JsonCsvConverters for multiple clients running.
    /// </summary>
    public class ConverterManager
    {
        private readonly ConcurrentDictionary<Guid, JsonCsvConverter> _converters;
        public ConverterManager()
        {
            _converters = new ();
        }

        public JsonCsvConverter GetOrAdd(Guid clientId)
        {
            return _converters.GetOrAdd(clientId, new JsonCsvConverter());
        }

        public void Remove(Guid clientId)
        {
            _converters.TryRemove(clientId, out _);
        }
    }
}
