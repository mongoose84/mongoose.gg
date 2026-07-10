using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Mongoose.Api.Core.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mongoose.Api.Infrastructure.Telemetry;

/// <summary>
/// Schema registry implementation - loads and validates events from YAML
/// </summary>
public class EventSchemaRegistry : IEventSchemaRegistry
{
    private readonly ILogger<EventSchemaRegistry> _logger;
    private readonly string _schemaFilePath;
    private ImmutableDictionary<string, EventSchema> _schemas = ImmutableDictionary<string, EventSchema>.Empty;
    private int _schemaVersion = 1;
    private DateTime _lastLoadTime = DateTime.MinValue;

    public EventSchemaRegistry(ILogger<EventSchemaRegistry> logger, string schemaFilePath)
    {
        _logger = logger;
        _schemaFilePath = schemaFilePath;
    }

    /// <summary>
    /// Load schema registry from YAML file
    /// </summary>
    public async Task ReloadAsync()
    {
        try
        {
            if (!File.Exists(_schemaFilePath))
            {
                _logger.LogError("Schema file not found: {SchemaPath}", _schemaFilePath);
                return;
            }

            var yaml = await File.ReadAllTextAsync(_schemaFilePath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            dynamic? parsed = deserializer.Deserialize<dynamic>(yaml);
            var schemas = new Dictionary<string, EventSchema>();

            if (parsed?["events"] is not null)
            {
                var events = (dynamic)parsed["events"];

                foreach (var eventEntry in events)
                {
                    var key = eventEntry.Key.ToString();
                    var eventDef = (dynamic)eventEntry.Value;

                    var name = eventDef["name"]?.ToString() ?? key;
                    var category = eventDef["category"]?.ToString() ?? "system";
                    var version = int.TryParse(eventDef["version"]?.ToString() ?? "1", out var v) ? v : 1;
                    var retentionDays = int.TryParse(eventDef["retention_days"]?.ToString() ?? "90", out var r) ? r : 90;
                    var piiSensitive = bool.TryParse(eventDef["pii_sensitive"]?.ToString() ?? "false", out var p) ? p : false;
                    var description = eventDef["description"]?.ToString() ?? "";
                    var deprecated = bool.TryParse(eventDef["deprecated"]?.ToString() ?? "false", out var d) ? d : false;
                    var replacement = eventDef["replacement"]?.ToString();

                    var allowedKeys = ExtractStringSet(eventDef["allowed_payload_keys"]);
                    var requiredKeys = ExtractStringSet(eventDef["required_payload_keys"]);
                    var keyTypes = ExtractKeyTypes(eventDef["payload_key_types"]);

                    var schema = new EventSchema(
                        name, category, version, retentionDays, piiSensitive,
                        allowedKeys, requiredKeys, keyTypes, description, deprecated, replacement
                    );

                    schemas[name] = schema;
                    _logger.LogDebug("Loaded schema: {EventName} (category: {Category})", name, category);
                }
            }

            _schemas = schemas.ToImmutableDictionary();
            _schemaVersion++;
            _lastLoadTime = DateTime.UtcNow;

            _logger.LogInformation("Loaded {Count} event schemas (v{Version})", _schemas.Count, _schemaVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load event schema registry");
        }
    }

    public EventSchema? GetSchema(string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
            return null;

        _schemas.TryGetValue(eventName, out var schema);
        return schema;
    }

    public IReadOnlyDictionary<string, EventSchema> GetAllSchemas() => _schemas;

    public bool IsRegistered(string eventName) => _schemas.ContainsKey(eventName);

    public int GetSchemaVersion() => _schemaVersion;

    /// <summary>
    /// Extract string set from YAML list
    /// </summary>
    private static IReadOnlySet<string> ExtractStringSet(dynamic? value)
    {
        if (value is null)
            return ImmutableHashSet<string>.Empty;

        try
        {
            if (value is IEnumerable<object> list)
                return list.Cast<string>().ToImmutableHashSet();
        }
        catch { }

        return ImmutableHashSet<string>.Empty;
    }

    /// <summary>
    /// Extract key types from YAML dict
    /// </summary>
    private static IReadOnlyDictionary<string, string> ExtractKeyTypes(dynamic? value)
    {
        if (value is null)
            return ImmutableDictionary<string, string>.Empty;

        try
        {
            var types = new Dictionary<string, string>();
            if (value is IDictionary dict)
            {
                foreach (var key in dict.Keys)
                {
                    types[key.ToString()] = dict[key]?.ToString() ?? "string";
                }
            }
            return types.ToImmutableDictionary();
        }
        catch { }

        return ImmutableDictionary<string, string>.Empty;
    }
}
