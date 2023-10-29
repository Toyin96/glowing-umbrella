using ClosedXML.Excel;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.CSO;
using System.Data;

namespace LegalSearch.Infrastructure.File.Report
{
    public static class ReportFileGenerator
    {
        public static void WriteLegalSearchReportToStreamForStaff(Stream outputStream, StaffRootResponsePayload reports)
        {
            using var workbook = new XLWorkbook();
            using var dataTable = GenerateLegalSearchDataTableForStaffReport(reports);
            var workSheet = workbook.AddWorksheet(dataTable, ReportConstants.LegalSearchReport);

            workSheet.SheetView.FreezeRows(1);
            workSheet.Columns().AdjustToContents();

            workbook.SaveAs(outputStream);
        }

        public static void WriteLegalSearchReportToStreamForSolicitor(Stream outputStream, LegalSearchRootResponsePayload reports)
        {
            using var workbook = new XLWorkbook();
            using var dataTable = GenerateLegalSearchDataTableForSolicitor(reports);
            var workSheet = workbook.AddWorksheet(dataTable, ReportConstants.LegalSearchReport);

            workSheet.SheetView.FreezeRows(1);
            workSheet.Columns().AdjustToContents();

            workbook.SaveAs(outputStream);
        }

        public static DataTable GenerateLegalSearchDataTableForSolicitor(LegalSearchRootResponsePayload reports)
        {
            var dataTable = new DataTable(ReportConstants.LegalSearchReport);
            ProcessLegalSearchRootResponsePayloadColumns(dataTable);

            var serial = 1;

            foreach (var row in reports.LegalSearchRequests)
            {
                dataTable.Rows.Add(
                    serial++,
                    row.RequestInitiator,
                    row.RequestType,
                    row.CustomerAccountName,
                    row.RequestStatus,
                    row.CustomerAccountNumber,
                    row.BusinessLocation,
                    row.BusinessLocationId,
                    row.RegistrationLocation,
                    row.RequestSubmissionDate.HasValue ? row.RequestSubmissionDate.Value.ToString("MMM dd, yyyy HH:mm:ss") : string.Empty,
                    row.RegistrationLocationId,
                    row.RegistrationNumber,
                    row.DateCreated.ToString("MMM dd, yyyy HH:mm:ss"),
                    row.DateDue.HasValue ? row.DateDue.Value.ToString("MMM dd, yyyy HH:mm:ss") : string.Empty,
                    row.Solicitor,
                    row.ReasonOfCancellation,
                    row.DateOfCancellation.HasValue ? row.DateOfCancellation.Value.ToString("MMM dd, yyyy HH:mm:ss") : string.Empty,
                    row.RegistrationDate.ToString("MMM dd, yyyy HH:mm:ss"),
                    row.Region,
                    row.RegionCode
                );
            }

            dataTable.Rows.Add(
                serial++,
                "", // Placeholder for RequestInitiator (string)
                "", // Placeholder for RequestType (string)
                "", // Placeholder for CustomerAccountName (string)
                "", // Placeholder for RequestStatus (string)
                "", // Placeholder for CustomerAccountNumber (string)
                "", // Placeholder for BusinessLocation (string)
                Guid.Empty, // Placeholder for BusinessLocationId (Guid)
                "", // Placeholder for RegistrationLocation (string)
                "", // Placeholder for RequestSubmissionDate (string)
                Guid.Empty, // Placeholder for RegistrationLocationId (Guid)
                "", // Placeholder for RegistrationNumber (string)
                "", // Placeholder for DateCreated (DateTime)
                "", // Placeholder for DateDue (string)
                "", // Placeholder for Solicitor (string)
                "", // Placeholder for ReasonOfCancellation (string)
                "", // Placeholder for DateOfCancellation (DateTime)
                "", // Placeholder for RegistrationDate (string)
                "", // Placeholder for Region
                Guid.Empty, // Placeholder for Region code
                reports.TotalRequestsCount, // TotalRequestsCount (int)
                reports.AssignedRequestsCount, // AssignedRequestsCount (int)
                reports.NewRequestsCount, // NewRequestsCount (int)
                reports.ReturnedRequestsCount, // ReturnedRequestsCount (string)
                reports.CompletedRequestsCount, // CompletedRequestsCount (string)
                reports.RejectedRequestsCount, // RejectedRequestsCount (string)
                reports.WithinSLACount, // WithinSLACount (int)
                reports.ElapsedSLACount, // ElapsedSLACount (int)
                reports.Within3HoursToDueCount // Within3HoursToSLACount (int)
            );

            return dataTable;
        }

        private static void ProcessLegalSearchRootResponsePayloadColumns(DataTable dataTable)
        {
            dataTable.Columns.Add("S/N", typeof(long));
            dataTable.Columns.Add("RequestInitiator", typeof(string));
            dataTable.Columns.Add("RequestType", typeof(string));
            dataTable.Columns.Add("CustomerAccountName", typeof(string));
            dataTable.Columns.Add("RequestStatus", typeof(string));
            dataTable.Columns.Add("CustomerAccountNumber", typeof(string));
            dataTable.Columns.Add("BusinessLocation", typeof(string));
            dataTable.Columns.Add("BusinessLocationId", typeof(Guid));
            dataTable.Columns.Add("RegistrationLocation", typeof(string));
            dataTable.Columns.Add("RequestSubmissionDate", typeof(string));
            dataTable.Columns.Add("RegistrationLocationId", typeof(string));
            dataTable.Columns.Add("RegistrationNumber", typeof(string));
            dataTable.Columns.Add("DateCreated", typeof(string));
            dataTable.Columns.Add("DateDue", typeof(string));
            dataTable.Columns.Add("Solicitor", typeof(string));
            dataTable.Columns.Add("ReasonOfCancellation", typeof(string));
            dataTable.Columns.Add("DateOfCancellation", typeof(string));
            dataTable.Columns.Add("RegistrationDate", typeof(string));
            dataTable.Columns.Add("Region", typeof(string));
            dataTable.Columns.Add("RegionCode", typeof(Guid));
            dataTable.Columns.Add("TotalRequestsCount", typeof(int));
            dataTable.Columns.Add("AssignedRequestsCount", typeof(int));
            dataTable.Columns.Add("NewRequestsCount", typeof(int));
            dataTable.Columns.Add("ReturnedRequestsCount", typeof(int));
            dataTable.Columns.Add("CompletedRequestsCount", typeof(int));
            dataTable.Columns.Add("RejectedRequestsCount", typeof(int));
            dataTable.Columns.Add("WithinSLACount", typeof(int));
            dataTable.Columns.Add("ElapsedSLACount", typeof(int));
            dataTable.Columns.Add("Within3HoursToDueCount", typeof(int));
        }

        public static DataTable GenerateLegalSearchDataTableForStaffReport(StaffRootResponsePayload reports)
        {
            var dataTable = new DataTable(ReportConstants.LegalSearchReport);
            ProcessStaffRootResponsePayloadColumns(dataTable);

            var serial = 1;

            if (reports.LegalSearchRequests != null)
            {
                foreach (var row in reports.LegalSearchRequests)
                {
                    dataTable.Rows.Add(
                        serial++,
                        row.RequestInitiator,
                        row.RequestType,
                        row.CustomerAccountName,
                        row.RequestStatus,
                        row.CustomerAccountNumber,
                        row.BusinessLocation,
                        row.BusinessLocationId,
                        row.RegistrationLocation,
                        row.RequestSubmissionDate.HasValue ? row.RequestSubmissionDate.Value.ToString("MMM dd, yyyy HH:mm:ss") : string.Empty,
                        row.RegistrationLocationId,
                        row.RegistrationNumber,
                        row.DateCreated.ToString("MMM dd, yyyy HH:mm:ss"),
                        row.DateDue.HasValue ? row.DateDue.Value.ToString("MMM dd, yyyy HH:mm:ss") : string.Empty,
                        row.Solicitor,
                        row.ReasonOfCancellation,
                        row.DateOfCancellation.HasValue ? row.DateOfCancellation.Value.ToString("MMM dd, yyyy HH:mm:ss") : string.Empty,
                        row.RegistrationDate.ToString("MMM dd, yyyy HH:mm:ss"),
                        row.Region,
                        row.RegionCode
                    );
                }

                dataTable.Rows.Add(
                    serial++,
                    "", // Placeholder for RequestInitiator (string)
                    "", // Placeholder for RequestType (string)
                    "", // Placeholder for CustomerAccountName (string)
                    "", // Placeholder for RequestStatus (string)
                    "", // Placeholder for CustomerAccountNumber (string)
                    "", // Placeholder for BusinessLocation (string)
                    Guid.Empty, // Placeholder for BusinessLocationId (Guid)
                    "", // Placeholder for RegistrationLocation (string)
                    "", // Placeholder for RequestSubmissionDate (string)
                    Guid.Empty, // Placeholder for RegistrationLocationId (Guid)
                    "", // Placeholder for RegistrationNumber (string)
                    "", // Placeholder for DateCreated (DateTime)
                    "", // Placeholder for DateDue (DateTime)
                    "", // Placeholder for Solicitor (string)
                    "", // Placeholder for ReasonOfCancellation (string)
                    "", // Placeholder for DateOfCancellation (DateTime)
                    "", // Placeholder for RegistrationDate (DateTime)
                    "", // Placeholder for Region
                    Guid.Empty, // Placeholder for Region code
                    reports.PendingRequests, // PendingRequests (int)
                    reports.CompletedRequests, // CompletedRequests (int)
                    reports.OpenRequests, // OpenRequests (int)
                    reports.AverageProcessingTime, // AverageProcessingTime (string)
                    reports.TotalRequests, // TotalRequests (int)
                    reports.WithinSLACount, // WithinSLACount (int)
                    reports.ElapsedSLACount, // ElapsedSLACount (int)
                    reports.Within3HoursToSLACount, // Within3HoursToSLACount (int)
                    reports.RequestsWithLawyersFeedbackCount // RequestsWithLawyersFeedbackCount (int)
                    );
            }

            return dataTable;
        }

        private static void ProcessStaffRootResponsePayloadColumns(DataTable dataTable)
        {
            dataTable.Columns.Add("S/N", typeof(long));
            dataTable.Columns.Add("RequestInitiator", typeof(string));
            dataTable.Columns.Add("RequestType", typeof(string));
            dataTable.Columns.Add("CustomerAccountName", typeof(string));
            dataTable.Columns.Add("RequestStatus", typeof(string));
            dataTable.Columns.Add("CustomerAccountNumber", typeof(string));
            dataTable.Columns.Add("BusinessLocation", typeof(string));
            dataTable.Columns.Add("BusinessLocationId", typeof(Guid));
            dataTable.Columns.Add("RegistrationLocation", typeof(string));
            dataTable.Columns.Add("RequestSubmissionDate", typeof(string)); // Change to string for custom formatting
            dataTable.Columns.Add("RegistrationLocationId", typeof(string)); // Change to string for consistency
            dataTable.Columns.Add("RegistrationNumber", typeof(string));
            dataTable.Columns.Add("DateCreated", typeof(string)); // Change to string for custom formatting
            dataTable.Columns.Add("DateDue", typeof(string)); // Change to string for custom formatting
            dataTable.Columns.Add("Solicitor", typeof(string));
            dataTable.Columns.Add("ReasonOfCancellation", typeof(string));
            dataTable.Columns.Add("DateOfCancellation", typeof(string)); // Change to string for custom formatting
            dataTable.Columns.Add("RegistrationDate", typeof(string)); // Change to string for custom formatting
            dataTable.Columns.Add("Region", typeof(string));
            dataTable.Columns.Add("RegionCode", typeof(Guid));
            dataTable.Columns.Add("PendingRequests", typeof(int));
            dataTable.Columns.Add("CompletedRequests", typeof(int));
            dataTable.Columns.Add("OpenRequests", typeof(int));
            dataTable.Columns.Add("AverageProcessingTime", typeof(string));
            dataTable.Columns.Add("TotalRequests", typeof(int));
            dataTable.Columns.Add("WithinSLACount", typeof(int));
            dataTable.Columns.Add("ElapsedSLACount", typeof(int));
            dataTable.Columns.Add("Within3HoursToSLACount", typeof(int));
            dataTable.Columns.Add("RequestsWithLawyersFeedbackCount", typeof(int));
        }
    }
}
