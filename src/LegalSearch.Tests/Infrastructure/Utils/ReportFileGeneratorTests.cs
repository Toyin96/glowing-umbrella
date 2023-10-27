using ClosedXML.Excel;
using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Infrastructure.File.Report;

namespace LegalSearch.Test.Infrastructure.Utils
{
    public class ReportFileGeneratorTests
    {
        [Fact]
        public void WriteLegalSearchReportToStreamForStaff_ValidData_WritesToStream()
        {
            // Arrange
            var outputStream = new MemoryStream();
            var reports = GetStaffRootResponsePayload(); 
            var dataTable = ReportFileGenerator.GenerateLegalSearchDataTableForStaffReport(reports);
            var expectedRowCount = dataTable.Rows.Count + 1;

            // Act
            ReportFileGenerator.WriteLegalSearchReportToStreamForStaff(outputStream, reports);

            // Assert
            outputStream.Seek(0, SeekOrigin.Begin);
            using (var workbook = new XLWorkbook(outputStream))
            {
                var worksheet = workbook.Worksheets.Worksheet(1);
                Assert.NotNull(worksheet);

                // Check if the number of rows matches the data
                Assert.Equal(expectedRowCount, worksheet.Rows().Count());
            }
        }

        [Fact]
        public void WriteLegalSearchReportToStreamForSolicitor_ValidData_WritesToStream()
        {
            // Arrange
            var outputStream = new MemoryStream();
            var reports = GetLegalSearchRootResponsePayload(); // Initialize with valid data
            var dataTable = ReportFileGenerator.GenerateLegalSearchDataTableForSolicitor(reports);
            var expectedRowCount = dataTable.Rows.Count + 1;

            // Act
            ReportFileGenerator.WriteLegalSearchReportToStreamForSolicitor(outputStream, reports);

            // Assert
            outputStream.Seek(0, SeekOrigin.Begin);
            using (var workbook = new XLWorkbook(outputStream))
            {
                var worksheet = workbook.Worksheets.Worksheet(1);
                Assert.NotNull(worksheet);

                // Check if the number of rows matches the data
                Assert.Equal(expectedRowCount, worksheet.Rows().Count());
            }
        }

        [Fact]
        public void GenerateLegalSearchDataTableForSolicitor_ValidData_GeneratesDataTable()
        {
            // Arrange
            var reports = GetLegalSearchRootResponsePayload(); // Initialize with valid data

            // Act
            var dataTable = ReportFileGenerator.GenerateLegalSearchDataTableForSolicitor(reports);

            // Assert
            Assert.NotNull(dataTable);
        }

        [Fact]
        public void GenerateLegalSearchDataTableForStaff_ValidData_GeneratesDataTable()
        {
            // Arrange
            var reports = new StaffRootResponsePayload(); // Initialize with valid data

            // Act
            var dataTable = ReportFileGenerator.GenerateLegalSearchDataTableForStaffReport(reports);

            // Assert
            Assert.NotNull(dataTable);
        }

        private StaffRootResponsePayload GetStaffRootResponsePayload()
        {
            // Sample data for MonthlyRequestData
            var monthlyData1 = new MonthlyRequestData { Name = "January", New = 10, Comp = 8 };
            var monthlyData2 = new MonthlyRequestData { Name = "February", New = 15, Comp = 12 };
            var monthlyData3 = new MonthlyRequestData { Name = "March", New = 18, Comp = 16 };
            var monthlyDataList = new List<MonthlyRequestData> { monthlyData1, monthlyData2, monthlyData3 };

            // Sample data for LegalSearchResponsePayload
            var legalSearch1 = new LegalSearchResponsePayload
            {
                Id = Guid.NewGuid(),
                RequestInitiator = "John Doe",
                RequestType = "Type A",
                CustomerAccountName = "ABC Corp",
                RequestStatus = "Pending",
                CustomerAccountNumber = "12345",
                BusinessLocation = "Location A",
                BusinessLocationId = Guid.NewGuid(),
                RegistrationLocation = "Location X",
                RequestSubmissionDate = DateTime.Now,
                RegistrationLocationId = Guid.NewGuid(),
                RegistrationNumber = "Reg-001",
                DateCreated = DateTime.Now,
                DateDue = DateTime.Now.AddHours(24),
                Solicitor = "Solicitor 1",
                ReasonOfCancellation = "Cancellation Reason 1",
                DateOfCancellation = null,
                RegistrationDate = DateTime.Now,
                Region = "Region 1",
                RegionCode = Guid.NewGuid()
            };

            var legalSearch2 = new LegalSearchResponsePayload
            {
                Id = Guid.NewGuid(),
                RequestInitiator = "Alice Smith",
                RequestType = "Type B",
                CustomerAccountName = "XYZ Corp",
                RequestStatus = "Completed",
                CustomerAccountNumber = "54321",
                BusinessLocation = "Location B",
                BusinessLocationId = Guid.NewGuid(),
                RegistrationLocation = "Location Y",
                RequestSubmissionDate = DateTime.Now,
                RegistrationLocationId = Guid.NewGuid(),
                RegistrationNumber = "Reg-002",
                DateCreated = DateTime.Now,
                DateDue = DateTime.Now.AddHours(48),
                Solicitor = "Solicitor 2",
                ReasonOfCancellation = "Cancellation Reason 2",
                DateOfCancellation = DateTime.Now.AddHours(12),
                RegistrationDate = DateTime.Now,
                Region = "Region 2",
                RegionCode = Guid.NewGuid()
            };

            var legalSearchList = new List<LegalSearchResponsePayload> { legalSearch1, legalSearch2 };

            // Sample data for StaffRootResponsePayload
            return new StaffRootResponsePayload
            {
                LegalSearchRequests = legalSearchList,
                RequestsCountBarChart = monthlyDataList,
                PendingRequests = 5,
                CompletedRequests = 10,
                OpenRequests = 3,
                AverageProcessingTime = "2 days",
                TotalRequests = 18,
                WithinSLACount = 14,
                ElapsedSLACount = 4,
                Within3HoursToSLACount = 5,
                RequestsWithLawyersFeedbackCount = 2
            };

        }

        private LegalSearchRootResponsePayload GetLegalSearchRootResponsePayload()
        {
            // Sample data for MonthlyRequestData
            var monthlyData1 = new MonthlyRequestData { Name = "January", New = 10, Comp = 8 };
            var monthlyData2 = new MonthlyRequestData { Name = "February", New = 15, Comp = 12 };
            var monthlyData3 = new MonthlyRequestData { Name = "March", New = 18, Comp = 16 };
            var monthlyDataList = new List<MonthlyRequestData> { monthlyData1, monthlyData2, monthlyData3 };

            // Sample data for LegalSearchResponsePayload
            var legalSearch1 = new LegalSearchResponsePayload
            {
                Id = Guid.NewGuid(),
                RequestInitiator = "John Doe",
                RequestType = "Type A",
                CustomerAccountName = "ABC Corp",
                RequestStatus = "Pending",
                CustomerAccountNumber = "12345",
                BusinessLocation = "Location A",
                BusinessLocationId = Guid.NewGuid(),
                RegistrationLocation = "Location X",
                RequestSubmissionDate = DateTime.Now,
                RegistrationLocationId = Guid.NewGuid(),
                RegistrationNumber = "Reg-001",
                DateCreated = DateTime.Now,
                DateDue = DateTime.Now.AddHours(24),
                Solicitor = "Solicitor 1",
                ReasonOfCancellation = "Cancellation Reason 1",
                DateOfCancellation = null,
                RegistrationDate = DateTime.Now,
                Region = "Region 1",
                RegionCode = Guid.NewGuid()
            };

            var legalSearch2 = new LegalSearchResponsePayload
            {
                Id = Guid.NewGuid(),
                RequestInitiator = "Alice Smith",
                RequestType = "Type B",
                CustomerAccountName = "XYZ Corp",
                RequestStatus = "Completed",
                CustomerAccountNumber = "54321",
                BusinessLocation = "Location B",
                BusinessLocationId = Guid.NewGuid(),
                RegistrationLocation = "Location Y",
                RequestSubmissionDate = DateTime.Now,
                RegistrationLocationId = Guid.NewGuid(),
                RegistrationNumber = "Reg-002",
                DateCreated = DateTime.Now,
                DateDue = DateTime.Now.AddHours(48),
                Solicitor = "Solicitor 2",
                ReasonOfCancellation = "Cancellation Reason 2",
                DateOfCancellation = DateTime.Now.AddHours(12),
                RegistrationDate = DateTime.Now,
                Region = "Region 2",
                RegionCode = Guid.NewGuid()
            };

            var legalSearchList = new List<LegalSearchResponsePayload> { legalSearch1, legalSearch2 };

            // Sample data for LegalSearchRootResponsePayload
            return new LegalSearchRootResponsePayload
            {
                LegalSearchRequests = legalSearchList,
                RequestsByMonth = monthlyDataList,
                TotalRequestsCount = legalSearchList.Count,
                AssignedRequestsCount = 5,
                CompletedRequestsCount = 8,
                NewRequestsCount = 3,
                ReturnedRequestsCount = 2,
                RejectedRequestsCount = 1,
                WithinSLACount = 6,
                ElapsedSLACount = 2,
                Within3HoursToDueCount = 4
            };
        }
    }
}
