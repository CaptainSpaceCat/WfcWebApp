
namespace WfcWebApp.Wfc {

public class WfcConfig
{
    public bool Wrap { get; set; }
    public bool RotationalSymmetry { get; set; }
    public int OutputWidth { get; set; }
    public int OutputHeight { get; set; }


    public static WfcConfig Default = new() {
      Wrap = true,
      RotationalSymmetry = true,
      OutputWidth = 64,
      OutputHeight = 64
    };

    public static WfcConfig NoWrap = new() {
      Wrap = false,
      RotationalSymmetry = true,
      OutputWidth = 64,
      OutputHeight = 64
    };
}

}

