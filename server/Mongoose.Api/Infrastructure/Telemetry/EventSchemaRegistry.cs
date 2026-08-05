using System.Collections;
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

            var eventsField = parsed is null ? null : GetField(parsed, "events");
            if (eventsField is not null)
            {
                var events = (dynamic)eventsField;

                foreach (var eventEntry in events)
                {
                    string key = eventEntry.Key.ToString();
                    var eventDef = (dynamic)eventEntry.Value;

                    string name = GetField(eventDef, "name")?.ToString() ?? key;
                    string category = GetField(eventDef, "category")?.ToString() ?? "system";
                    string versionStr = GetField(eventDef, "version")?.ToString() ?? "1";
                    var version = int.TryParse(versionStr, out var v) ? v : 1;
                    string retentionDaysStr = GetField(eventDef, "retentionDays")?.ToString() ?? "90";
                    var retentionDays = int.TryParse(retentionDaysStr, out var r) ? r : 90;
                    string piiSensitiveStr = GetField(eventDef, "piiSensitive")?.ToString() ?? "false";
                    var piiSensitive = bool.TryParse(piiSensitiveStr, out var p) ? p : false;
                    string description = GetField(eventDef, "description")?.ToString() ?? "";
                    string deprecatedStr = GetField(eventDef, "deprecated")?.ToString() ?? "false";
                    var deprecated = bool.TryParse(deprecatedStr, out var d) ? d : false;
                    string? replacement = GetField(eventDef, "replacement")?.ToString();

                    var allowedKeys = ExtractStringSet(GetField(eventDef, "allowedPayloadKeys"));
                    var requiredKeys = ExtractStringSet(GetField(eventDef, "requiredPayloadKeys"));
                    var keyTypes = ExtractKeyTypes(GetField(eventDef, "payloadKeyTypes"));

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
    /// Safely read a key from a YAML-parsed dynamic map, returning null instead of throwing when absent.
    /// (YamlDotNet's dynamic deserialization yields a Dictionary&lt;object, object&gt; whose indexer throws
    /// KeyNotFoundException on a miss, unlike a normal lookup.)
    /// </summary>
    private static object? GetField(dynamic map, string key)
    {
        if (map is IDictionary dict && dict.Contains(key))
        {
            return dict[key];
        }

        return null;
    }

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
                    types[key.ToString() ?? string.Empty] = dict[key]?.ToString() ?? "string";
                }
            }
            return types.ToImmutableDictionary();
        }
        catch { }

        return ImmutableDictionary<string, string>.Empty;
    }
}
