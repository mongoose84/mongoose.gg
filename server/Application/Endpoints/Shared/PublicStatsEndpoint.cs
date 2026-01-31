using Microsoft.AspNetCore.Mvc;
using RiotProxy.Core.Interfaces;

namespace RiotProxy.Application.Endpoints
{
	/// <summary>
	/// Public stats endpoint for exposing non-sensitive aggregate metrics
	/// (e.g. total matches analyzed) used on the marketing/landing page.
	/// </summary>
	public sealed class PublicStatsEndpoint : IEndpoint
	{
	    public string Route { get; }

	    public PublicStatsEndpoint(string basePath)
	    {
	        Route = basePath + "/public/stats";
	    }

	    public void Configure(WebApplication app)
	    {
	        app.MapGet(Route, async (
	            [FromServices] IMatchesRepository matchesRepository,
	            [FromServices] IUsersRepository usersRepository) =>
	        {
	            var totalMatches = await matchesRepository.GetTotalMatchCountAsync();
	            var activePlayers = await usersRepository.GetActiveUserCountAsync();

	            return Results.Ok(new
	            {
	                totalMatches,
	                activePlayers
	            });
	        });
	    }
	}
}
