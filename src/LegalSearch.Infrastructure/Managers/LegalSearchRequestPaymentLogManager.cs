using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Managers
{
    internal class LegalSearchRequestPaymentLogManager : ILegalSearchRequestPaymentLogManager
    {
        private readonly AppDbContext _appDbContext;

        public LegalSearchRequestPaymentLogManager(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task<bool> AddLegalSearchRequestPaymentLog(LegalSearchRequestPaymentLog legalSearchRequestPaymentLog)
        {
            _appDbContext.LegalSearchRequestPaymentLogs.Add(legalSearchRequestPaymentLog);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<LegalSearchRequestPaymentLog>> GetAllLegalSearchRequestPaymentLogNotYetCompleted()
        {
            var paymentRecords = await _appDbContext.LegalSearchRequestPaymentLogs
                .Where(x => x.PaymentStatus != PaymentStatusType.PaymentMade)
                .ToListAsync();

            return paymentRecords  ?? Enumerable.Empty<LegalSearchRequestPaymentLog>();
        }
    }
}
