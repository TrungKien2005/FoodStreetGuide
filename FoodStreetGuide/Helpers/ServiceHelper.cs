namespace doanC_.Helpers
{
    public static class ServiceHelper
    {
        public static T GetService<T>() where T : class
        {
            return IPlatformApplication.Current?.Services.GetService<T>();
        }
    }
}