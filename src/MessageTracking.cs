using System;

namespace DotNetCore.CAP.Contrib.Idempotency
{
    public class MessageTracking
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}