namespace Stampsy.Social
{
    public enum LoginProgress
    {
        Authorizing,
        PresentingAccountChoice,
        PresentingAuthController,
#if PLATFORM_IOS
        PresentingSafari
#endif
    }
}

