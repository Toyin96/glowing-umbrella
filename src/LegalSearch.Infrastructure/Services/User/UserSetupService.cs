﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.User.Events;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LegalSearch.Infrastructure.Services.User
{
    public class UserSetupService : IUserSetupService
    {
        private readonly UserManager<Solicitor> userManager;
        private readonly AppDbContext dbContext;
        private readonly ISessionService sessionService;
        private readonly ILogger<UserSetupService> logger;
        private readonly IMediator mediator;

        public UserSetupService(AppDbContext dbContext, ISessionService sessionService, ILogger<UserSetupService> logger, IMediator mediator, UserManager<Solicitor> userManager)
        {
            this.dbContext = dbContext;
            this.sessionService = sessionService;
            this.logger = logger;
            this.mediator = mediator;
            this.userManager = userManager;
        }

        public async Task<ObjectResponse<SolicitorOnboardResponse>> OnboardSolicitorAsync(SolicitorOnboardRequest request)
        {
            var session = sessionService.GetUserSession();

            if (session is null) return new ObjectResponse<SolicitorOnboardResponse>("User Is Unauthenticated", ResponseCodes.Unauthenticated);

            var solicitor = new Solicitor
            {
                Address = request.Address, Email = request.Email,
                FirmId = request.FirmId, LgaId = request.LgaId,
                StateId = request.StateId, BankAccountId = request.BankAccountId,
                FirstName = request.FirstName, LastName = request.LastName,
                PhoneNumber = request.PhoneNumber
            };

            var defaultPassword = Helpers.GenerateDefaultPassword();
            
            var (createStatus, createMessage) = await CreateSolicitor(solicitor, defaultPassword);

            if (!createStatus)
                return new ObjectResponse<SolicitorOnboardResponse>(createMessage, ResponseCodes.ServiceError);
            
            _ = mediator.Publish(new SolicitorCreatedEvent(solicitor, defaultPassword));

            var userResponse = await GetSolicitorDetails(solicitor.Id);
            
            return new ObjectResponse<SolicitorOnboardResponse>("Successfully Created Solicitor")
            {
                Data = userResponse
            };
        }
        
        private async Task<(bool, string)> CreateSolicitor(Solicitor solicitor, string defaultPassword)
        {
            var result = await userManager.CreateAsync(solicitor, defaultPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Could not successfully create the solicitor", result);
                // log error
                return (false, result.Errors.FirstOrDefault()?.Description);
            }

            logger.LogInformation("Successfully created solicitor with default password");
            return (true, string.Empty);
        }
        
        private async Task<SolicitorOnboardResponse> GetSolicitorDetails(Guid solicitorId)
        {
            var solicitor = await (from user in dbContext.Solicitors
                join lga in dbContext.Lgas.Include(x => x.State) on user.LgaId equals lga.Id
                join firm in dbContext.Firms on user.FirmId equals firm.Id
                join bankAccount in dbContext.BankAccounts on user.BankAccountId equals bankAccount.Id
                join bank in dbContext.Banks on bankAccount.BankId equals bank.Id
                select new SolicitorOnboardResponse
                {
                    Bank = bank.Name, Email = user.Email, Firm = firm.Name,
                    Lga = lga.Name, State = lga.State.Name, AccountName = bankAccount.AccountName,
                    AccountNumber = bankAccount.AccountNumber, FirstName = user.FirstName,
                    LastName = user.LastName, PhoneNumber = user.PhoneNumber, SolicitorId = user.Id,
                    Address = user.Address
                }).FirstAsync(x => x.SolicitorId == solicitorId);

            return solicitor;
        }
    }
}