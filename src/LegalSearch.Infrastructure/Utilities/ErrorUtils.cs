﻿using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Infrastructure.Utilities
{
    public static class ErrorUtils
    {
        public static string? GetStandardizedError(this IdentityResult identityResult)
        {
            var errors = new StringBuilder();

            // Check if the IdentityResult is null, return null in that case
            if (identityResult == null) return null;

            // Check if there are any errors in the IdentityResult
            if (identityResult.Errors.Any())
            {
                // Loop through each error, replace "\n" with actual newline characters, and append to 'errors'
                identityResult.Errors.ToList().ForEach(error =>
                {
                    errors.Append(error.Description);
                    errors.AppendLine(); // Add a newline after each error description
                });
            }

            // Return the concatenated error messages as a single string
            return errors.ToString();
        }
    }
}
