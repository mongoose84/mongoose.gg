using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints
{
    public sealed class UsersEndpoint : IEndpoint
    {
        public string Route { get; }

        public UsersEndpoint(string basePath)
        {
            Route = basePath + "/users";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromServices] UserRepository userRepo
                ) =>
            {
                try
                {
                    var users = await userRepo.GetAllUsersAsync();
                
                    return Results.Ok(users);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid operation when getting users");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid argument when getting users");
                }
                catch (Exception ex) when (
                    !(ex is OutOfMemoryException) &&
                    !(ex is StackOverflowException) &&
                    !(ex is ThreadAbortException)
                )
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Error when getting users");
                }
            });
        }
    }
}