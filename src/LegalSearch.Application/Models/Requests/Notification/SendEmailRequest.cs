using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Application.Models.Requests.Notification
{
    public class SendEmailRequest
    {
        public required string From { get; set; }
        public required string To { get; set; }
        public List<string> Cc { get; set; }
        public List<string> Bcc { get; set; }
        public required string Subject { get; set; }
        public required string Body { get; set; }
        public List<IFormFile> Attachment { get; set; }
    }
}
