﻿namespace LegalSearch.Application.Models.Responses
{
    public sealed record StateResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
