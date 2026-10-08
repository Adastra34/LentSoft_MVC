using System.Net;
using System.Net.Http.Headers;

namespace LentSoft.Mobile.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly IAuthStorageService _storageService;

    public AuthenticatedHttpClientHandler(IAuthStorageService storageService)
    {
        _storageService = storageService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _storageService.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent($"{{\"message\": \"No hay conexión con el servidor: {ex.Message}\"}}")
            };
        }
        catch (Exception ex)
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent($"{{\"message\": \"Error de red: {ex.Message}\"}}")
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _storageService.ClearAsync();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (Shell.Current != null)
                    {
                        await Shell.Current.GoToAsync("//login");
                    }
                }
                catch { }
            });
        }

        return response;
    }
}
