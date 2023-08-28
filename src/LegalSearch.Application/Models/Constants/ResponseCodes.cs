using Fcmb.Shared.Models.Constants;

namespace LegalSearch.Application.Models.Constants
{
    public class ResponseCodes : BaseResponseCodes
    {
        #region User Codes
        public const string InactiveUser = "800";
        public const string InvalidToken = "801";
        public const string LockedOutUser = "802";
        #endregion

        #region User Creation

        #endregion

        #region GeneralApplication
        public const string BadRequest = "700"; // Use this status code when the request cannot be processed due to malformed syntax, validation errors, or other client errors that are the client's responsibility
        public const string Forbidden = "701"; // Use this status code when the client's request is authenticated but lacks the necessary permissions to access the requested resource.
        public const string NotFound = "702"; // Use this status code when the requested resource is not found on the server.
        public const string MethodNotAllowed = "703"; // Use this status code when the HTTP method used in the request is not supported for the requested resource.
        public const string Conflict = "704"; // Use this status code when the request cannot be completed due to a conflict with the current state of the target resource.
        #endregion
    }
}
