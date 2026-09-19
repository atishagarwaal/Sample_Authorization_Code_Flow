# Sample Authorization Code Flow

1. Overview

This repository contains a minimal sample demonstrating the OAuth2 / OpenID Connect Authorization Code flow with PKCE using Duende IdentityServer, a protected Web API, and a console client that performs the interactive sign-in and calls the API.

2. Description

- IdentityServer: Duende IdentityServer that issues tokens (listening on https://localhost:5001).
- SampleAPI: A protected Web API that validates JWT access tokens and exposes a WeatherForecast endpoint (https://localhost:5003).
- SampleClient: A console application that performs an authorization code + PKCE flow to obtain tokens and call the SampleAPI.

3. Pre-requisites

- .NET 10 SDK installed
- Visual Studio 2026 (or any IDE / editor that supports .NET 10)
- (Optional) Docker if you want to run container profiles

4. Build and Run

Using Visual Studio
- Open Sample_Authorization_Code_Flow.slnx in Visual Studio.
- Set projects individually as startup projects and run in this order: IdentityServer, SampleAPI, then SampleClient. Visual Studio will use the development launch profiles and configured HTTPS ports.

Using the command line (PowerShell)
- Restore and build the solution:
  dotnet restore
  dotnet build
- Start IdentityServer (HTTPS profile — matches https://localhost:5001):
  dotnet run --project IdentityServer --launch-profile https
- Start the API (in a separate terminal):
  dotnet run --project SampleAPI --launch-profile https
- Run the console client (in another terminal):
  dotnet run --project SampleClient