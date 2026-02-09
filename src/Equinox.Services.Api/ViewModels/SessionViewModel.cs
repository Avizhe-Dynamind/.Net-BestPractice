using System;

namespace Equinox.Services.Api.ViewModels
{
    public class SessionViewModel
    {
        public Guid Id { get; set; }
        public string DeviceName { get; set; }
        public string IpAddress { get; set; }
        public string Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public bool IsCurrent { get; set; }
    }
}
