using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;

namespace RiotProxy.Application.Endpoints
{
    public sealed class GamersEndpoint : IEndpoint
    {
        public string Route { get; }

        public GamersEndpoint(string basePath)
        {
            Route = basePath + "/gamers/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] GamerRepository gamerRepo,
                [FromServices] IRiotApiClient riotApiClient
                ) =>
            {
                try
                {
                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var gamers = await gamerRepo.GetGamersByUserIdAsync(userIdInt);
                    var lolVersion = await riotApiClient.GetLolVersionAsync();
                    foreach (var gamer in gamers)
                    {
                        gamer.IconUrl = $"https://ddragon.leagueoflegends.com/cdn/{lolVersion}/img/profileicon/{gamer.IconId}.png";
                    }
                
                    return Results.Ok(gamers);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid operation when getting gamers");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid argument when getting gamers");
                }
                catch (Exception ex) when (
                    !(ex is OutOfMemoryException) &&
                    !(ex is StackOverflowException) &&
                    !(ex is ThreadAbortException)
                )
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Error when getting gamers");
                }
            });
        }
    }
}