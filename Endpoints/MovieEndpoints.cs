using Microsoft.EntityFrameworkCore;
using MovieApi.Common;
using MovieApi.DTOs;
using MovieApi.Persistence;
using MovieApi.Services;

namespace MovieApi.Endpoints;
public static class MovieEndpoints
{
	public static void MapMovieEndpoints(this IEndpointRouteBuilder routes)
	{
		var movieApi = routes.MapGroup("/api/movies").WithTags("Movies");

		movieApi.MapPost("/", async (IMovieService service, CreateMovieDto command) =>
		{
			var movie = await service.CreateMovieAsync(command);
			return TypedResults.Created($"/api/movies/{movie.Id}", movie);
		});

		// ======== offset pagination ========
		movieApi.MapGet("/", async (
			[AsParameters] MovieQueryFilter filter,
			IMovieService service,
			CancellationToken ct
		) =>
		{
			var moviesFilter = await service.GetAllMoviesAsync(filter, ct);
			return TypedResults.Ok(moviesFilter);
		});


		// ====== GET ALL WITHOUT PAGINATION =======
		// movieApi.MapGet("/", async (IMovieService service) =>
		// {
		// 	var movies = await service.GetAllMoviesAsync();
		// 	return TypedResults.Ok(movies);
		// });

		// ===== keyset pagination ======
		movieApi.MapGet("/cursor", async (MovieDbContext db, DateTimeOffset? after, int pageSize = 10, CancellationToken ct = default) =>
		{
			pageSize = Math.Clamp(pageSize, 1, 50);

			var query = db.Movies
				.AsNoTracking()
				.OrderByDescending(m => m.Created)
				.AsQueryable();

			if (after.HasValue)
			{
				query = query.Where(m => m.Created < after.Value);
			}

			var items = await query
						.Take(pageSize + 1)
						.Select(m => new MovieDto(m.Id, m.Title, m.Genre, m.ReleaseDate, m.Rating))
						.ToListAsync(ct);

			var hasMore = items.Count > pageSize;
			var data = hasMore ? items[..pageSize] : items;

			return TypedResults.Ok(new 
				{ Data = data, HasNextPage = hasMore}
			);
		});

		movieApi.MapGet("/{id}", async (IMovieService service, Guid id) =>
		{
			var movie = await service.GetMovieByIdAsync(id);

			return movie is null
				? (IResult)TypedResults.NotFound(new { Message = $"Movie with ID {id} not found." })
				: TypedResults.Ok(movie);
		});

		movieApi.MapPut("/{id}", async (IMovieService service, Guid id, UpdateMovieDto command) =>
		{
			await service.UpdateMovieAsync(id, command);
			return TypedResults.NoContent();
		});

		movieApi.MapDelete("/{id}", async (IMovieService service, Guid id) =>
		{
			await service.DeleteMovieAsync(id);
			return TypedResults.NoContent();
		});
	}
}