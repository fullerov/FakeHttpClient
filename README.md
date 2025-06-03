# FakeHttpClient

This document explains how to use the `FakeHttpClient` utility to simulate HTTP responses in your unit tests, and how to inject the fake `HttpClient` into your wrapper for .NET Framework 4.8 (also compatible with .NET 8).

---

## Table of Contents

* [Overview](#overview)
* [Helper: FakeHttpClient](#helper-fakehttpclient)
* [Usage in Tests](#usage-in-tests)
* [Injecting into Your Wrapper](#injecting-into-your-wrapper)
* [Example Test](#example-test)
* [Tips & Best Practices](#tips--best-practices)

---

## Overview

When writing unit tests for `LogicSomethingWrapper`, you should avoid real HTTP calls and control the responses your methods receive. The `FakeHttpClient` class in your test project allows you to:

1. **Intercept** outgoing `HttpRequestMessage` instances.
2. **Return** custom `HttpResponseMessage` objects.
3. **Create** an `HttpClient` that uses the fake handler instead of the network stack.

---

## Helper: FakeHttpClient

Add class FakeHttpClient to your **Tests** project. This handler delegates `SendAsync` to your custom `responder` function.

---

## Usage in Tests

Here are three common test scenarios using `FakeHttpClient.GetClient`.

### 1. Simulate a 422 Unprocessable Entity

```csharp
var body = "{ \"message\": \"Invalid product key\" }";
HttpClient client = FakeHttpClient.GetClient((req, ct) =>
    Task.FromResult(new HttpResponseMessage((HttpStatusCode)422)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    })
);
```

### 2. Simulate a Successful 200 OK

```csharp
var responseModel = new ResponseSometing { /* ... */ };
var json = JsonConvert.SerializeObject(responseModel);
HttpClient client = FakeHttpClient.GetClient((req, ct) =>
    Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    })
);
```

### 3. Simulate a Timeout

```csharp
HttpClient client = FakeHttpClient.GetClient((req, ct) =>
    throw new TaskCanceledException("Request timed out")
);
```

---

## Injecting into Your Wrapper

Ensure your `LogicSomethingWrapper` has a constructor that accepts an `HttpClient`:

```csharp
public class LogicSomethingWrapper : ILogicSomethingWrapper
{
    private readonly HttpClient _http;

    // Production constructor
    public LogicSomethingWrapper(string baseUrl, string token = null) { /* ... */ }

    // Test-friendly constructor
    public LogicSomethingWrapper(HttpClient httpClient, string token = null)
    {
        _http = httpClient;
        if (!string.IsNullOrEmpty(token))
            SetTokenHeader(token);
    }

    // ... rest of wrapper methods ...
}
```

Then in your unit test:

```csharp
var wrapper = new LogicSomethingWrapper(client, token: "test-token");
var result  = await wrapper.DoSomethingAsync(request);
// Assert on result
```

---

## Example Test Method

```csharp
[TestMethod]
public async Task DoSomethingAsync_UnprocessableEntity_MapsError()
{
    // Arrange
    var body   = "{ \"message\": \"Partner not found\" }";
    var client = FakeHttpClient.GetClient((req, ct) =>
        Task.FromResult(new HttpResponseMessage((HttpStatusCode)422)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        })
    );
    var wrapper = new LogicSomethingWrapper(client);

    // Act
    var result = await wrapper.DoSomethingAsync(
        new RequestSomething { DateOn = "10.12.2025", Product = "Test" }
    );

    // Assert
    Assert.IsFalse(result.IsActive);
    Assert.AreEqual("Partner not found", result.ErrorMessage);
}
```

---

## Tips & Best Practices

* **Reuse** `FakeHttpClient` for all async/sync methods.
* **Simulate** different HTTP statuses and payloads to cover every branch (success, error, timeout).
* **Keep tests fast** by avoiding real network calls.
* **Validate** request properties (URI, headers, body) in your `responder` to ensure your wrapper sends correct data.

Happy testing!
