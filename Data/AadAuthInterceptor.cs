using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace HW5NoteKeeper.Data;

/// <summary>
/// EF Core interceptor that sets the Azure AD access token on every SQL connection
/// using our controlled <see cref="DefaultAzureCredential"/> (which excludes
/// <c>InteractiveBrowserCredential</c> in development to prevent popup windows).
/// </summary>
public class AadAuthInterceptor : DbConnectionInterceptor
{
    private static readonly string[] Scopes = ["https://database.windows.net/.default"];

    private readonly DefaultAzureCredential _credential;

    /// <summary>
    /// Initializes a new instance of <see cref="AadAuthInterceptor"/>.
    /// </summary>
    /// <param name="credential">The Azure credential used to acquire database tokens.</param>
    public AadAuthInterceptor(DefaultAzureCredential credential)
    {
        _credential = credential;
    }

    /// <inheritdoc />
    public override InterceptionResult ConnectionOpening(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        if (connection is SqlConnection sqlConnection)
        {
            var token = _credential.GetToken(new TokenRequestContext(Scopes), default);
            sqlConnection.AccessToken = token.Token;
        }
        return result;
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (connection is SqlConnection sqlConnection)
        {
            var token = await _credential.GetTokenAsync(
                new TokenRequestContext(Scopes), cancellationToken);
            sqlConnection.AccessToken = token.Token;
        }
        return result;
    }
}
