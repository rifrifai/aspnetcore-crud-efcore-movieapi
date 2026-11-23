using Microsoft.EntityFrameworkCore;
using MovieApi.Common;
using MovieApi.DTOs;
using MovieApi.Models;
using MovieApi.Persistence;

namespace MovieApi.Services;

public class MovieService(MovieDbContext _db, ILogger<MovieService> _logger) : IMovieService
{
	public async Task<MovieDto> CreateMovieAsync(CreateMovieDto command)
	{
		var movie = Movie.Create(command.Title, command.Genre, command.ReleaseDate, command.Rating);
		await _db.Movies.AddAsync(movie);
		await _db.SaveChangesAsync();

		return new MovieDto(
			movie.Id,
			movie.Title,
			movie.Genre,
			movie.ReleaseDate,
			movie.Rating
		);
	}

	public async Task<PagedResponse<MovieDto>> GetAllMoviesAsync(MovieQueryFilter filter, CancellationToken ct = default)
	{
		var pageNumber = Math.Max(1, filter.PageNumber);
		var pageSize = Math.Clamp(filter.PageSize, 1, 50);

		var query = _db.Movies.AsNoTracking().AsQueryable();

		// 1. Apply search filter (reduces the dataset)
		query = query.ApplySearch(filter.Search);

		// 2. Count total records AFTER filtering, BEFORE pagination
		var totalRecords = await query.CountAsync(ct);

		// 3. Apply sorting (default to Title if not specified)
		query = query.ApplySort(string.IsNullOrWhiteSpace(filter.SortBy) ? "Title" : filter.SortBy);

		// 4. Apply pagination and project to DTOs
		var movies = await query
			.ApplyPagination(pageNumber, pageSize)
			.Select(m => new MovieDto(m.Id, m.Title, m.Genre, m.ReleaseDate, m.Rating))
			.IgnoreQueryFilters(["HighRated"])
			.ToListAsync(ct);

		return new PagedResponse<MovieDto>
		{
			Data = movies,
			PageNumber = pageNumber,
			PageSize = pageSize,
			TotalRecords = totalRecords,
			TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
		};
	}

	// public async Task<IEnumerable<MovieDto>> GetAllMoviesAsync()
	// {
	// 	var movies = await _db.Movies
	// 							.AsNoTracking()
	// 							.Select(movie => new MovieDto(
	// 								movie.Id,
	// 								movie.Title,
	// 								movie.Genre,
	// 								movie.ReleaseDate,
	// 								movie.Rating
	// 							)).IgnoreQueryFilters().ToListAsync();
	// 	return movies;
	// }

	public async Task<MovieDto?> GetMovieByIdAsync(Guid id)
	{
		var movie = await _db.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
		if (movie is null) return null;

		return new MovieDto(
			movie.Id,
			movie.Title,
			movie.Genre,
			movie.ReleaseDate,
			movie.Rating
		);
	}

	public async Task UpdateMovieAsync(Guid id, UpdateMovieDto command)
	{
		var movieToUpdate = await _db.Movies.FindAsync(id) ?? throw new ArgumentNullException($"Invalid Movie Id {id}");

		movieToUpdate.Update(command.Title, command.Genre, command.ReleaseDate, command.Rating);
		await _db.SaveChangesAsync();
	}

	public async Task DeleteMovieAsync(Guid id)
	{
		var movieToDelete = await _db.Movies.FindAsync(id);
		if (movieToDelete is not null)
		{
			_db.Movies.Remove(movieToDelete);
			await _db.SaveChangesAsync();
		}
	}
}