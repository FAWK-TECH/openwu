using System;
using System.IO;
using System.Text.Json;

namespace OpenWu.Core.Policy;

public sealed class PolicyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public string PolicyPath => _path;

    public PolicyStore(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            _path = customPath;
        }
        else
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            _path = Path.Combine(programData, "OpenWU", "policy.json");
        }
    }

    public PolicyModel Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var text = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize<PolicyModel>(text, JsonOptions);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Fallback to default on error
        }

        return GetDefaults();
    }

    public void Save(PolicyModel policy)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(policy, JsonOptions);
        File.WriteAllText(_path, json);
    }

    public void Reset()
    {
        Save(GetDefaults());
    }

    public static PolicyModel GetDefaults()
    {
        return new PolicyModel();
    }
}
