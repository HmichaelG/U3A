using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3A.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        private string _email;
        private string _userName;
        private bool _emailConfirmed;
        private string _password;
        private string _confirmPassword;
        public override string? Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    base.Email = value;
                    LastUpdated = DateTime.UtcNow;
                }
            }
        }

        public override string? UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    base.UserName = value;
                    LastUpdated = DateTime.UtcNow;
                }
            }
        }

        public override bool EmailConfirmed
        {
            get => _emailConfirmed;
            set
            {
                if (_emailConfirmed != value)
                {
                    _emailConfirmed = value;
                    base.EmailConfirmed = value;
                    LastUpdated = DateTime.UtcNow;
                }
            }
        }

        public DateTime? LastUpdated { get; set; }
        public DateTime? LastLogin { get; set; }

        [NotMapped]
        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    LastUpdated = DateTime.UtcNow;
                }
            }
        }
        [NotMapped]
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (_confirmPassword != value)
                {
                    _confirmPassword = value;
                    LastUpdated = DateTime.UtcNow;
                }
            }
        }

    }


}
