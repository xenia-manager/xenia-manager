﻿using NUnit.Framework.Legacy;
using XeniaManager.Core;
using XeniaManager.Core.Constants;

namespace XeniaManager.Tests;

public class HttpClientServiceTest
{
    private HttpClientService _httpClientService;
        
    [SetUp]
    public void Setup()
    {
        _httpClientService = new HttpClientService();
    }
        
    [Test]
    public async Task GetAsync_ValidUrl_ReturnsNonEmptyJsonResponse()
    {
        // Act: Send the GET request.
        string response = await _httpClientService.GetAsync(Urls.XboxDatabase);

        // Assert: Verify that the response is not null or empty.
        ClassicAssert.IsNotNull(response, "Response should not be null.");
        ClassicAssert.IsNotEmpty(response, "Response should not be empty.");

        // Optionally, verify that the response appears to be valid JSON.
        // For example, checking if it starts with '{' or '['.
        string trimmedResponse = response.TrimStart();
        ClassicAssert.IsTrue(
            trimmedResponse.StartsWith("{") || trimmedResponse.StartsWith("["),
            "Response does not appear to be valid JSON."
        );
    }
        
    [TearDown]
    public void TearDown()
    {
        _httpClientService?.Dispose();
    }
}