using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenWu.Core.Policy;

public sealed class PolicyModel
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("service")]
    public string Service { get; set; } = "MicrosoftUpdate";

    [JsonPropertyName("includeDrivers")]
    public bool IncludeDrivers { get; set; } = false;

    [JsonPropertyName("includeOptional")]
    public bool IncludeOptional { get; set; } = false;

    [JsonPropertyName("autoSelect")]
    public string AutoSelect { get; set; } = "SecurityOnly";

    [JsonPropertyName("reboot")]
    public string Reboot { get; set; } = "Never";

    [JsonPropertyName("hiddenKBs")]
    public List<string> HiddenKBs { get; set; } = new();

    [JsonPropertyName("denyTitlesContains")]
    public List<string> DenyTitlesContains { get; set; } = new() { "Preview" };

    [JsonPropertyName("allowOnDomainController")]
    public bool AllowOnDomainController { get; set; } = false;

    [JsonPropertyName("maxInstallBatch")]
    public int MaxInstallBatch { get; set; } = 50;

    [JsonPropertyName("searchTimeoutSec")]
    public int SearchTimeoutSec { get; set; } = 300;
}
