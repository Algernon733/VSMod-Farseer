using Vintagestory.API.Client;
using System;
using Vintagestory.API.Config;

namespace Farseer.Client;

public class FarseerClient : IDisposable
{
  readonly FarseerModSystem modSystem;
  readonly ICoreClientAPI capi;
  readonly FarseerClientConfig config;
  FarseerClientConfig configOnLastLoad;

  readonly FarRegionRenderer renderer;

  readonly FarseerConfigDialog configDialog;

  public FarseerClientConfig Config => config;

  public FarseerClient(FarseerModSystem modSystem, ICoreClientAPI capi)
  {
    this.modSystem = modSystem;
    this.capi = capi;

    var channel = capi.Network.GetChannel(FarseerModSystem.MOD_CHANNEL_NAME);
    channel.SetMessageHandler<FarRegionData>(OnReceiveFarRegionData);
    channel.SetMessageHandler<FarRegionUnload>(OnReceiveFarRegionUnload);

    try
    {
      config = capi.LoadModConfig<FarseerClientConfig>("farseer-client.json");
      config ??= new FarseerClientConfig();
      capi.StoreModConfig(config, "farseer-client.json");
    }
    catch (Exception e)
    {
      modSystem.Mod.Logger.Error("Could not load config! Loading default settings instead.");
      modSystem.Mod.Logger.Error(e);
      config = new FarseerClientConfig();
    }

    configOnLastLoad = config.Clone();

    renderer = new FarRegionRenderer(modSystem, capi);
    configDialog = new FarseerConfigDialog(modSystem, capi);

    capi.Input.RegisterHotKey(
            "toggleFarseerConfig",
            Lang.Get("farseer:toggle-config"),
            GlKeys.F,
            HotkeyType.GUIOrOtherControls,
            false, // Alt
            true, // Control
            true // Shift
    );
    capi.Input.SetHotKeyHandler("toggleFarseerConfig", ToggleConfigDialog);

    capi.Event.LevelFinalize += Init;
  }

  public void SaveConfigChanges()
  {
    capi.StoreModConfig(config, "farseer-client.json");

    if (config.ShouldShareWithServer(configOnLastLoad))
    {
      var channel = capi.Network.GetChannel(FarseerModSystem.MOD_CHANNEL_NAME);
      if (channel != null)
      {
        if (config.Enabled)
        {
          channel.SendPacket(new FarseerEnable
          {
            PlayerConfig = config.ToServerPlayerConfig(),
          });
        }
        else
        {
          channel.SendPacket(new FarseerDisable());
        }
      }
    }

    if (config.FarViewDistance != configOnLastLoad.FarViewDistance)
    {
      // re-init renderer so that zfar is updated
      renderer.Init();
    }

    if (configOnLastLoad.Enabled && !config.Enabled)
    {
      renderer.ClearLoadedRegions();
    }

    configOnLastLoad = config.Clone();
    modSystem.Mod.Logger.Notification("Saved client config changes.");
  }

  private bool ToggleConfigDialog(KeyCombination _)
  {
    configDialog.Toggle();
    return true;
  }

  private void OnReceiveFarRegionData(FarRegionData data)
  {
    if (config.Enabled)
    {
      renderer.BuildRegion(data);
    }
  }

  private void OnReceiveFarRegionUnload(FarRegionUnload packet)
  {
    if (config.Enabled)
    {
      foreach (var idx in packet.RegionIndices)
      {
        renderer.UnloadRegion(idx);
      }
    }
  }

  public void Init()
  {
    var channel = capi.Network.GetChannel(FarseerModSystem.MOD_CHANNEL_NAME);
    channel?.SendPacket(new FarseerEnable
    {
      PlayerConfig = config.ToServerPlayerConfig(),
    });
    renderer.Init();
  }

  public void Dispose()
  {
    renderer?.Dispose();
    GC.SuppressFinalize(this);
  }
}
