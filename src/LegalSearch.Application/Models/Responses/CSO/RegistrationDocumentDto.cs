namespace LegalSearch.Application.Models.Responses.CSO
{
    public class RegistrationDocumentDto
    {
        public required string FileName { get; set; }
        public required string FileType { get; set; }
        public required byte[] FileContent { get; set; }
    }
}
