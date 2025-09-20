using CleanArchitecture.WebAPI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchitecture.WebAPI.Tests.Middleware;

/// <summary>
/// Unit tests for CorrelationIdMiddleware
/// </summary>
public class CorrelationIdMiddlewareTests
{
    private readonly Mock<ILogger<CorrelationIdMiddleware>> _mockLogger;
    private readonly DefaultHttpContext _context;

    public CorrelationIdMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<CorrelationIdMiddleware>>();
        _context = new DefaultHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_Should_Generate_New_CorrelationId_When_None_Provided()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new CorrelationIdMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.True(_context.Items.ContainsKey("CorrelationId"));
        Assert.NotNull(_context.Items["CorrelationId"]);
        Assert.True(Guid.TryParse(_context.Items["CorrelationId"].ToString(), out _));
        Assert.True(_context.Response.Headers.ContainsKey("X-Correlation-ID"));
    }

    [Fact]
    public async Task InvokeAsync_Should_Use_CorrelationId_From_Request_Header()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-id-123";
        _context.Request.Headers.Add("X-Correlation-ID", expectedCorrelationId);

        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new CorrelationIdMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(expectedCorrelationId, _context.Items["CorrelationId"]);
        Assert.Equal(expectedCorrelationId, _context.Response.Headers["X-Correlation-ID"]);
    }

    [Fact]
    public async Task InvokeAsync_Should_Use_CorrelationId_From_Query_Parameter()
    {
        // Arrange
        var expectedCorrelationId = "query-correlation-id-456";
        _context.Request.QueryString = new QueryString($"?correlationId={expectedCorrelationId}");

        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new CorrelationIdMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(expectedCorrelationId, _context.Items["CorrelationId"]);
        Assert.Equal(expectedCorrelationId, _context.Response.Headers["X-Correlation-ID"]);
    }

    [Fact]
    public async Task InvokeAsync_Should_Prefer_Header_Over_Query_Parameter()
    {
        // Arrange
        var headerCorrelationId = "header-correlation-id";
        var queryCorrelationId = "query-correlation-id";
        
        _context.Request.Headers.Add("X-Correlation-ID", headerCorrelationId);
        _context.Request.QueryString = new QueryString($"?correlationId={queryCorrelationId}");

        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new CorrelationIdMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(headerCorrelationId, _context.Items["CorrelationId"]);
        Assert.Equal(headerCorrelationId, _context.Response.Headers["X-Correlation-ID"]);
    }

    [Fact]
    public async Task InvokeAsync_Should_Generate_New_Id_When_Header_Is_Empty()
    {
        // Arrange
        _context.Request.Headers.Add("X-Correlation-ID", "");

        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new CorrelationIdMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.True(_context.Items.ContainsKey("CorrelationId"));
        Assert.NotNull(_context.Items["CorrelationId"]);
        Assert.NotEmpty(_context.Items["CorrelationId"].ToString());
        Assert.True(Guid.TryParse(_context.Items["CorrelationId"].ToString(), out _));
    }

    [Fact]
    public async Task InvokeAsync_Should_Add_CorrelationId_To_Response_Headers()
    {
        // Arrange
        var expectedCorrelationId = "response-header-test";
        _context.Request.Headers.Add("X-Correlation-ID", expectedCorrelationId);

        RequestDelegate next = (HttpContext hc) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(_context.Response.Headers.ContainsKey("X-Correlation-ID"));
        Assert.Equal(expectedCorrelationId, _context.Response.Headers["X-Correlation-ID"]);
    }
}

/// <summary>
/// Unit tests for CorrelationIdService
/// </summary>
public class CorrelationIdServiceTests
{
    [Fact]
    public void GetCorrelationId_Should_Return_CorrelationId_From_HttpContext()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-id";
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = expectedCorrelationId;

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        var service = new CorrelationIdService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCorrelationId();

        // Assert
        Assert.Equal(expectedCorrelationId, result);
    }

    [Fact]
    public void GetCorrelationId_Should_Return_Unknown_When_HttpContext_Is_Null()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null);

        var service = new CorrelationIdService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCorrelationId();

        // Assert
        Assert.Equal("unknown", result);
    }

    [Fact]
    public void GetCorrelationId_Should_Return_Unknown_When_CorrelationId_Not_In_Items()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        var service = new CorrelationIdService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCorrelationId();

        // Assert
        Assert.Equal("unknown", result);
    }
}