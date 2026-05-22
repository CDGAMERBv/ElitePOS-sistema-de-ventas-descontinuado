using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElitePOS.Services
{
    public abstract class BaseFirestoreService<T> where T : class
    {
        protected readonly HttpClient _http;
        protected readonly string _projectId = "TU_FIREBASE_PROJECT_ID";
        protected abstract string CollectionName { get; }
        
        protected readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        public BaseFirestoreService(HttpClient http)
        {
            _http = http;
        }

        protected string GetBaseUrl() => 
            $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/{CollectionName}";

        public async Task<T?> GetDocumentAsync(string documentId)
        {
            try
            {
                var response = await _http.GetAsync($"{GetBaseUrl()}/{documentId}");
                if (!response.IsSuccessStatusCode) return null;

                var firestoreDoc = await response.Content.ReadFromJsonAsync<FirestoreDocumentResponse>(_jsonOptions);
                return firestoreDoc?.Fields != null ? MapFromFirestore(firestoreDoc.Fields) : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? [BaseFirestoreService] Error al obtener documento {documentId}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SaveDocumentAsync(string documentId, T entity, bool isUpdate = true)
        {
            try
            {
                var firestoreFields = MapToFirestore(entity);
                var payload = new { fields = firestoreFields };
                
                string url = $"{GetBaseUrl()}/{documentId}";
                
                if (isUpdate)
                {
                    var updateMask = GenerateUpdateMask(entity);
                    var response = await _http.PatchAsJsonAsync($"{url}?{updateMask}", payload, _jsonOptions);
                    return response.IsSuccessStatusCode;
                }
                else
                {
                    var response = await _http.PostAsJsonAsync($"{GetBaseUrl()}?documentId={documentId}", payload, _jsonOptions);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? [BaseFirestoreService] Error al guardar documento {documentId}: {ex.Message}");
                return false;
            }
        }

        private Dictionary<string, object> MapToFirestore(T entity)
        {
            var fields = new Dictionary<string, object>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;

                var value = prop.GetValue(entity);
                if (value == null) continue;

                var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                var fieldName = attr?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(prop.Name);

                var firestoreValue = ConvertToFirestoreValue(value);
                if (firestoreValue != null) fields[fieldName] = firestoreValue;
            }

            return fields;
        }

        private T MapFromFirestore(Dictionary<string, FirestoreValueResponse> fields)
        {
            var entity = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                var fieldName = attr?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(prop.Name);

                if (fields.TryGetValue(fieldName, out var firestoreValue))
                {
                    var value = ConvertFromFirestoreValue(firestoreValue, prop.PropertyType);
                    if (value != null) prop.SetValue(entity, value);
                }
            }

            return entity;
        }

        private object? ConvertToFirestoreValue(object value)
        {
            return value switch
            {
                string s => new { stringValue = s },
                bool b => new { booleanValue = b },
                int i => new { integerValue = i.ToString() },
                long l => new { integerValue = l.ToString() },
                decimal d => new { doubleValue = (double)d },
                double dbl => new { doubleValue = dbl },
                DateTime dt => new { timestampValue = dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                _ => null
            };
        }

        private object? ConvertFromFirestoreValue(FirestoreValueResponse val, Type targetType)
        {
            if (val.StringValue != null) return val.StringValue;
            if (val.BooleanValue != null) return val.BooleanValue;
            if (val.IntegerValue != null && (targetType == typeof(int) || targetType == typeof(int?))) return int.Parse(val.IntegerValue);
            if (val.IntegerValue != null && (targetType == typeof(long) || targetType == typeof(long?))) return long.Parse(val.IntegerValue);
            if (val.DoubleValue != null) 
            {
                if (targetType == typeof(decimal) || targetType == typeof(decimal?)) return (decimal)Convert.ToDouble(val.DoubleValue);
                return Convert.ToDouble(val.DoubleValue);
            }
            if (val.TimestampValue != null && (targetType == typeof(DateTime) || targetType == typeof(DateTime?))) return DateTime.Parse(val.TimestampValue);
            return null;
        }

        private string GenerateUpdateMask(T entity)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var paths = new List<string>();

            foreach (var prop in properties)
            {
                if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;
                var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                var fieldName = attr?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
                paths.Add($"updateMask.fieldPaths={fieldName}");
            }

            return string.Join("&", paths);
        }
    }

    internal class FirestoreDocumentResponse
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("fields")] public Dictionary<string, FirestoreValueResponse>? Fields { get; set; }
    }

    internal class FirestoreValueResponse
    {
        public string? StringValue { get; set; }
        public bool? BooleanValue { get; set; }
        public string? IntegerValue { get; set; }
        public object? DoubleValue { get; set; }
        public string? TimestampValue { get; set; }
    }
}


