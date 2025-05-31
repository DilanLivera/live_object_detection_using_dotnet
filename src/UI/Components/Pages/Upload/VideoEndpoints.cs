using Microsoft.AspNetCore.Mvc;
using UI.Infrastructure;

namespace UI.Components.Pages.Upload;

public static class VideoEndpoints
{
    public static void MapVideoEndpoints(this WebApplication app)
        => app.MapGet(pattern: "/api/video/{filename}",
                      (string filename, FileService fileService) =>
                      {
                          if (string.IsNullOrWhiteSpace(filename))
                          {
                              return Results.Problem(title: "Invalid filename",
                                                     detail: "Filename parameter is required and cannot be empty",
                                                     statusCode: StatusCodes.Status400BadRequest);
                          }

                          string[] allowedExtensions = [".mp4", ".webm", ".mov"];
                          string extension = Path.GetExtension(filename).ToLowerInvariant();

                          if (!allowedExtensions.Contains(extension))
                          {
                              return Results.Problem(title: "Unsupported file type",
                                                     detail: $"File extension '{extension}' is not supported. Allowed types: {string.Join(", ", allowedExtensions)}",
                                                     statusCode: StatusCodes.Status400BadRequest);
                          }

                          Result<(FileStream Stream, string ContentType)> result = fileService.GetVideoAsStream(filename);

                          if (!result.IsSuccess)
                          {
                              return result.ErrorMessage! switch
                              {
                                  var error when error.Contains("could not be found") =>
                                      Results.Problem(title: "Video file not found",
                                                      detail: error,
                                                      statusCode: StatusCodes.Status404NotFound),

                                  _ => Results.Problem(title: "Internal server error",
                                                       detail: result.ErrorMessage!,
                                                       statusCode: StatusCodes.Status500InternalServerError)
                              };
                          }

                          (FileStream fileStream, string contentType) = result.Value;

                          return Results.File(fileStream, contentType, enableRangeProcessing: true);
                      })
              .WithName("GetVideo")
              .WithSummary("Serves a video file by its filename")
              .WithDescription("Returns a video file stream with proper content type and range processing support")
              .Produces<FileStreamResult>(StatusCodes.Status200OK, contentType: "video/mp4", "video/webm", "video/quicktime")
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
}