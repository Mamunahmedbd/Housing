using System;

namespace house_management.Models
{
    public static class UserSession
    {
        public static User CurrentUser { get; private set; }

        public static bool IsAuthenticated => CurrentUser != null;

        public static bool IsInRole(UserRole role) => CurrentUser?.Role == role;

        public static bool IsAdmin => CurrentUser?.Role == UserRole.Admin;

        public static void SignIn(User user)
        {
            CurrentUser = user;
            OnSessionChanged?.Invoke(CurrentUser, EventArgs.Empty);
        }

        public static void SignOut()
        {
            CurrentUser = null;
            OnSessionChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Refresh(User updatedUser)
        {
            if (updatedUser != null && CurrentUser != null && updatedUser.Id == CurrentUser.Id)
            {
                CurrentUser = updatedUser;
                OnSessionChanged?.Invoke(CurrentUser, EventArgs.Empty);
            }
        }

        public static event EventHandler OnSessionChanged;
    }
}
