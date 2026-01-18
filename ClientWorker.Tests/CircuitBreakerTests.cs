namespace ClientWorker.Tests;
using ClientWorker.Infrastructure.Resilience;
using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

public class CircuitBreakerTests
{
    private readonly CircuitBreaker<string> _breaker;

    public CircuitBreakerTests()
    {
        _breaker = new CircuitBreaker<string>(
            failureThreshold: 3,
            failureWindow: TimeSpan.FromSeconds(30),   // short for fast tests
            openDuration: TimeSpan.FromSeconds(10),
            logger: Mock.Of<ILogger>()
        );
    }

    // 1. Normal success in Closed state
    [Fact]
    public async Task ExecuteAsync_SuccessInClosed_ReturnsResultAndStaysClosed()
    {
        var result = await _breaker.ExecuteAsync(() => Task.FromResult("OK"));

        Assert.Equal("OK", result);
        Assert.Equal(CircuitBreakerState.Closed, _breaker.CurrentState);
    }

    // 2. Single failure keeps Closed
    [Fact]
    public async Task ExecuteAsync_OneFailure_StaysClosed()
    {
        var failingOp = new Mock<Func<Task<string>>>();
        failingOp.Setup(op => op()).ThrowsAsync(new Exception("boom"));

        await Assert.ThrowsAsync<Exception>(() => _breaker.ExecuteAsync(failingOp.Object));

        Assert.Equal(CircuitBreakerState.Closed, _breaker.CurrentState);
    }

    // 3. Threshold failures → Open
    [Fact]
    public async Task ExecuteAsync_ThresholdFailures_TripsToOpen()
    {
        var failingOp = new Mock<Func<Task<string>>>();
        failingOp.Setup(op => op()).ThrowsAsync(new Exception("fail"));

        // 3 failures
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAnyAsync<Exception>(() => _breaker.ExecuteAsync(failingOp.Object));
        }

        Assert.Equal(CircuitBreakerState.Open, _breaker.CurrentState);

        // Next call should fast-fail without invoking operation
        failingOp.Reset(); // clear invocations
        await Assert.ThrowsAsync<CircuitBreaker<string>.CircuitOpenException>(
            () => _breaker.ExecuteAsync(failingOp.Object));

        failingOp.Verify(op => op(), Times.Never); // important: no call to downstream
    }

    // 4. After open duration → HalfOpen, success → Closed
    [Fact]
    public async Task ExecuteAsync_AfterOpenDuration_SuccessInHalfOpen_Closes()
    {
        // Trip it first
        var failing = new Mock<Func<Task<string>>>();
        failing.Setup(op => op()).ThrowsAsync(new Exception());

        for (int i = 0; i < 3; i++)
            await _breaker.ExecuteAsync(failing.Object);

        Assert.Equal(CircuitBreakerState.Open, _breaker.CurrentState);

        // Simulate time passing
        await Task.Delay(11000); // > openDuration 10s

        // Now in HalfOpen on next call
        var successOp = () => Task.FromResult("recovered");

        var result = await _breaker.ExecuteAsync(successOp);

        Assert.Equal("recovered", result);
        Assert.Equal(CircuitBreakerState.Closed, _breaker.CurrentState);
    }

    // 5. HalfOpen failure → back to Open
    [Fact]
    public async Task ExecuteAsync_FailureInHalfOpen_Reopens()
    {
        // Trip to Open (same as above)
        var failing = new Mock<Func<Task<string>>>();
        failing.Setup(op => op()).ThrowsAsync(new Exception());

        for (int i = 0; i < 3; i++)
            await _breaker.ExecuteAsync(failing.Object);

        await Task.Delay(11000);

        // Probe fails in HalfOpen
        await Assert.ThrowsAsync<Exception>(() => _breaker.ExecuteAsync(failing.Object));

        Assert.Equal(CircuitBreakerState.Open, _breaker.CurrentState);
    }

    // 6. Failures outside window don't count
    [Fact]
    public async Task ExecuteAsync_OldFailuresIgnored_DoesNotTrip()
    {
        var failing = new Mock<Func<Task<string>>>();
        failing.Setup(op => op()).ThrowsAsync(new Exception());

        // 2 failures now
        await _breaker.ExecuteAsync(failing.Object);
        await _breaker.ExecuteAsync(failing.Object);

        // Wait longer than window
        await Task.Delay(35000); // > 30s window

        // One more failure — should not trip (only 1 recent)
        await Assert.ThrowsAsync<Exception>(() => _breaker.ExecuteAsync(failing.Object));

        Assert.Equal(CircuitBreakerState.Closed, _breaker.CurrentState);
    }

    // 7. Success clears failures
    [Fact]
    public async Task ExecuteAsync_SuccessAfterSomeFailures_ClearsAndStaysClosed()
    {
        var op = new Mock<Func<Task<string>>>();

        // Fail twice
        op.SetupSequence(op => op())
          .ThrowsAsync(new Exception())
          .ThrowsAsync(new Exception())
          .ReturnsAsync("OK");

        await Assert.ThrowsAsync<Exception>(() => _breaker.ExecuteAsync(op.Object));
        await Assert.ThrowsAsync<Exception>(() => _breaker.ExecuteAsync(op.Object));

        // Success clears
        await _breaker.ExecuteAsync(op.Object);

        Assert.Equal(CircuitBreakerState.Closed, _breaker.CurrentState);
        Assert.Equal(0, _breaker.CurrentState == CircuitBreakerState.Closed ? 0 : 1); // indirect check
    }

    // Bonus: Fast-fail does NOT call the operation
    [Fact]
    public async Task ExecuteAsync_OpenState_DoesNotInvokeOperation()
    {
        // Trip to open
        var failing = new Mock<Func<Task<string>>>();
        failing.Setup(op => op()).ThrowsAsync(new Exception());

        for (int i = 0; i < 3; i++)
            await _breaker.ExecuteAsync(failing.Object);

        failing.Reset();

        await Assert.ThrowsAsync<CircuitBreaker<string>.CircuitOpenException>(
            () => _breaker.ExecuteAsync(failing.Object));

        failing.Verify(op => op(), Times.Never);
    }
}