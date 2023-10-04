﻿using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Application.Validations.Solicitor
{
    public class RejectRequestValidator : AbstractValidator<RejectRequest>
    {
        public RejectRequestValidator()
        {
            RuleFor(request => request.RequestId)
                .NotEmpty().WithMessage("Request ID is required.");

            RuleFor(request => request.RejectionMessage)
                .MaximumLength(500).WithMessage("Rejection message exceeds maximum length of 500 characters.");
        }
    }
}
