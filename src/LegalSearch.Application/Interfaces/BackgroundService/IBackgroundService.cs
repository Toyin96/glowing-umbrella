namespace LegalSearch.Application.Interfaces.BackgroundService
{
    public interface IBackgroundService
    {
        Task AssignRequestToSolicitors(Guid requestId);
    }
}
