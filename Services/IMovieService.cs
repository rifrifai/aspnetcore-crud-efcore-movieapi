using MovieApi.Common;
using MovieApi.DTOs;

namespace MovieApi.Services;
public interface IMovieService
{
	Task<MovieDto> CreateMovieAsync(CreateMovieDto command);
	Task<MovieDto?> GetMovieByIdAsync(Guid id);
	Task<PagedResponse<MovieDto>> GetAllMoviesAsync(MovieQueryFilter filter, CancellationToken ct = default);
	Task UpdateMovieAsync(Guid id, UpdateMovieDto command);
	Task DeleteMovieAsync(Guid id);
}