using UnityEngine;

public static class UserSession
{
    // Currently selected user for this play session
    public static string CurrentUserId;
    public static string CurrentUserName;
    public static string CurrentUserEmail;

    public static bool HasUser =>
        !string.IsNullOrEmpty(CurrentUserId);

    public static void Clear()
    {
        CurrentUserId = null;
        CurrentUserName = null;
        CurrentUserEmail = null;
    }
}
