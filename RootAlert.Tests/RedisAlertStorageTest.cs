using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using RootAlert.Config;
using RootAlert.Redis;
using RootAlert.Storage;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace RootAlert.Tests
{
    class TestState
    {
        public override string ToString() => "Test State Message";
    }

    public class RedisAlertStorageTests
    {
        private readonly Mock<IConnectionMultiplexer> _mockRedis;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<ILogger<RedisAlertStorage>> _mockLogger;
        private readonly TestableRedisAlertStorage _storage;

        public RedisAlertStorageTests()
        {
            _mockRedis = new Mock<IConnectionMultiplexer>();
            _mockDatabase = new Mock<IDatabase>();
            _mockLogger = new Mock<ILogger<RedisAlertStorage>>();

            _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);

            _storage = new TestableRedisAlertStorage("localhost:6379", _mockRedis.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AddToBatchAsync_ShouldAddExceptionToRedis()
        {
            // Arrange
            var exception = new Exception("Test Exception");
            var requestInfo = new RequestInfo(
                Url: "/test?test=true",
                Method: "GET",
                Headers: "{\"User-Agent\":\"Test\"}"
            );

            _mockDatabase.Setup(db => db.ListRightPushAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()
            )).ReturnsAsync(1);

            // Act
            await _storage.AddToBatchAsync(exception, requestInfo);

            // Assert
            _mockDatabase.Setup(db => db.ListRightPushAsync(
                                    It.IsAny<RedisKey>(),
                                    It.IsAny<RedisValue>(),
                                    It.IsAny<When>(),
                                    It.IsAny<CommandFlags>()
                                )).ThrowsAsync(new Exception("Redis Error"));
        }

        // [Fact]
        // public async Task AddToBatchAsync_ShouldLogError_WhenExceptionOccurs()
        // {
        //     // Arrange
        //     var exception = new Exception("Test Exception");
        //     var requestInfo = new RequestInfo(
        //         Url: "/test",
        //         Method: "GET",
        //         Headers: "{}"
        //     );

        //     string loggedMessage = null;
        //     _mockLogger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);
        //     _mockLogger.Setup(x => x.Log<IReadOnlyList<KeyValuePair<string, object>>>(
        //         LogLevel.Error,
        //         It.IsAny<EventId>(),
        //         It.IsAny<IReadOnlyList<KeyValuePair<string, object>>>(),
        //         It.IsAny<Exception>(),
        //         It.IsAny<Func<IReadOnlyList<KeyValuePair<string, object>>, Exception, string>>()))
        //         .Callback<LogLevel, EventId, IReadOnlyList<KeyValuePair<string, object>>, Exception, Func<IReadOnlyList<KeyValuePair<string, object>>, Exception, string>>(
        //             (level, id, state, ex, formatter) => loggedMessage = formatter(state, ex));

        //     _mockDatabase.Setup(db => db.ListRightPushAsync(
        //         It.IsAny<RedisKey>(),
        //         It.IsAny<RedisValue>(),
        //         It.IsAny<When>(),
        //         It.IsAny<CommandFlags>()
        //     )).ThrowsAsync(new Exception("Redis Error"));

        //     // Act
        //     await _storage.AddToBatchAsync(exception, requestInfo);

        //     // Assert
        //     _mockLogger.Verify(x => x.Log<IReadOnlyList<KeyValuePair<string, object>>>(
        //         LogLevel.Error,
        //         It.IsAny<EventId>(),
        //         It.IsAny<IReadOnlyList<KeyValuePair<string, object>>>(),
        //         It.IsAny<Exception>(),
        //         It.IsAny<Func<IReadOnlyList<KeyValuePair<string, object>>, Exception, string>>()),
        //         Times.Once());

        //     Assert.NotNull(loggedMessage);
        //     Assert.Contains("RootAlert : Failed to add data", loggedMessage);
        // }


        [Fact]
        public async Task GetBatchAsync_ShouldReturnConsolidatedErrors()
        {
            // Arrange
            var exception1 = new ExceptionInfo("Error 1", "Stack 1", "System.Exception");
            var exception2 = new ExceptionInfo("Error 1", "Stack 1", "System.Exception"); // Duplicate
            var exception3 = new ExceptionInfo("Error 2", "Stack 2", "System.Exception");

            var requestInfo1 = new RequestInfo(Url: "/test1", Method: "GET", Headers: "{}");
            var requestInfo2 = new RequestInfo(Url: "/test2", Method: "POST", Headers: "{}");
            var requestInfo3 = new RequestInfo(Url: "/test3", Method: "PUT", Headers: "{}");

            var entry1 = new ErrorLogEntry { Exception = exception1, Request = requestInfo1, Count = 1 };
            var entry2 = new ErrorLogEntry { Exception = exception2, Request = requestInfo2, Count = 1 };
            var entry3 = new ErrorLogEntry { Exception = exception3, Request = requestInfo3, Count = 1 };

            var json1 = JsonSerializer.Serialize(entry1);
            var json2 = JsonSerializer.Serialize(entry2);
            var json3 = JsonSerializer.Serialize(entry3);

            _mockDatabase.Setup(db => db.ListLengthAsync(It.IsAny<RedisKey>(), CommandFlags.None))
                .ReturnsAsync(3);

            _mockDatabase.Setup(db => db.ListRangeAsync(It.IsAny<RedisKey>(), 0, 2, CommandFlags.None))
                .ReturnsAsync(new[] { (RedisValue)json1, (RedisValue)json2, (RedisValue)json3 });

            _mockDatabase.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None))
                .ReturnsAsync(true);

            // Act
            var result = await _storage.GetBatchAsync();

            // Assert
            Assert.Equal(2, result.Count); // Should consolidate duplicates

            var firstError = result.FirstOrDefault(e => e.Exception?.Message == "Error 1");
            Assert.NotNull(firstError);
            Assert.Equal(2, firstError.Count); // Count should be incremented for duplicates

            var secondError = result.FirstOrDefault(e => e.Exception?.Message == "Error 2");
            Assert.NotNull(secondError);
            Assert.Equal(1, secondError.Count);

            _mockDatabase.Verify(db => db.KeyDeleteAsync("RootAlert:ErrorBatch", CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task GetBatchAsync_ShouldReturnEmptyList_WhenExceptionOccurs()
        {
            // Arrange
            _mockDatabase.Setup(db => db.ListLengthAsync(It.IsAny<RedisKey>(), CommandFlags.None))
                .ThrowsAsync(new Exception("Redis Error"));

            // Act
            var result = await _storage.GetBatchAsync();

            // Assert
            Assert.Empty(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("RootAlert : Failed to retrive data")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ClearBatchAsync_ShouldDeleteKey()
        {
            // Arrange
            _mockDatabase.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None))
                .ReturnsAsync(true);

            // Act
            await _storage.ClearBatchAsync();

            // Assert
            _mockDatabase.Verify(db => db.KeyDeleteAsync("RootAlert:ErrorBatch", CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task ClearBatchAsync_ShouldLogError_WhenExceptionOccurs()
        {
            // Arrange
            _mockDatabase.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None))
                .ThrowsAsync(new Exception("Redis Error"));

            // Act
            await _storage.ClearBatchAsync();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("RootAlert : Failed to delete data")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        private bool VerifyErrorLogJson(RedisValue value, Exception exception, RequestInfo requestInfo)
        {
            try
            {
                var errorEntry = JsonSerializer.Deserialize<ErrorLogEntry>(value.ToString());
                return errorEntry != null
                    && errorEntry.Exception?.Message == exception.Message
                    && errorEntry.Exception?.Name == exception.GetType().Name
                    && errorEntry.Request!.Url == requestInfo.Url
                    && errorEntry.Request.Method == requestInfo.Method
                    && errorEntry.Request.Headers == requestInfo.Headers;
            }
            catch
            {
                return false;
            }
        }

        // Test double class to override the Redis connection
        private class TestableRedisAlertStorage : RedisAlertStorage
        {
            private readonly IConnectionMultiplexer _testRedis;

            public TestableRedisAlertStorage(
                string redisConnectionString,
                IConnectionMultiplexer mockRedis,
                ILogger<RedisAlertStorage> logger)
                : base(redisConnectionString, logger)
            {
                _testRedis = mockRedis;
            }

            protected override IDatabase GetDatabase()
            {
                return _testRedis.GetDatabase();
            }
        }
    }
}