using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DndChat.Models
{
    public class ChatUser : IdentityUser
    {

        [ProtectedPersonalData]
        public string? FirstName { get; set; }
        [ProtectedPersonalData]
        public string? LastName { get; set; }

        public string? DisplayName { get; set; }

    }
}
