using LegalSearch.Infrastructure.Utilities;

namespace LegalSearch.Test.Infrastructure.Utils
{
    public class TimeUtilsTests
    {
        [Fact]
        public void GetCurrentLocalTime_ShouldReturnCurrentTimePlusOneHour()
        {
            // Arrange
            DateTime utcNow = DateTime.UtcNow;
            DateTime expectedLocalTime = utcNow.AddHours(1);

            // Act
            DateTime actualLocalTime = TimeUtils.GetCurrentLocalTime();

            // Assert
            Assert.Equal(expectedLocalTime, actualLocalTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CalculateDateDueForRequest_ShouldReturnCurrentTimePlusThreeDays()
        {
            // Arrange
            DateTime utcNow = DateTime.UtcNow;
            DateTime expectedDateDue = utcNow.AddDays(3);

            // Act
            DateTime actualDateDue = TimeUtils.CalculateDateDueForRequest();

            // Assert
            Assert.Equal(expectedDateDue.Date, actualDateDue.Date);
        }

        [Fact]
        public void GetTwentyFourHoursElapsedTime_ShouldReturnTwentyFourHoursAgo()
        {
            // Arrange
            DateTime utcNow = DateTime.UtcNow;
            DateTime expectedElapsedTime = utcNow.AddHours(-24);

            // Act
            DateTime actualElapsedTime = TimeUtils.GetTwentyFourHoursElapsedTime();

            // Assert
            Assert.Equal(expectedElapsedTime.Hour, actualElapsedTime.Hour);
        }

        [Fact]
        public void GetSeventyTwoHoursElapsedTime_ShouldReturnSeventyTwoHoursAgo()
        {
            // Arrange
            DateTime utcNow = DateTime.UtcNow;
            DateTime expectedElapsedTime = utcNow.AddHours(-72);

            // Act
            DateTime actualElapsedTime = TimeUtils.GetSeventyTwoHoursElapsedTime();

            // Assert
            Assert.Equal(expectedElapsedTime.Hour, actualElapsedTime.Hour);
        }
    }
}
