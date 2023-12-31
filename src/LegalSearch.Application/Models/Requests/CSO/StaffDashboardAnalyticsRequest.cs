﻿using Fcmb.Shared.Models.Requests;
using LegalSearch.Domain.Enums.LegalRequest;
using System.Text.Json.Serialization;

namespace LegalSearch.Application.Models.Requests.CSO
{
    public record StaffDashboardAnalyticsRequest : PaginatedDateRangeRequest
    {
        public CsoRequestStatusType? CsoRequestStatusType { get; set; }
        public SearchType? SearchType { get; set; }
        [JsonIgnore]
        public string? BranchId { get; set; }
        public string? RegionId { get; set; }
    }
}
