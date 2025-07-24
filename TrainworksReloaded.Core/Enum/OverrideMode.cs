namespace TrainworksReloaded.Core.Enum
{
    public enum OverrideMode
    {
        New,
        Replace,
        Append
    }

    public static class OverrideModeExtensions
    {
        public static bool IsNewContent(this OverrideMode overrideMode)
        {
            return overrideMode == OverrideMode.New;
        }

        public static bool IsOverriding(this OverrideMode overrideMode)
        {
            return overrideMode == OverrideMode.Replace || overrideMode == OverrideMode.Append;
        }
    }
}
