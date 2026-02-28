namespace TicketFlow.Shared.Ollama;

public class OllamaOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Profile { get; set; } = "gpu";
    public ModelProfiles Models { get; set; } = new();

    public string GetModel() => Profile.ToLowerInvariant() switch
    {
        "gpu" => Models.Gpu,
        "cpu" => Models.Cpu,
        "cpu-fast" => Models.CpuFast,
        _ => Models.Gpu
    };
}

public class ModelProfiles
{
    public string Gpu { get; set; } = "mistral";
    public string Cpu { get; set; } = "mistral";
    public string CpuFast { get; set; } = "phi3";
}
