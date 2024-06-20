﻿namespace InterModClient;

[HarmonyPatch]
public class PatchClass
{
    #region Settings
    const int RETRIES = 10;

    public static Settings Settings = new();
    static string settingsPath => Path.Combine(Mod.ModPath, "Settings.json");
    private FileInfo settingsInfo = new(settingsPath);

    private JsonSerializerOptions _serializeOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private void SaveSettings()
    {
        string jsonString = JsonSerializer.Serialize(Settings, _serializeOptions);

        if (!settingsInfo.RetryWrite(jsonString, RETRIES))
        {
            ModManager.Log($"Failed to save settings to {settingsPath}...", ModManager.LogLevel.Warn);
            Mod.State = ModState.Error;
        }
    }

    private void LoadSettings()
    {
        if (!settingsInfo.Exists)
        {
            ModManager.Log($"Creating {settingsInfo}...");
            SaveSettings();
        }
        else
            ModManager.Log($"Loading settings from {settingsPath}...");

        if (!settingsInfo.RetryRead(out string jsonString, RETRIES))
        {
            Mod.State = ModState.Error;
            return;
        }

        try
        {
            Settings = JsonSerializer.Deserialize<Settings>(jsonString, _serializeOptions);
        }
        catch (Exception)
        {
            ModManager.Log($"Failed to deserialize Settings: {settingsPath}", ModManager.LogLevel.Warn);
            Mod.State = ModState.Error;
            return;
        }
    }
    #endregion

    #region Start/Shutdown
    public void Start()
    {
        //Need to decide on async use
        Mod.State = ModState.Loading;
        LoadSettings();

        if (Mod.State == ModState.Error)
        {
            ModManager.DisableModByPath(Mod.ModPath);
            return;
        }

        Mod.State = ModState.Running;
    }

    public void Shutdown()
    {
        //if (Mod.State == ModState.Running)
        // Shut down enabled mod...

        //If the mod is making changes that need to be saved use this and only manually edit settings when the patch is not active.
        //SaveSettings();

        if (Mod.State == ModState.Error)
            ModManager.Log($"Improper shutdown: {Mod.ModPath}", ModManager.LogLevel.Error);
    }
    #endregion

    [CommandHandler("test", AccessLevel.Player, CommandHandlerFlag.None)]
    public static void HandleInterModTest(Session session, params string[] parameters)
    {
        ModManager.Log($"{InterModHost.HostPatchClass.Counter++}");

        //...or get the ModContainer -> IHarmonyMod -> as the type of the desired mod -> do things
        var mod = ModManager.GetModContainerByName(nameof(InterModHost));
        if (mod is null)
        {
            ModManager.Log($"Host not found.");
            return;
        }

        if (mod.Instance is InterModHost.HostPatchClass host)
            ModManager.Log($"Found host: {host.InstanceCounter++}");
        else
        {
            try
            {
                var castHost = (InterModHost.HostPatchClass)mod.Instance;
                ModManager.Log($"Found host: {castHost.InstanceCounter++}");
                return;
            }
            catch (Exception ex)
            {
                ModManager.Log(ex.Message, ModManager.LogLevel.Error);
            }
        }

        dynamic dHost = mod.Instance;
        dynamic dPatch = dHost.Patch;
        ModManager.Log($"Dynamic counter: {dPatch.InstanceCounter++}");
    }
}

