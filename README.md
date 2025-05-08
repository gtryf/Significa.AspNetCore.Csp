# Significa.AspNetCore.Csp

## Overview
Significa.AspNetCore.Csp is a middleware component designed to enhance security by implementing Content Security Policy (CSP) headers in ASP.NET Core applications.

## Features
- Easy integration with existing ASP.NET Core applications
- Configurable CSP policies
- Supports multiple environments (development, staging, production)
- Automatic nonce generation and injection

## Installation
To install Significa.AspNetCore.Csp, use the following command:
```sh
dotnet add package Significa.AspNetCore.Csp
```

## Usage
To use Significa.AspNetCore.Csp in your application, follow these steps:

1. Configure the middleware in `Program.cs`:
    ```csharp
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddCsp(builder.Configuration.GetSection(CspConfigurationSection.Name));

    var app = builder.Build();

    // Use CSP middleware
    app.UseCsp();
    app.UseNonceInjection();

    // ...existing code...

    app.Run();
    ```

2. Add CSP configuration to `appsettings.json`:
    ```json
    {
      "CspConfiguration": {
        "NoncePlaceholder": "{nonce}",
        "ReportOnly": false,
        "Default": {
          "Sources": [ "'self'" ]
        },
        "Script": {
          "Sources": [ "'self'", "'unsafe-inline'" ],
          "UseNonce": true
        },
        "Style": {
          "Sources": [ "'self'", "https://fonts.googleapis.com" ],
          "UseNonce": true
        },
        "Image": {
          "Sources": [ "'self'", "data:" ]
        },
        "Connect": {
          "Sources": [ "'self'", "https://api.example.com" ]
        },
        "Font": {
          "Sources": [ "'self'", "https://fonts.gstatic.com" ]
        },
        "Object": {},
        "Media": {
          "Sources": [ "'self'" ]
        },
        "Frame": {
          "Sources": [ "'self'" ]
        },
        "Child": {
          "Sources": [ "'self'" ]
        },
        "Manifest": {
          "Sources": [ "'self'" ]
        },
        "BaseUri": {
          "Sources": [ "'self'" ]
        },
        "FormActions": {
          "Sources": [ "'self'" ]
        },
        "FrameAncestors": {
          "Sources": [ "'self'" ]
        },
        "BlockMixedContent": true,
        "UpgradeInsecureRequests": true,
        "ReportUri": "/csp-report",
        "ReportTo": "/csp-report-to"
      }
    }
    ```

## Configuration
Significa.AspNetCore.Csp can be configured using the following options:
- `NoncePlaceholder`: A placeholder string to be replaced with the generated nonce.
- `ReportOnly`: A boolean indicating whether to use the `Content-Security-Policy-Report-Only` header instead of `Content-Security-Policy`.
- `ReportUri`: A URI to which CSP violation reports should be sent.
- `ReportTo`: A URI to which CSP violation reports should be sent.
- `BlockMixedContent`: A boolean indicating whether to block mixed content.
- `UpgradeInsecureRequests`: A boolean indicating whether to upgrade insecure requests.
- `directives`: An object specifying the CSP directives and their values.

