using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using RiotProxy.Infrastructure.External;

namespace RiotProxy.Application.Endpoints
{
    public class UserEndpoint : IEndpoint
    {
        public string Route { get; }

        public UserEndpoint(string basePath)
        {
            Route = basePath + "/user/{userName}";
        }

        public void Configure(WebApplication app)
        {
            ConfigurePost(app);
            ConfigureGet(app);
        }
        
        private void ConfigureGet(WebApplication app)
        {
            app.MapGet(Route, async (
                string userName,
                [FromServices] UserRepository userRepo
                ) =>
            {
                try
                {
                    var user = await userRepo.GetByUserNameAsync(userName);
                    if (user is null)
                    {
                        return Results.NotFound("User not found");
                    }

                    return Results.Content(user.ToJson(), "application/json");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid operation when getting user");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid argument when getting user");
                }
                catch (Exception ex) when (
                    !(ex is OutOfMemoryException) &&
                    !(ex is StackOverflowException) &&
                    !(ex is ThreadAbortException)
                )
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Error when getting user");
                }
            });
        }

        private void ConfigurePost(WebApplication app)
        {
            app.MapPost(Route, async (
                string userName,
                [FromBody] CreateUserRequest body,
                [FromServices] UserRepository userRepo,
                [FromServices] GamerRepository gamerRepo,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] MatchHistorySyncJob matchHistorySyncJob,
                [FromServices] IRiotApiClient riotApiClient
                ) =>
            {
                ValidateBody(body);

                try
                {
                    var user = await userRepo.CreateUserAsync(userName);
                    if (user is null)
                    {
                        return Results.NotFound("Could not create user");
                    }

                    Console.WriteLine($"Created user: {user.UserName} with ID: {user.UserId} ");

                    // Get Puuid from Riot API
                    foreach (var account in body.Accounts)
                    {
                        var puuid = await riotApiClient.GetPuuidAsync(account.GameName, account.TagLine);

                        var summoner = await riotApiClient.GetSummonerByPuuidAsync(account.TagLine, puuid);
                        if (summoner is null)
                        {
                            Console.WriteLine($"Could not find summoner for account: {account.GameName}#{account.TagLine}");
                            continue;
                        }
                        // Create Gamer entry
                        var gamerCreated = await gamerRepo.CreateGamerAsync(puuid, account.GameName, account.TagLine, summoner.ProfileIconId, summoner.SummonerLevel);
                        if (!gamerCreated)
                        {
                            // Log error but continue
                            Console.WriteLine($"Could not create gamer for account: {account.GameName}#{account.TagLine}");
                            continue;
                        }

                        // Link Gamer to User
                        var linkCreated = await userGamerRepo.LinkGamerToUserAsync(user.UserId, puuid);
                        if (!linkCreated)
                        {
                            // Log error but continue
                            Console.WriteLine($"Could not create gamer for account: {account.GameName}#{account.TagLine}");
                            continue;
                        }
                    }
                    
                    // Trigger the background job to update match history
                    Console.WriteLine("Triggering MatchHistorySyncJob after user creation...");
                    _ = Task.Run(() => matchHistorySyncJob.RunJobAsync(CancellationToken.None));

                    return Results.Ok("{\"message\":\"User and gamers created successfully\"}");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid operation when getting user");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid argument when getting user");
                }
                catch (Exception ex) when (
                    !(ex is OutOfMemoryException) &&
                    !(ex is StackOverflowException) &&
                    !(ex is ThreadAbortException)
                )
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Error when getting user");
                }
            });
        }

        private void ValidateBody(CreateUserRequest body)
        {
            if (body == null)
            {
                throw new ArgumentException("Request body is null");
            }

            if (body.Accounts == null || body.Accounts.Count == 0)
            {
                throw new ArgumentException("Accounts list is null or empty");
            }

            foreach (var account in body.Accounts)
            {
                if (string.IsNullOrWhiteSpace(account.GameName))
                {
                    throw new ArgumentException("GameName is null or empty");
                }

                if (string.IsNullOrWhiteSpace(account.TagLine))
                {
                    throw new ArgumentException("TagLine is null or empty");
                }
            }
        }
    }
}