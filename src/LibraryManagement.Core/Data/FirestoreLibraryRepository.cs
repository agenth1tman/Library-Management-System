using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Core;
using LibraryManagement.Models;

namespace LibraryManagement.Data;

/// <summary>Stores the library in Cloud Firestore, using a Firebase service-account key.</summary>
public sealed class FirestoreLibraryRepository : ILibraryRepository
{
    /// <summary>Without this a call made while offline would keep the UI waiting for minutes.</summary>
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(20);

    private readonly FirestoreDb _database;

    private FirestoreLibraryRepository(FirestoreDb database)
    {
        _database = database;
    }

    public string Description => $"Firestore · {_database.ProjectId}";

    public bool IsPersistent => true;

    /// <summary>Builds a client from a service-account key file. Does not touch the network.</summary>
    /// <exception cref="LibraryException">The file is missing or is not a service-account key.</exception>
    public static async Task<FirestoreLibraryRepository> CreateAsync(
        string keyFilePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(keyFilePath))
        {
            throw new LibraryException($"The Firebase key file was not found:\n{keyFilePath}");
        }

        ServiceAccountCredential credential;
        try
        {
            // Asking for a ServiceAccountCredential (rather than "any Google credential")
            // rejects every other kind of credential file up front.
            credential = await CredentialFactory
                .FromFileAsync<ServiceAccountCredential>(keyFilePath, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new LibraryException(
                "That file is not a valid Firebase service-account key. Download a new private key from " +
                "Firebase console → Project settings → Service accounts.",
                exception);
        }

        if (string.IsNullOrWhiteSpace(credential.ProjectId))
        {
            throw new LibraryException("The Firebase key file does not say which project it belongs to (project_id).");
        }

        var database = await new FirestoreDbBuilder
        {
            ProjectId = credential.ProjectId,
            GoogleCredential = credential.ToGoogleCredential(),
        }.BuildAsync(cancellationToken).ConfigureAwait(false);

        return new FirestoreLibraryRepository(database);
    }

    public Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default) =>
        CallAsync<IReadOnlyList<Book>>(
            async token =>
            {
                var snapshot = await Books.GetSnapshotAsync(token).ConfigureAwait(false);
                return snapshot.Documents
                    .Select(document => FirestoreSchema.ToBook(document.Id, document.ToDictionary()))
                    .ToList();
            },
            cancellationToken);

    public Task<IReadOnlyList<Member>> GetMembersAsync(CancellationToken cancellationToken = default) =>
        CallAsync<IReadOnlyList<Member>>(
            async token =>
            {
                var snapshot = await Members.GetSnapshotAsync(token).ConfigureAwait(false);
                return snapshot.Documents
                    .Select(document => FirestoreSchema.ToMember(document.Id, document.ToDictionary()))
                    .ToList();
            },
            cancellationToken);

    public Task AddBookAsync(Book book, CancellationToken cancellationToken = default) =>
        CallAsync(
            async token =>
            {
                try
                {
                    // CreateAsync (unlike SetAsync) refuses to overwrite an existing document.
                    await Books.Document(book.Id)
                        .CreateAsync(FirestoreSchema.NewBookFields(book), token)
                        .ConfigureAwait(false);
                }
                catch (RpcException exception) when (exception.StatusCode == StatusCode.AlreadyExists)
                {
                    throw new BookIdTakenException(book.Id, exception);
                }
            },
            cancellationToken);

    public Task UpdateBookDetailsAsync(
        string bookId,
        BookDetails details,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(bookId, FirestoreSchema.DetailsFields(details), cancellationToken);

    public Task SetLoanAsync(string bookId, Loan? loan, CancellationToken cancellationToken = default) =>
        UpdateAsync(bookId, FirestoreSchema.LoanFields(loan), cancellationToken);

    public Task DeleteBookAsync(string bookId, CancellationToken cancellationToken = default) =>
        CallAsync(
            token => Books.Document(bookId).DeleteAsync(cancellationToken: token),
            cancellationToken);

    private CollectionReference Books => _database.Collection(FirestoreSchema.BooksCollection);

    private CollectionReference Members => _database.Collection(FirestoreSchema.MembersCollection);

    /// <summary>All fields are sent in one request, so a failure cannot leave a half-updated book.</summary>
    private Task UpdateAsync(string bookId, Dictionary<string, object> fields, CancellationToken cancellationToken) =>
        CallAsync(
            token => Books.Document(bookId).UpdateAsync(fields, cancellationToken: token),
            cancellationToken);

    private static Task CallAsync(Func<CancellationToken, Task> call, CancellationToken cancellationToken) =>
        CallAsync<object?>(
            async token =>
            {
                await call(token).ConfigureAwait(false);
                return null;
            },
            cancellationToken);

    private static async Task<T> CallAsync<T>(Func<CancellationToken, Task<T>> call, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CallTimeout);

        try
        {
            return await call(timeout.Token).ConfigureAwait(false);
        }
        catch (LibraryException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new LibraryException(Explain(exception), exception);
        }
    }

    /// <summary>
    /// Turns a low-level failure into a sentence a librarian can act on. The original
    /// exception travels along as the inner exception so the app can log the details.
    /// </summary>
    internal static string Explain(Exception exception)
    {
        const string Unreachable = "Can’t reach the library database. Check the internet connection and try again.";

        if (exception is OperationCanceledException)
        {
            return "The library database took too long to answer. Check the internet connection and try again.";
        }

        if (exception is not RpcException rpc)
        {
            return exception is HttpRequestException or IOException
                ? Unreachable
                : $"The library database reported a problem: {exception.Message}";
        }

        return rpc.StatusCode switch
        {
            StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.Cancelled => Unreachable,

            // gRPC reports "could not even open a connection" as Internal with the network error attached.
            StatusCode.Internal when rpc.Status.DebugException is HttpRequestException or IOException => Unreachable,

            StatusCode.PermissionDenied or StatusCode.Unauthenticated =>
                "Firebase rejected the key file. Make sure it belongs to this project and has not been revoked.",

            StatusCode.NotFound =>
                "That record no longer exists in the database. Refresh and try again.",

            _ => $"The library database reported a problem ({rpc.StatusCode}). Try again in a moment.",
        };
    }
}
